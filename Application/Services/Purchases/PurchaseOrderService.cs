using System.Text.Json;
using Application.DTOs.Purchases;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Goods;
using Domain.Entities.Purchases;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 采购单应用服务，实现事务化 CRUD、计划数量占用及草稿完成或取消状态流转。
/// </summary>
public class PurchaseOrderService(
    IPurchaseOrderRepository purchaseOrderRepository,
    IPurchasePlanRepository purchasePlanRepository,
    ISupplierRepository supplierRepository,
    IPurchaserRepository purchaserRepository,
    IGoodsRepository goodsRepository,
    IGoodsUnitRepository goodsUnitRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreatePurchaseOrderDto> createValidator,
    IValidator<UpdatePurchaseOrderDto> updateValidator,
    IValidator<GeneratePurchaseOrdersFromPlansDto> generateValidator,
    ILogger<PurchaseOrderService> logger) : IPurchaseOrderService
{
    private const decimal QuantityTolerance = 0.000001m;

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(PurchaseOrderQueryParameters parameters)
    {
        var result = await purchaseOrderRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size,
            x => x.CreateTime!,
            true);
        return mapper.ToPagedResult<PurchaseOrder, PurchaseOrderDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto> GetByIdAsync(Guid id)
    {
        return mapper.Map<PurchaseOrderDto>(await GetRequiredOrderAsync(id));
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto)
    {
        await ValidateAsync(createValidator, dto);
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseNo = await GeneratePurchaseNoAsync(),
            PurchasePattern = dto.PurchasePattern,
            ReceiveTime = dto.ReceiveTime,
            BusinessStatus = PurchaseOrderStatus.Draft,
            Remark = Normalize(dto.Remark)
        };
        await ApplyPartiesAsync(
            order,
            dto.SupplierId,
            dto.PurchaserId,
            dto.SupplierContactName,
            dto.SupplierContactPhone);
        ApplyCreateAudit(order);

        foreach (var detailDto in dto.Details)
        {
            var detail = await BuildManualDetailAsync(order.Id, detailDto);
            ApplyCreateAudit(detail);
            order.Details.Add(detail);
        }

        await ExecuteInTransactionAsync(async () => await purchaseOrderRepository.AddAsync(order));
        logger.LogInformation("采购单手工创建成功: {PurchaseOrderId}, {PurchaseNo}", order.Id, order.PurchaseNo);
        return mapper.Map<PurchaseOrderDto>(await GetRequiredOrderAsync(order.Id));
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto> UpdateAsync(UpdatePurchaseOrderDto dto)
    {
        await ValidateAsync(updateValidator, dto);
        var order = await GetRequiredOrderAsync(dto.Id);
        EnsureDraft(order, "编辑");
        ValidateDetailOwnership(order, dto.Details);

        var originalAllocations = GetAllocationTotals(order.Details.SelectMany(x => x.PlanRelations));
        var preparedDetails = new List<PreparedDetail>(dto.Details.Count);
        foreach (var detailDto in dto.Details)
        {
            preparedDetails.Add(await PrepareDetailAsync(detailDto));
        }

        var requestedAllocations = preparedDetails
            .SelectMany(x => x.Allocations)
            .GroupBy(x => x.PlanDetail.Id)
            .ToDictionary(x => x.Key, x => RoundQuantity(x.Sum(item => item.Quantity)));
        ValidatePlanAvailability(originalAllocations, requestedAllocations, preparedDetails);

        await ExecuteInTransactionAsync(async () =>
        {
            await ApplyPartiesAsync(
                order,
                dto.SupplierId,
                dto.PurchaserId,
                dto.SupplierContactName,
                dto.SupplierContactPhone);
            order.PurchasePattern = dto.PurchasePattern;
            order.ReceiveTime = dto.ReceiveTime;
            order.Remark = Normalize(dto.Remark);
            ApplyUpdateAudit(order);

            ApplyAllocationDeltas(originalAllocations, requestedAllocations, preparedDetails, order);
            SynchronizeDetails(order, preparedDetails);
            await purchaseOrderRepository.UpdateAsync(order);
        });

        logger.LogInformation("采购单更新成功: {PurchaseOrderId}, {PurchaseNo}", order.Id, order.PurchaseNo);
        return mapper.Map<PurchaseOrderDto>(await GetRequiredOrderAsync(order.Id));
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        var order = await GetRequiredOrderAsync(id);
        EnsureDraft(order, "删除");
        await ExecuteInTransactionAsync(async () =>
        {
            ReleasePlanAllocations(order);
            await purchaseOrderRepository.DeleteAsync(order);
        });
        logger.LogInformation("采购单草稿删除成功: {PurchaseOrderId}, {PurchaseNo}", order.Id, order.PurchaseNo);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<PurchaseOrderDto>> GenerateFromPlansAsync(GeneratePurchaseOrdersFromPlansDto dto)
    {
        await ValidateAsync(generateValidator, dto);
        var plans = new List<PurchasePlan>();
        foreach (var planId in dto.PlanIds.Distinct())
        {
            var plan = await purchasePlanRepository.GetByIdAsync(planId)
                       ?? throw new BusinessException($"采购计划不存在: {planId}");
            ValidatePlanForGeneration(plan);
            plans.Add(plan);
        }

        var orders = new List<PurchaseOrder>();
        foreach (var group in plans.GroupBy(x => new GenerationGroupKey(
                     x.PurchasePattern,
                     x.SupplierId,
                     x.PurchaserId)))
        {
            var order = await BuildOrderFromPlansAsync(group.ToList(), dto);
            orders.Add(order);
        }

        await ExecuteInTransactionAsync(async () =>
        {
            foreach (var order in orders)
            {
                await purchaseOrderRepository.AddAsync(order);
            }

            foreach (var plan in plans)
            {
                foreach (var detail in plan.Details)
                {
                    if (detail.PlannedQuantity > detail.PurchasedQuantity)
                    {
                        detail.PurchasedQuantity = detail.PlannedQuantity;
                        ApplyUpdateAudit(detail);
                    }
                }

                RefreshPlanStatus(plan);
                ApplyUpdateAudit(plan);
                await purchasePlanRepository.UpdateAsync(plan);
            }
        });

        var results = new List<PurchaseOrderDto>(orders.Count);
        foreach (var order in orders)
        {
            results.Add(mapper.Map<PurchaseOrderDto>(await GetRequiredOrderAsync(order.Id)));
        }

        logger.LogInformation("采购计划生成采购单成功: {PlanCount} 个计划, {OrderCount} 张采购单", plans.Count, orders.Count);
        return results;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto> CompleteAsync(Guid id)
    {
        var order = await GetRequiredOrderAsync(id);
        EnsureDraft(order, "完成");
        if (!order.PurchaserId.HasValue)
        {
            throw new BusinessException("采购单完成前必须分配采购员");
        }

        if (order.PurchasePattern == PurchasePattern.SupplierDirect && !order.SupplierId.HasValue)
        {
            throw new BusinessException("供应商直供采购单完成前必须选择供应商");
        }

        if (order.Details.Count == 0 || order.Details.Any(x => x.PurchaseQuantity <= 0m))
        {
            throw new BusinessException("采购单必须包含有效采购商品才能完成");
        }

        order.BusinessStatus = PurchaseOrderStatus.Completed;
        ApplyUpdateAudit(order);
        await ExecuteInTransactionAsync(async () => await purchaseOrderRepository.UpdateAsync(order));
        logger.LogInformation("采购单完成: {PurchaseOrderId}, {PurchaseNo}", order.Id, order.PurchaseNo);
        return mapper.Map<PurchaseOrderDto>(await GetRequiredOrderAsync(order.Id));
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderDto> CancelAsync(Guid id)
    {
        var order = await GetRequiredOrderAsync(id);
        EnsureDraft(order, "取消");
        await ExecuteInTransactionAsync(async () =>
        {
            ReleasePlanAllocations(order);
            order.BusinessStatus = PurchaseOrderStatus.Cancelled;
            ApplyUpdateAudit(order);
            await purchaseOrderRepository.UpdateAsync(order);
        });
        logger.LogInformation("采购单取消: {PurchaseOrderId}, {PurchaseNo}", order.Id, order.PurchaseNo);
        return mapper.Map<PurchaseOrderDto>(await GetRequiredOrderAsync(order.Id));
    }

    private async Task<PurchaseOrder> BuildOrderFromPlansAsync(
        IReadOnlyCollection<PurchasePlan> plans,
        GeneratePurchaseOrdersFromPlansDto dto)
    {
        var firstPlan = plans.First();
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseNo = await GeneratePurchaseNoAsync(),
            PurchasePattern = firstPlan.PurchasePattern,
            ReceiveTime = dto.ReceiveTime ?? plans.Min(x => x.PlanDate),
            BusinessStatus = PurchaseOrderStatus.Draft,
            Remark = Normalize(dto.Remark)
        };
        await ApplyPartiesAsync(order, firstPlan.SupplierId, firstPlan.PurchaserId, null, null);
        ApplyCreateAudit(order);

        var remainingDetails = plans
            .SelectMany(x => x.Details)
            .Where(x => x.PlannedQuantity - x.PurchasedQuantity > QuantityTolerance)
            .ToList();
        foreach (var detailGroup in remainingDetails.GroupBy(x => new { x.GoodsId, x.PurchaseUnitId }))
        {
            var firstDetail = detailGroup.First();
            var purchaseQuantity = RoundQuantity(detailGroup.Sum(x => x.PlannedQuantity - x.PurchasedQuantity));
            var orderDetail = new PurchaseOrderDetail
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = order.Id,
                GoodsId = firstDetail.GoodsId,
                GoodsNameSnapshot = firstDetail.GoodsNameSnapshot,
                GoodsCodeSnapshot = firstDetail.GoodsCodeSnapshot,
                PurchaseUnitId = firstDetail.PurchaseUnitId,
                PurchaseUnitNameSnapshot = firstDetail.PurchaseUnitNameSnapshot,
                RequiredQuantity = purchaseQuantity,
                PurchaseQuantity = purchaseQuantity,
                PurchasePrice = 0m,
                PurchaseTotalPrice = 0m,
                Remark = Normalize(firstDetail.Remark)
            };
            ApplyCreateAudit(orderDetail);

            foreach (var planDetail in detailGroup)
            {
                var allocatedQuantity = RoundQuantity(planDetail.PlannedQuantity - planDetail.PurchasedQuantity);
                var relation = new PurchaseOrderPlanRelation
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderDetailId = orderDetail.Id,
                    PurchasePlanDetailId = planDetail.Id,
                    AllocatedQuantity = allocatedQuantity,
                    PurchasePlanDetail = planDetail
                };
                ApplyCreateAudit(relation);
                orderDetail.PlanRelations.Add(relation);
            }

            order.Details.Add(orderDetail);
        }

        return order;
    }

    private async Task<PurchaseOrderDetail> BuildManualDetailAsync(
        Guid purchaseOrderId,
        CreatePurchaseOrderDetailDto dto)
    {
        var (goods, unit) = await GetGoodsAndUnitAsync(dto.GoodsId, dto.PurchaseUnitId);
        return CreateDetail(
            Guid.NewGuid(),
            purchaseOrderId,
            goods,
            unit,
            dto.RequiredQuantity ?? dto.PurchaseQuantity,
            dto.PurchaseQuantity,
            dto.PurchasePrice,
            dto.ProductDate,
            dto.Remark);
    }

    private async Task<PreparedDetail> PrepareDetailAsync(UpdatePurchaseOrderDetailDto dto)
    {
        var (goods, unit) = await GetGoodsAndUnitAsync(dto.GoodsId, dto.PurchaseUnitId);
        var allocations = new List<PreparedAllocation>(dto.PlanAllocations.Count);
        foreach (var allocationDto in dto.PlanAllocations)
        {
            var planDetail = await purchasePlanRepository.GetDetailByIdAsync(allocationDto.PurchasePlanDetailId)
                             ?? throw new BusinessException("采购计划商品明细不存在");
            if (planDetail.GoodsId != goods.Id || planDetail.PurchaseUnitId != unit.Id)
            {
                throw new BusinessException($"采购计划 {planDetail.PurchasePlan.PlanNo} 的商品或采购单位与采购单商品行不一致");
            }

            allocations.Add(new PreparedAllocation(planDetail, RoundQuantity(allocationDto.AllocatedQuantity)));
        }

        if (allocations.Select(x => x.PlanDetail.Id).Distinct().Count() != allocations.Count)
        {
            throw new BusinessException("同一采购单商品行不能重复占用同一采购计划明细");
        }

        if (allocations.Count > 0
            && Math.Abs(allocations.Sum(x => x.Quantity) - dto.PurchaseQuantity) > QuantityTolerance)
        {
            throw new BusinessException("采购计划占用数量合计必须等于采购数量");
        }

        return new PreparedDetail(dto, goods, unit, allocations);
    }

    private static void ValidatePlanAvailability(
        IReadOnlyDictionary<Guid, decimal> originalAllocations,
        IReadOnlyDictionary<Guid, decimal> requestedAllocations,
        IEnumerable<PreparedDetail> preparedDetails)
    {
        var planDetails = preparedDetails
            .SelectMany(x => x.Allocations)
            .Select(x => x.PlanDetail)
            .DistinctBy(x => x.Id)
            .ToDictionary(x => x.Id);
        foreach (var (detailId, requestedQuantity) in requestedAllocations)
        {
            var planDetail = planDetails[detailId];
            var originalQuantity = originalAllocations.GetValueOrDefault(detailId);
            var availableQuantity = RoundQuantity(
                planDetail.PlannedQuantity - planDetail.PurchasedQuantity + originalQuantity);
            if (requestedQuantity - availableQuantity > QuantityTolerance)
            {
                throw new BusinessException($"采购计划 {planDetail.PurchasePlan.PlanNo} 可生成数量不足");
            }
        }
    }

    private void ApplyAllocationDeltas(
        IReadOnlyDictionary<Guid, decimal> originalAllocations,
        IReadOnlyDictionary<Guid, decimal> requestedAllocations,
        IEnumerable<PreparedDetail> preparedDetails,
        PurchaseOrder order)
    {
        var planDetails = preparedDetails
            .SelectMany(x => x.Allocations)
            .Select(x => x.PlanDetail)
            .Concat(order.Details
                .SelectMany(x => x.PlanRelations)
                .Select(x => x.PurchasePlanDetail))
            .DistinctBy(x => x.Id)
            .ToDictionary(x => x.Id);

        foreach (var planDetail in planDetails.Values)
        {
            var delta = requestedAllocations.GetValueOrDefault(planDetail.Id)
                        - originalAllocations.GetValueOrDefault(planDetail.Id);
            if (Math.Abs(delta) > QuantityTolerance)
            {
                planDetail.PurchasedQuantity = RoundQuantity(planDetail.PurchasedQuantity + delta);
                ApplyUpdateAudit(planDetail);
            }

            RefreshPlanStatus(planDetail.PurchasePlan);
            ApplyUpdateAudit(planDetail.PurchasePlan);
        }
    }

    private void SynchronizeDetails(PurchaseOrder order, IReadOnlyCollection<PreparedDetail> preparedDetails)
    {
        var existingById = order.Details.ToDictionary(x => x.Id);
        var retainedIds = preparedDetails
            .Where(x => x.Dto.Id.HasValue)
            .Select(x => x.Dto.Id!.Value)
            .ToHashSet();
        foreach (var removed in order.Details.Where(x => !retainedIds.Contains(x.Id)).ToList())
        {
            order.Details.Remove(removed);
        }

        foreach (var prepared in preparedDetails)
        {
            var detail = prepared.Dto.Id.HasValue
                ? existingById[prepared.Dto.Id.Value]
                : new PurchaseOrderDetail { Id = Guid.NewGuid(), PurchaseOrderId = order.Id };
            ApplyDetailValues(detail, prepared);
            SynchronizeRelations(detail, prepared.Allocations);
            if (!prepared.Dto.Id.HasValue)
            {
                ApplyCreateAudit(detail);
                order.Details.Add(detail);
            }
            else
            {
                ApplyUpdateAudit(detail);
            }
        }
    }

    private void SynchronizeRelations(
        PurchaseOrderDetail detail,
        IReadOnlyCollection<PreparedAllocation> allocations)
    {
        var existingByPlanDetail = detail.PlanRelations.ToDictionary(x => x.PurchasePlanDetailId);
        var retainedPlanDetailIds = allocations.Select(x => x.PlanDetail.Id).ToHashSet();
        foreach (var removed in detail.PlanRelations
                     .Where(x => !retainedPlanDetailIds.Contains(x.PurchasePlanDetailId))
                     .ToList())
        {
            detail.PlanRelations.Remove(removed);
        }

        foreach (var allocation in allocations)
        {
            if (existingByPlanDetail.TryGetValue(allocation.PlanDetail.Id, out var relation))
            {
                relation.AllocatedQuantity = allocation.Quantity;
                ApplyUpdateAudit(relation);
                continue;
            }

            relation = new PurchaseOrderPlanRelation
            {
                Id = Guid.NewGuid(),
                PurchaseOrderDetailId = detail.Id,
                PurchasePlanDetailId = allocation.PlanDetail.Id,
                PurchasePlanDetail = allocation.PlanDetail,
                AllocatedQuantity = allocation.Quantity
            };
            ApplyCreateAudit(relation);
            detail.PlanRelations.Add(relation);
        }
    }

    private static void ApplyDetailValues(PurchaseOrderDetail detail, PreparedDetail prepared)
    {
        var dto = prepared.Dto;
        detail.GoodsId = prepared.Goods.Id;
        detail.GoodsNameSnapshot = prepared.Goods.Name;
        detail.GoodsCodeSnapshot = prepared.Goods.Code;
        detail.GoodsInfoSnapshot = SerializeGoodsInfo(prepared.Goods);
        detail.PurchaseUnitId = prepared.Unit.Id;
        detail.PurchaseUnitNameSnapshot = prepared.Unit.Name;
        detail.RequiredQuantity = dto.RequiredQuantity ?? dto.PurchaseQuantity;
        detail.PurchaseQuantity = dto.PurchaseQuantity;
        detail.PurchasePrice = dto.PurchasePrice;
        detail.PurchaseTotalPrice = RoundMoney(dto.PurchaseQuantity * dto.PurchasePrice);
        detail.ProductDate = dto.ProductDate;
        detail.Remark = Normalize(dto.Remark);
    }

    private static PurchaseOrderDetail CreateDetail(
        Guid id,
        Guid purchaseOrderId,
        Goods goods,
        GoodsUnit unit,
        decimal requiredQuantity,
        decimal purchaseQuantity,
        decimal purchasePrice,
        DateOnly? productDate,
        string? remark)
    {
        return new PurchaseOrderDetail
        {
            Id = id,
            PurchaseOrderId = purchaseOrderId,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            GoodsInfoSnapshot = SerializeGoodsInfo(goods),
            PurchaseUnitId = unit.Id,
            PurchaseUnitNameSnapshot = unit.Name,
            RequiredQuantity = requiredQuantity,
            PurchaseQuantity = purchaseQuantity,
            PurchasePrice = purchasePrice,
            PurchaseTotalPrice = RoundMoney(purchaseQuantity * purchasePrice),
            ProductDate = productDate,
            Remark = Normalize(remark)
        };
    }

    private async Task<(Goods Goods, GoodsUnit Unit)> GetGoodsAndUnitAsync(Guid goodsId, Guid unitId)
    {
        var goods = await goodsRepository.GetByIdAsync(goodsId)
                    ?? throw new BusinessException("商品不存在");
        var unit = await goodsUnitRepository.GetByIdAsync(unitId);
        if (unit is null || unit.GoodsId != goods.Id)
        {
            throw new BusinessException($"采购单位不属于商品 {goods.Name}");
        }

        return (goods, unit);
    }

    private async Task ApplyPartiesAsync(
        PurchaseOrder order,
        Guid? supplierId,
        Guid? purchaserId,
        string? contactName,
        string? contactPhone)
    {
        Supplier? supplier = null;
        if (supplierId.HasValue)
        {
            supplier = await supplierRepository.GetByIdAsync(supplierId.Value)
                       ?? throw new BusinessException("供应商不存在");
        }

        Purchaser? purchaser = null;
        if (purchaserId.HasValue)
        {
            purchaser = await purchaserRepository.GetByIdAsync(purchaserId.Value)
                        ?? throw new BusinessException("采购员不存在");
        }

        order.SupplierId = supplier?.Id;
        order.SupplierNameSnapshot = supplier?.Name;
        order.SupplierContactNameSnapshot = Normalize(contactName) ?? supplier?.ContactName;
        order.SupplierContactPhoneSnapshot = Normalize(contactPhone) ?? supplier?.ContactPhone;
        order.PurchaserId = purchaser?.Id;
        order.PurchaserNameSnapshot = purchaser?.Name;
    }

    private static void ValidatePlanForGeneration(PurchasePlan plan)
    {
        if (plan.PurchasePattern == PurchasePattern.SupplierDirect && !plan.SupplierId.HasValue)
        {
            throw new BusinessException($"采购计划 {plan.PlanNo} 为供应商直供，生成采购单前必须分配供应商");
        }

        if (!plan.PurchaserId.HasValue)
        {
            throw new BusinessException($"采购计划 {plan.PlanNo} 生成采购单前必须分配采购员");
        }

        if (plan.Details.Count == 0
            || plan.Details.All(x => x.PlannedQuantity - x.PurchasedQuantity <= QuantityTolerance))
        {
            throw new BusinessException($"采购计划 {plan.PlanNo} 没有可生成采购单的剩余数量");
        }
    }

    private void ReleasePlanAllocations(PurchaseOrder order)
    {
        var affectedPlans = new Dictionary<Guid, PurchasePlan>();
        foreach (var relation in order.Details.SelectMany(x => x.PlanRelations))
        {
            var planDetail = relation.PurchasePlanDetail;
            planDetail.PurchasedQuantity = RoundQuantity(
                Math.Max(0m, planDetail.PurchasedQuantity - relation.AllocatedQuantity));
            ApplyUpdateAudit(planDetail);
            affectedPlans[planDetail.PurchasePlanId] = planDetail.PurchasePlan;
        }

        foreach (var plan in affectedPlans.Values)
        {
            RefreshPlanStatus(plan);
            ApplyUpdateAudit(plan);
        }
    }

    private static Dictionary<Guid, decimal> GetAllocationTotals(
        IEnumerable<PurchaseOrderPlanRelation> relations)
    {
        return relations
            .GroupBy(x => x.PurchasePlanDetailId)
            .ToDictionary(x => x.Key, x => RoundQuantity(x.Sum(item => item.AllocatedQuantity)));
    }

    private static void RefreshPlanStatus(PurchasePlan plan)
    {
        if (plan.Details.All(x => x.PurchasedQuantity <= QuantityTolerance))
        {
            plan.PurchaseStatus = PurchasePlanStatus.Unpublished;
        }
        else if (plan.Details.All(x => x.PurchasedQuantity >= x.PlannedQuantity - QuantityTolerance))
        {
            plan.PurchaseStatus = PurchasePlanStatus.Generated;
        }
        else
        {
            plan.PurchaseStatus = PurchasePlanStatus.PartiallyGenerated;
        }
    }

    private static void ValidateDetailOwnership(
        PurchaseOrder order,
        IEnumerable<UpdatePurchaseOrderDetailDto> details)
    {
        var existingIds = order.Details.Select(x => x.Id).ToHashSet();
        var foreignId = details
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .FirstOrDefault(id => !existingIds.Contains(id));
        if (foreignId != Guid.Empty)
        {
            throw new BusinessException("采购单商品行不属于当前采购单");
        }
    }

    private static void EnsureDraft(PurchaseOrder order, string operation)
    {
        if (order.BusinessStatus != PurchaseOrderStatus.Draft)
        {
            throw new BusinessException($"采购单 {order.PurchaseNo} 不是草稿，不能{operation}");
        }
    }

    private async Task<PurchaseOrder> GetRequiredOrderAsync(Guid id)
    {
        return await purchaseOrderRepository.GetByIdAsync(id)
               ?? throw new NotFoundException("采购单不存在");
    }

    private async Task<string> GeneratePurchaseNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            var purchaseNo = $"PO{DateTime.UtcNow:yyyyMMddHHmmssfff}{suffix}";
            if (!await purchaseOrderRepository.ExistsPurchaseNoAsync(purchaseNo))
            {
                return purchaseNo;
            }
        }

        throw new BusinessException("采购单编号生成失败，请重试");
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }

    private async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        await unitOfWork.BeginTransactionAsync();
        try
        {
            await action();
            await unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            if (unitOfWork.HasActiveTransaction)
            {
                await unitOfWork.RollbackTransactionAsync();
            }

            throw;
        }
    }

    private void ApplyCreateAudit(BaseEntity entity)
    {
        entity.CreateBy = currentUserService.GetUserId();
        entity.CreateName = currentUserService.GetUserName();
    }

    private void ApplyUpdateAudit(BaseEntity entity)
    {
        entity.UpdateBy = currentUserService.GetUserId();
        entity.UpdateName = currentUserService.GetUserName();
    }

    private static string SerializeGoodsInfo(Goods goods)
    {
        return JsonSerializer.Serialize(new
        {
            goods.Spec,
            goods.Brand,
            goods.Origin
        });
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static decimal RoundQuantity(decimal quantity)
    {
        return decimal.Round(quantity, 6, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundMoney(decimal money)
    {
        return decimal.Round(money, 4, MidpointRounding.AwayFromZero);
    }

    private sealed record GenerationGroupKey(
        PurchasePattern PurchasePattern,
        Guid? SupplierId,
        Guid? PurchaserId);

    private sealed record PreparedAllocation(PurchasePlanDetail PlanDetail, decimal Quantity);

    private sealed record PreparedDetail(
        UpdatePurchaseOrderDetailDto Dto,
        Goods Goods,
        GoodsUnit Unit,
        IReadOnlyCollection<PreparedAllocation> Allocations);
}
