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
using static Shared.Constants.NumericPrecision;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 采购计划应用服务，实现查询、生成、分配、合并与拆分，并保证计划变更处于未发布状态。
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
    IDocumentNoGenerator documentNoGenerator,
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
        await createValidator.ValidateOrThrowAsync(dto);

        var plan = new PurchasePlan
        {
            Id = Guid.NewGuid(),
            PlanNo = await NextPlanNoAsync(),
            PlanDate = dto.PlanDate,
            PurchasePattern = dto.PurchasePattern,
            PurchaseStatus = PurchasePlanStatus.Unpublished,
            Remark = NormalizeRemark(dto.Remark)
        };
        await ApplySupplierAsync(plan, dto.SupplierId);
        await ApplyPurchaserAsync(plan, dto.PurchaserId);
        plan.ApplyCreateAudit(currentUserService);

        foreach (var detailDto in dto.Details)
        {
            var detail = await BuildManualDetailAsync(plan.Id, detailDto);
            detail.ApplyCreateAudit(currentUserService);
            plan.Details.Add(detail);
        }

        await unitOfWork.ExecuteInTransactionAsync(async () => await purchasePlanRepository.AddAsync(plan));

        logger.LogInformation("采购计划手工创建成功: {PlanId}, {PlanNo}", plan.Id, plan.PlanNo);
        return mapper.Map<PurchasePlanDto>(await GetRequiredPlanAsync(plan.Id));
    }

    /// <inheritdoc />
    public async Task<List<PurchasePlanDto>> GenerateFromOrdersAsync(GeneratePurchasePlanFromOrdersDto dto)
    {
        await generateValidator.ValidateOrThrowAsync(dto);

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
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var order in orders)
            {
                var plan = await BuildPlanFromOrderAsync(order, remark);
                createdPlanIds.Add(plan.Id);
                await purchasePlanRepository.AddAsync(plan);

                // 订单经 GetByIdAsync 加载后处于跟踪状态，直接改写标记即可随事务保存。
                order.HasPurchasePlan = true;
                order.ApplyUpdateAudit(currentUserService);
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

    /// <inheritdoc />
    public async Task<List<PurchasePlanDto>> AssignSupplierAsync(AssignPurchasePlanSupplierDto dto)
    {
        var plans = await GetMutablePlansAsync(dto.PlanIds, "分配供应商");
        Supplier? supplier = null;
        if (dto.SupplierId.HasValue)
        {
            supplier = await supplierRepository.GetByIdAsync(dto.SupplierId.Value)
                       ?? throw new BusinessException("供应商不存在");
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var plan in plans)
            {
                plan.SupplierId = supplier?.Id;
                plan.SupplierNameSnapshot = supplier?.Name;
                plan.ApplyUpdateAudit(currentUserService);
                await purchasePlanRepository.UpdateAsync(plan);
            }
        });

        logger.LogInformation("采购计划供应商分配成功: {PlanCount} 张计划, {SupplierId}", plans.Count, supplier?.Id);
        return await MapPlansAsync(plans.Select(x => x.Id));
    }

    /// <inheritdoc />
    public async Task<List<PurchasePlanDto>> AssignPurchaserAsync(AssignPurchasePlanPurchaserDto dto)
    {
        var plans = await GetMutablePlansAsync(dto.PlanIds, "分配采购员");
        Purchaser? purchaser = null;
        if (dto.PurchaserId.HasValue)
        {
            purchaser = await purchaserRepository.GetByIdAsync(dto.PurchaserId.Value)
                        ?? throw new BusinessException("采购员不存在");
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var plan in plans)
            {
                plan.PurchaserId = purchaser?.Id;
                plan.PurchaserNameSnapshot = purchaser?.Name;
                plan.ApplyUpdateAudit(currentUserService);
                await purchasePlanRepository.UpdateAsync(plan);
            }
        });

        logger.LogInformation("采购计划采购员分配成功: {PlanCount} 张计划, {PurchaserId}", plans.Count, purchaser?.Id);
        return await MapPlansAsync(plans.Select(x => x.Id));
    }

    /// <inheritdoc />
    public async Task<PurchasePlanDto> MergeAsync(MergePurchasePlansDto dto)
    {
        var planIds = NormalizeIds(dto.PlanIds, "待合并采购计划");
        if (planIds.Count < 2)
        {
            throw new BusinessException("合并采购计划至少需要两张不同计划");
        }

        var plans = await GetMutablePlansAsync(planIds, "合并");
        var first = plans[0];
        if (plans.Any(plan => plan.PurchasePattern != first.PurchasePattern))
        {
            throw new BusinessException("采购模式不同的计划不能合并");
        }

        if (plans.Any(plan => plan.SupplierId != first.SupplierId))
        {
            throw new BusinessException("供应商不同的计划不能合并");
        }

        if (plans.Any(plan => plan.PurchaserId != first.PurchaserId))
        {
            throw new BusinessException("采购员不同的计划不能合并");
        }

        var mergedPlan = await CreateDerivedPlanAsync(first, plans.Min(x => x.PlanDate), dto.Remark);
        foreach (var group in plans
                     .SelectMany(plan => plan.Details)
                     .GroupBy(detail => new { detail.GoodsId, detail.PurchaseUnitId }))
        {
            var sample = group.First();
            var mergedDetail = CreateDerivedDetail(
                mergedPlan.Id,
                sample,
                group.Sum(x => x.RequiredQuantity),
                group.Sum(x => x.PlannedQuantity));

            foreach (var relationGroup in group
                         .SelectMany(detail => detail.OrderRelations)
                         .GroupBy(relation => new { relation.SaleOrderId, relation.SaleOrderDetailId }))
            {
                var relationSample = relationGroup.First();
                mergedDetail.OrderRelations.Add(CreateDerivedRelation(
                    mergedDetail.Id,
                    relationSample,
                    relationGroup.Sum(x => x.RequiredQuantity)));
            }

            mergedPlan.Details.Add(mergedDetail);
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await purchasePlanRepository.AddAsync(mergedPlan);
            await purchasePlanRepository.DeleteRangeAsync(plans);
        });

        logger.LogInformation("采购计划合并成功: {SourceCount} 张计划合并为 {PlanId}", plans.Count, mergedPlan.Id);
        return mapper.Map<PurchasePlanDto>(await GetRequiredPlanAsync(mergedPlan.Id));
    }

    /// <inheritdoc />
    public async Task<List<SplittablePurchasePlanOrderDto>> GetSplittableOrdersAsync(Guid planId)
    {
        var plan = await GetRequiredPlanAsync(planId);
        ValidateMutablePlan(plan, "拆分");
        return plan.Details
            .SelectMany(detail => detail.OrderRelations)
            .GroupBy(relation => new { relation.SaleOrderId, relation.SaleOrder.OrderNo })
            .Select(group => new SplittablePurchasePlanOrderDto
            {
                SaleOrderId = group.Key.SaleOrderId,
                SaleOrderNo = group.Key.OrderNo,
                RequiredQuantity = group.Sum(x => x.RequiredQuantity)
            })
            .OrderBy(x => x.SaleOrderNo, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<PurchasePlanDto> SplitByOrdersAsync(SplitPurchasePlanByOrdersDto dto)
    {
        var saleOrderIds = NormalizeIds(dto.SaleOrderIds, "待拆分来源订单");
        var selectedOrderIds = saleOrderIds.ToHashSet();
        var sourcePlan = await GetRequiredPlanAsync(dto.PlanId);
        ValidateMutablePlan(sourcePlan, "按订单拆分");

        var availableOrderIds = sourcePlan.Details
            .SelectMany(detail => detail.OrderRelations)
            .Select(relation => relation.SaleOrderId)
            .ToHashSet();
        var missingOrderIds = saleOrderIds.Where(id => !availableOrderIds.Contains(id)).ToList();
        if (missingOrderIds.Count > 0)
        {
            throw new BusinessException($"采购计划不包含来源订单: {string.Join(", ", missingOrderIds)}");
        }

        var splitValues = sourcePlan.Details
            .Select(detail =>
            {
                var relations = detail.OrderRelations
                    .Where(relation => selectedOrderIds.Contains(relation.SaleOrderId))
                    .ToList();
                var requiredQuantity = relations.Sum(x => x.RequiredQuantity);
                var plannedQuantity = detail.RequiredQuantity > 0m
                    ? RoundQuantity(detail.PlannedQuantity * requiredQuantity / detail.RequiredQuantity)
                    : 0m;
                return new { Detail = detail, Relations = relations, RequiredQuantity = requiredQuantity, PlannedQuantity = plannedQuantity };
            })
            .Where(x => x.Relations.Count > 0)
            .ToList();

        if (splitValues.Count == 0)
        {
            throw new BusinessException("所选订单没有可拆分的采购计划数量");
        }

        if (sourcePlan.Details.All(detail =>
                detail.PlannedQuantity - (splitValues.FirstOrDefault(x => x.Detail.Id == detail.Id)?.PlannedQuantity ?? 0m) <= 0m))
        {
            throw new BusinessException("拆分后原采购计划必须保留至少一条有效商品明细");
        }

        var splitPlan = await CreateDerivedPlanAsync(sourcePlan, sourcePlan.PlanDate, dto.Remark ?? sourcePlan.Remark);
        foreach (var value in splitValues)
        {
            var splitDetail = CreateDerivedDetail(
                splitPlan.Id,
                value.Detail,
                value.RequiredQuantity,
                value.PlannedQuantity);
            foreach (var relation in value.Relations)
            {
                splitDetail.OrderRelations.Add(CreateDerivedRelation(splitDetail.Id, relation, relation.RequiredQuantity));
                value.Detail.OrderRelations.Remove(relation);
            }

            value.Detail.RequiredQuantity = RoundQuantity(value.Detail.RequiredQuantity - value.RequiredQuantity);
            value.Detail.PlannedQuantity = RoundQuantity(value.Detail.PlannedQuantity - value.PlannedQuantity);
            value.Detail.ApplyUpdateAudit(currentUserService);
            splitPlan.Details.Add(splitDetail);
        }

        RemoveEmptyDetails(sourcePlan);
        sourcePlan.ApplyUpdateAudit(currentUserService);
        await PersistSplitAsync(sourcePlan, splitPlan);

        logger.LogInformation(
            "采购计划按订单拆分成功: {SourcePlanId} -> {SplitPlanId}, {OrderCount} 个订单",
            sourcePlan.Id,
            splitPlan.Id,
            saleOrderIds.Count);
        return mapper.Map<PurchasePlanDto>(await GetRequiredPlanAsync(splitPlan.Id));
    }

    /// <inheritdoc />
    public async Task<PurchasePlanDto> SplitByQuantityAsync(SplitPurchasePlanByQuantityDto dto)
    {
        if (dto.Details is null || dto.Details.Count == 0)
        {
            throw new BusinessException("至少需要一条商品数量拆分项");
        }

        if (dto.Details.Any(item => item.DetailId == Guid.Empty || item.Quantity <= 0m))
        {
            throw new BusinessException("拆分明细主键和拆分数量必须有效");
        }

        if (dto.Details.Select(item => item.DetailId).Distinct().Count() != dto.Details.Count)
        {
            throw new BusinessException("同一采购计划明细不能重复拆分");
        }

        var sourcePlan = await GetRequiredPlanAsync(dto.PlanId);
        ValidateMutablePlan(sourcePlan, "按商品数量拆分");
        var detailById = sourcePlan.Details.ToDictionary(detail => detail.Id);
        foreach (var item in dto.Details)
        {
            if (!detailById.TryGetValue(item.DetailId, out var detail))
            {
                throw new BusinessException($"采购计划不包含商品明细: {item.DetailId}");
            }

            if (item.Quantity > detail.PlannedQuantity)
            {
                throw new BusinessException($"商品 {detail.GoodsNameSnapshot} 的拆分数量不能超过计划数量");
            }
        }

        var splitQuantityByDetail = dto.Details.ToDictionary(item => item.DetailId, item => item.Quantity);
        if (sourcePlan.Details.All(detail =>
                detail.PlannedQuantity - splitQuantityByDetail.GetValueOrDefault(detail.Id) <= 0m))
        {
            throw new BusinessException("拆分后原采购计划必须保留至少一条有效商品明细");
        }

        var splitPlan = await CreateDerivedPlanAsync(sourcePlan, sourcePlan.PlanDate, dto.Remark ?? sourcePlan.Remark);
        foreach (var item in dto.Details)
        {
            var sourceDetail = detailById[item.DetailId];
            var ratio = item.Quantity / sourceDetail.PlannedQuantity;
            var splitRequiredQuantity = ratio == 1m
                ? sourceDetail.RequiredQuantity
                : RoundQuantity(sourceDetail.RequiredQuantity * ratio);
            var splitDetail = CreateDerivedDetail(
                splitPlan.Id,
                sourceDetail,
                splitRequiredQuantity,
                item.Quantity);

            foreach (var relation in sourceDetail.OrderRelations.ToList())
            {
                var relationQuantity = ratio == 1m
                    ? relation.RequiredQuantity
                    : RoundQuantity(relation.RequiredQuantity * ratio);
                if (relationQuantity <= 0m)
                {
                    continue;
                }

                splitDetail.OrderRelations.Add(CreateDerivedRelation(splitDetail.Id, relation, relationQuantity));
                relation.RequiredQuantity = RoundQuantity(relation.RequiredQuantity - relationQuantity);
                if (relation.RequiredQuantity <= 0m)
                {
                    sourceDetail.OrderRelations.Remove(relation);
                }
                else
                {
                    relation.ApplyUpdateAudit(currentUserService);
                }
            }

            sourceDetail.RequiredQuantity = RoundQuantity(sourceDetail.RequiredQuantity - splitRequiredQuantity);
            sourceDetail.PlannedQuantity = RoundQuantity(sourceDetail.PlannedQuantity - item.Quantity);
            sourceDetail.ApplyUpdateAudit(currentUserService);
            splitPlan.Details.Add(splitDetail);
        }

        RemoveEmptyDetails(sourcePlan);
        sourcePlan.ApplyUpdateAudit(currentUserService);
        await PersistSplitAsync(sourcePlan, splitPlan);

        logger.LogInformation(
            "采购计划按商品数量拆分成功: {SourcePlanId} -> {SplitPlanId}, {DetailCount} 条明细",
            sourcePlan.Id,
            splitPlan.Id,
            dto.Details.Count);
        return mapper.Map<PurchasePlanDto>(await GetRequiredPlanAsync(splitPlan.Id));
    }

    /// <summary>
    /// 持久化原计划数量调整与新拆分计划，确保两侧变更在同一事务提交。
    /// </summary>
    private async Task PersistSplitAsync(PurchasePlan sourcePlan, PurchasePlan splitPlan)
    {
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await purchasePlanRepository.UpdateAsync(sourcePlan);
            await purchasePlanRepository.AddAsync(splitPlan);
        });
    }

    /// <summary>
    /// 创建继承采购模式和责任方快照的新计划，计划编号始终重新生成。
    /// </summary>
    private async Task<PurchasePlan> CreateDerivedPlanAsync(PurchasePlan source, DateTime planDate, string? remark)
    {
        var plan = new PurchasePlan
        {
            Id = Guid.NewGuid(),
            PlanNo = await NextPlanNoAsync(),
            PlanDate = planDate,
            PurchasePattern = source.PurchasePattern,
            PurchaseStatus = PurchasePlanStatus.Unpublished,
            SupplierId = source.SupplierId,
            SupplierNameSnapshot = source.SupplierNameSnapshot,
            PurchaserId = source.PurchaserId,
            PurchaserNameSnapshot = source.PurchaserNameSnapshot,
            Remark = NormalizeRemark(remark)
        };
        plan.ApplyCreateAudit(currentUserService);
        return plan;
    }

    /// <summary>
    /// 按来源商品和单位快照创建派生计划明细。
    /// </summary>
    private PurchasePlanDetail CreateDerivedDetail(
        Guid planId,
        PurchasePlanDetail source,
        decimal requiredQuantity,
        decimal plannedQuantity)
    {
        var detail = new PurchasePlanDetail
        {
            Id = Guid.NewGuid(),
            PurchasePlanId = planId,
            GoodsId = source.GoodsId,
            GoodsNameSnapshot = source.GoodsNameSnapshot,
            GoodsCodeSnapshot = source.GoodsCodeSnapshot,
            PurchaseUnitId = source.PurchaseUnitId,
            PurchaseUnitNameSnapshot = source.PurchaseUnitNameSnapshot,
            RequiredQuantity = RoundQuantity(requiredQuantity),
            PlannedQuantity = RoundQuantity(plannedQuantity),
            PurchasedQuantity = 0m,
            Remark = source.Remark
        };
        detail.ApplyCreateAudit(currentUserService);
        return detail;
    }

    /// <summary>
    /// 复制来源订单关系到派生计划明细，并保留按采购单位计量的需求数量。
    /// </summary>
    private PurchasePlanOrderRelation CreateDerivedRelation(
        Guid detailId,
        PurchasePlanOrderRelation source,
        decimal requiredQuantity)
    {
        var relation = new PurchasePlanOrderRelation
        {
            Id = Guid.NewGuid(),
            PurchasePlanDetailId = detailId,
            SaleOrderId = source.SaleOrderId,
            SaleOrderDetailId = source.SaleOrderDetailId,
            RequiredQuantity = RoundQuantity(requiredQuantity)
        };
        relation.ApplyCreateAudit(currentUserService);
        return relation;
    }

    /// <summary>
    /// 删除拆分后数量归零且不再包含订单来源的空明细。
    /// </summary>
    private static void RemoveEmptyDetails(PurchasePlan plan)
    {
        foreach (var detail in plan.Details
                     .Where(detail => detail.PlannedQuantity <= 0m
                                      && detail.RequiredQuantity <= 0m
                                      && detail.OrderRelations.Count == 0)
                     .ToList())
        {
            plan.Details.Remove(detail);
        }
    }

    /// <summary>
    /// 批量加载采购计划并校验仅未发布且未产生采购数量的计划可变更。
    /// </summary>
    private async Task<List<PurchasePlan>> GetMutablePlansAsync(IEnumerable<Guid>? ids, string operation)
    {
        var normalizedIds = NormalizeIds(ids, "采购计划");
        var plans = new List<PurchasePlan>(normalizedIds.Count);
        foreach (var id in normalizedIds)
        {
            var plan = await GetRequiredPlanAsync(id);
            ValidateMutablePlan(plan, operation);
            plans.Add(plan);
        }

        return plans;
    }

    /// <summary>
    /// 校验采购计划仍处于未发布且所有明细尚未生成采购单的可编辑状态。
    /// </summary>
    private static void ValidateMutablePlan(PurchasePlan plan, string operation)
    {
        if (plan.PurchaseStatus != PurchasePlanStatus.Unpublished
            || plan.Details.Any(detail => detail.PurchasedQuantity > 0m))
        {
            throw new BusinessException($"采购计划 {plan.PlanNo} 已生成采购单，不能{operation}");
        }
    }

    /// <summary>
    /// 清理并校验业务主键集合，拒绝空主键和空请求。
    /// </summary>
    private static List<Guid> NormalizeIds(IEnumerable<Guid>? ids, string fieldName)
    {
        var normalizedIds = ids?.Distinct().ToList() ?? [];
        if (normalizedIds.Count == 0 || normalizedIds.Any(id => id == Guid.Empty))
        {
            throw new BusinessException($"{fieldName}不能为空且必须有效");
        }

        return normalizedIds;
    }

    /// <summary>
    /// 按请求顺序重新加载并映射采购计划集合。
    /// </summary>
    private async Task<List<PurchasePlanDto>> MapPlansAsync(IEnumerable<Guid> ids)
    {
        var results = new List<PurchasePlanDto>();
        foreach (var id in ids)
        {
            results.Add(mapper.Map<PurchasePlanDto>(await GetRequiredPlanAsync(id)));
        }

        return results;
    }

    /// <summary>
    /// 将采购数量统一保留到数据库定义的六位小数精度。
    /// </summary>
    /// <summary>
    /// 根据一张已审核订单构建采购计划，按商品聚合明细并保留订单来源关系。
    /// </summary>
    /// <param name="order">已审核通过的销售订单。</param>
    /// <param name="remark">写入采购计划的备注。</param>
    /// <returns>待持久化的采购计划实体。</returns>
    private async Task<PurchasePlan> BuildPlanFromOrderAsync(SaleOrder order, string? remark)
    {
        var plan = new PurchasePlan
        {
            Id = Guid.NewGuid(),
            PlanNo = await NextPlanNoAsync(),
            PlanDate = order.ReceiveDate ?? order.OrderDate,
            PurchasePattern = PurchasePattern.SupplierDirect,
            PurchaseStatus = PurchasePlanStatus.Unpublished,
            Remark = remark
        };
        plan.ApplyCreateAudit(currentUserService);

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
            detail.ApplyCreateAudit(currentUserService);

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
                relation.ApplyCreateAudit(currentUserService);
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


    private Task<string> NextPlanNoAsync()
    {
        return documentNoGenerator.NextAsync(
            DocumentNoKind.PurchasePlan,
            no => purchasePlanRepository.ExistsPlanNoAsync(no));
    }

    private static string? NormalizeRemark(string? remark)
    {
        return string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
    }


}
