using Application.DTOs.Purchases;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 采购计划应用服务，实现查询、手工新增与从已审核订单生成计划。
/// </summary>
public class PurchasePlanService(
    IPurchasePlanRepository purchasePlanRepository,
    ISaleOrderRepository saleOrderRepository,
    ISupplierRepository supplierRepository,
    IPurchaserRepository purchaserRepository,
    IGoodsRepository goodsRepository,
    IGoodsUnitRepository goodsUnitRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreatePurchasePlanDto> createValidator,
    IValidator<GeneratePurchasePlanFromOrdersDto> generateValidator,
    ILogger<PurchasePlanService> logger) : IPurchasePlanService
{
    /// <inheritdoc />
    public async Task<PagedResult<PurchasePlanDto>> GetPagedAsync(PurchasePlanQueryParameters parameters)
    {
        var result = await purchasePlanRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size,
            x => x.PlanDate,
            true);
        return mapper.ToPagedResult<PurchasePlan, PurchasePlanDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<PurchasePlanDto> GetByIdAsync(Guid id)
    {
        var plan = await GetRequiredPlanAsync(id);
        return mapper.Map<PurchasePlanDto>(plan);
    }

    /// <inheritdoc />
    public async Task<PurchasePlanDto> CreateAsync(CreatePurchasePlanDto dto)
    {
        await ValidateAsync(createValidator, dto);

        var plan = new PurchasePlan
        {
            Id = Guid.NewGuid(),
            PlanNo = await GeneratePlanNoAsync(),
            PlanDate = dto.PlanDate,
            PurchasePattern = dto.PurchasePattern,
            PurchaseStatus = PurchasePlanStatus.Unpublished,
            Remark = NormalizeRemark(dto.Remark)
        };
        await ApplySupplierAsync(plan, dto.SupplierId);
        await ApplyPurchaserAsync(plan, dto.PurchaserId);
        ApplyCreateAudit(plan);

        foreach (var detailDto in dto.Details)
        {
            var detail = await BuildManualDetailAsync(plan.Id, detailDto);
            ApplyCreateAudit(detail);
            plan.Details.Add(detail);
        }

        await ExecuteInTransactionAsync(async () => await purchasePlanRepository.AddAsync(plan));

        logger.LogInformation("采购计划手工创建成功: {PlanId}, {PlanNo}", plan.Id, plan.PlanNo);
        return mapper.Map<PurchasePlanDto>(await GetRequiredPlanAsync(plan.Id));
    }

    /// <inheritdoc />
    public async Task<List<PurchasePlanDto>> GenerateFromOrdersAsync(GeneratePurchasePlanFromOrdersDto dto)
    {
        await ValidateAsync(generateValidator, dto);

        var remark = NormalizeRemark(dto.Remark);
        var orderIds = dto.OrderIds.Distinct().ToList();
        var orders = new List<SaleOrder>();
        foreach (var orderId in orderIds)
        {
            var order = await saleOrderRepository.GetByIdAsync(orderId)
                        ?? throw new BusinessException($"销售订单不存在: {orderId}");
            if (!IsApproved(order.OrderStatus))
            {
                throw new BusinessException($"订单 {order.OrderNo} 未审核通过，不能生成采购计划");
            }

            if (order.HasPurchasePlan)
            {
                throw new BusinessException($"订单 {order.OrderNo} 已生成过采购计划");
            }

            if (order.Details.Count == 0)
            {
                throw new BusinessException($"订单 {order.OrderNo} 没有可生成采购计划的商品明细");
            }

            orders.Add(order);
        }

        var createdPlanIds = new List<Guid>();
        await ExecuteInTransactionAsync(async () =>
        {
            foreach (var order in orders)
            {
                var plan = BuildPlanFromOrder(order, remark);
                createdPlanIds.Add(plan.Id);
                await purchasePlanRepository.AddAsync(plan);

                // 订单经 GetByIdAsync 加载后处于跟踪状态，直接改写标记即可随事务保存。
                order.HasPurchasePlan = true;
                ApplyUpdateAudit(order);
                foreach (var orderDetail in order.Details)
                {
                    orderDetail.HasPurchasePlan = true;
                }
            }
        });

        logger.LogInformation(
            "采购计划从订单生成成功: {OrderCount} 个订单, {PlanCount} 张计划",
            orders.Count,
            createdPlanIds.Count);

        var results = new List<PurchasePlanDto>(createdPlanIds.Count);
        foreach (var planId in createdPlanIds)
        {
            results.Add(mapper.Map<PurchasePlanDto>(await GetRequiredPlanAsync(planId)));
        }

        return results;
    }

    /// <summary>
    /// 根据一张已审核订单构建采购计划，按商品聚合明细并保留订单来源关系。
    /// </summary>
    /// <param name="order">已审核通过的销售订单。</param>
    /// <param name="remark">写入采购计划的备注。</param>
    /// <returns>待持久化的采购计划实体。</returns>
    private PurchasePlan BuildPlanFromOrder(SaleOrder order, string? remark)
    {
        var plan = new PurchasePlan
        {
            Id = Guid.NewGuid(),
            PlanNo = GeneratePlanNoValue(),
            PlanDate = order.ReceiveDate ?? order.OrderDate,
            PurchasePattern = PurchasePattern.SupplierDirect,
            PurchaseStatus = PurchasePlanStatus.Unpublished,
            Remark = remark
        };
        ApplyCreateAudit(plan);

        // 同一商品的多条订单明细合并为一条采购计划明细，采购单位取商品基础单位。
        var goodsGroups = order.Details
            .GroupBy(detail => new { detail.GoodsId, PurchaseUnitId = detail.BaseUnitId ?? detail.GoodsUnitId });
        foreach (var group in goodsGroups)
        {
            var sample = group.First();
            var purchaseUnitId = sample.BaseUnitId ?? sample.GoodsUnitId;
            var purchaseUnitName = sample.BaseUnitNameSnapshot ?? sample.GoodsUnitNameSnapshot;
            var detail = new PurchasePlanDetail
            {
                Id = Guid.NewGuid(),
                PurchasePlanId = plan.Id,
                GoodsId = sample.GoodsId,
                GoodsNameSnapshot = sample.GoodsNameSnapshot,
                GoodsCodeSnapshot = sample.GoodsCodeSnapshot,
                PurchaseUnitId = purchaseUnitId,
                PurchaseUnitNameSnapshot = purchaseUnitName,
                RequiredQuantity = group.Sum(GetPurchaseQuantity),
                PlannedQuantity = group.Sum(GetPurchaseQuantity),
                PurchasedQuantity = 0m
            };
            ApplyCreateAudit(detail);

            foreach (var orderDetail in group)
            {
                var relation = new PurchasePlanOrderRelation
                {
                    Id = Guid.NewGuid(),
                    PurchasePlanDetailId = detail.Id,
                    SaleOrderId = order.Id,
                    SaleOrderDetailId = orderDetail.Id,
                    RequiredQuantity = GetPurchaseQuantity(orderDetail)
                };
                ApplyCreateAudit(relation);
                detail.OrderRelations.Add(relation);
            }

            plan.Details.Add(detail);
        }

        return plan;
    }

    /// <summary>
    /// 取订单明细的采购数量，优先使用基础单位数量，缺失时回退为下单数量。
    /// </summary>
    /// <param name="detail">销售订单商品明细。</param>
    /// <returns>按采购单位计量的数量。</returns>
    private static decimal GetPurchaseQuantity(SaleOrderDetail detail)
    {
        return detail.BaseUnitId.HasValue ? detail.BaseQuantity : detail.Quantity;
    }

    /// <summary>
    /// 构建手工新增的采购计划明细，校验采购单位归属并补齐快照。
    /// </summary>
    /// <param name="planId">所属采购计划主键。</param>
    /// <param name="detailDto">采购计划明细创建请求。</param>
    /// <returns>待持久化的采购计划明细实体。</returns>
    private async Task<PurchasePlanDetail> BuildManualDetailAsync(Guid planId, CreatePurchasePlanDetailDto detailDto)
    {
        var goods = await goodsRepository.GetByIdAsync(detailDto.GoodsId)
                    ?? throw new BusinessException("商品不存在");
        var purchaseUnit = await goodsUnitRepository.GetByIdAsync(detailDto.PurchaseUnitId);
        if (purchaseUnit is null || purchaseUnit.GoodsId != goods.Id)
        {
            throw new BusinessException($"采购单位不属于商品 {goods.Name}");
        }

        var plannedQuantity = detailDto.PlannedQuantity;
        return new PurchasePlanDetail
        {
            Id = Guid.NewGuid(),
            PurchasePlanId = planId,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            PurchaseUnitId = purchaseUnit.Id,
            PurchaseUnitNameSnapshot = purchaseUnit.Name,
            RequiredQuantity = detailDto.RequiredQuantity ?? plannedQuantity,
            PlannedQuantity = plannedQuantity,
            PurchasedQuantity = 0m,
            Remark = NormalizeRemark(detailDto.Remark)
        };
    }

    /// <summary>
    /// 校验供应商存在并写入名称快照，未指定时清空供应商信息。
    /// </summary>
    /// <param name="plan">目标采购计划。</param>
    /// <param name="supplierId">供应商主键，可为空。</param>
    private async Task ApplySupplierAsync(PurchasePlan plan, Guid? supplierId)
    {
        if (!supplierId.HasValue)
        {
            plan.SupplierId = null;
            plan.SupplierNameSnapshot = null;
            return;
        }

        var supplier = await supplierRepository.GetByIdAsync(supplierId.Value)
                       ?? throw new BusinessException("供应商不存在");
        plan.SupplierId = supplier.Id;
        plan.SupplierNameSnapshot = supplier.Name;
    }

    /// <summary>
    /// 校验采购员存在并写入名称快照，未指定时清空采购员信息。
    /// </summary>
    /// <param name="plan">目标采购计划。</param>
    /// <param name="purchaserId">采购员主键，可为空。</param>
    private async Task ApplyPurchaserAsync(PurchasePlan plan, Guid? purchaserId)
    {
        if (!purchaserId.HasValue)
        {
            plan.PurchaserId = null;
            plan.PurchaserNameSnapshot = null;
            return;
        }

        var purchaser = await purchaserRepository.GetByIdAsync(purchaserId.Value)
                        ?? throw new BusinessException("采购员不存在");
        plan.PurchaserId = purchaser.Id;
        plan.PurchaserNameSnapshot = purchaser.Name;
    }

    /// <summary>
    /// 判断订单当前状态是否为审核通过（已进入履约流程且未被驳回）。
    /// </summary>
    /// <param name="status">销售订单状态。</param>
    /// <returns>已审核通过返回 <c>true</c>。</returns>
    private static bool IsApproved(SaleOrderStatus status)
    {
        return status is not SaleOrderStatus.PendingAudit and not SaleOrderStatus.Rejected;
    }

    private async Task<PurchasePlan> GetRequiredPlanAsync(Guid id)
    {
        return await purchasePlanRepository.GetByIdAsync(id)
               ?? throw new NotFoundException("采购计划不存在");
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }

    private async Task<string> GeneratePlanNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var planNo = GeneratePlanNoValue();
            if (!await purchasePlanRepository.ExistsPlanNoAsync(planNo))
            {
                return planNo;
            }
        }

        throw new BusinessException("采购计划编号生成失败，请重试");
    }

    private static string GeneratePlanNoValue()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        return $"PP{DateTime.UtcNow:yyyyMMddHHmmssfff}{suffix}";
    }

    private static string? NormalizeRemark(string? remark)
    {
        return string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
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
}
