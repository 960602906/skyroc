using Application.DTOs.Storage;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Storage;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using static Shared.Constants.NumericPrecision;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 出库应用服务，实现销售、采购退货和其他出库的事务化维护，
/// 并在审核和反审核时原子扣减或恢复库存批次且只追加库存流水。
/// </summary>
public class StockOutService(
    IStockOutOrderRepository stockOutOrderRepository,
    IStockBatchRepository stockBatchRepository,
    IStockLedgerRepository stockLedgerRepository,
    IWareRepository wareRepository,
    ICustomerRepository customerRepository,
    ISupplierRepository supplierRepository,
    IDepartmentRepository departmentRepository,
    ISaleOrderRepository saleOrderRepository,
    IGoodsUnitRepository goodsUnitRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateSaleStockOutDto> createSaleValidator,
    IValidator<UpdateSaleStockOutDto> updateSaleValidator,
    IValidator<CreatePurchaseReturnStockOutDto> createPurchaseReturnValidator,
    IValidator<UpdatePurchaseReturnStockOutDto> updatePurchaseReturnValidator,
    IValidator<CreateOtherStockOutDto> createOtherValidator,
    IValidator<UpdateOtherStockOutDto> updateOtherValidator,
    ILogger<StockOutService> logger) : IStockOutService
{
    private const decimal QuantityTolerance = 0.000001m;

    /// <inheritdoc />
    public async Task<PagedResult<StockOutOrderDto>> GetPagedAsync(
        StockOutOrderType orderType,
        StockOutOrderQueryParameters parameters)
    {
        var result = await stockOutOrderRepository.GetPagedAsync(
            parameters.QueryBuild(orderType),
            parameters.Current,
            parameters.Size,
            x => x.OutTime,
            true);
        return mapper.ToPagedResult<StockOutOrder, StockOutOrderDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<StockOutOrderDto> GetByIdAsync(StockOutOrderType orderType, Guid id)
    {
        return mapper.Map<StockOutOrderDto>(await GetRequiredOrderAsync(orderType, id));
    }

    /// <inheritdoc />
    public async Task<StockOutOrderDto> CreateSaleAsync(CreateSaleStockOutDto dto)
    {
        await ValidateAsync(createSaleValidator, dto);
        var order = await BuildBaseOrderAsync(StockOutOrderType.Sale, dto.WareId, dto.OutTime, dto.Remark);
        await ApplySaleOrderSourceAsync(order, dto.SaleOrderId);
        await ApplyCustomerAsync(order, dto.CustomerId);
        await ApplyDepartmentAsync(order, dto.DepartmentId);
        await BuildDetailsAsync(order, dto.Details);
        await ValidateSourceRelationshipsAsync(order, lockSaleOrder: false);
        return await PersistNewOrderAsync(order);
    }

    /// <inheritdoc />
    public async Task<StockOutOrderDto> UpdateSaleAsync(UpdateSaleStockOutDto dto)
    {
        await ValidateAsync(updateSaleValidator, dto);
        StockOutOrder? updatedOrder = null;
        await ExecuteInTransactionAsync(async () =>
        {
            var order = await GetRequiredOrderForUpdateAsync(StockOutOrderType.Sale, dto.Id);
            EnsureEditable(order, "编辑");
            var details = await PrepareDetailsAsync(order, dto.Details, dto.WareId);
            await ApplyWareAsync(order, dto.WareId);
            order.OutTime = dto.OutTime;
            order.Remark = Normalize(dto.Remark);
            await ApplySaleOrderSourceAsync(order, dto.SaleOrderId);
            await ApplyCustomerAsync(order, dto.CustomerId);
            await ApplyDepartmentAsync(order, dto.DepartmentId);
            SynchronizeDetails(order, details);
            await ValidateSourceRelationshipsAsync(order, lockSaleOrder: false);
            ApplyUpdateAudit(order);
            await stockOutOrderRepository.UpdateAsync(order);
            updatedOrder = order;
        });

        var completedOrder = updatedOrder!;
        logger.LogInformation("销售出库单更新成功: {StockOutOrderId}, {OutNo}", completedOrder.Id, completedOrder.OutNo);
        return mapper.Map<StockOutOrderDto>(
            await GetRequiredOrderAsync(StockOutOrderType.Sale, completedOrder.Id));
    }

    /// <inheritdoc />
    public async Task<StockOutOrderDto> CreatePurchaseReturnAsync(CreatePurchaseReturnStockOutDto dto)
    {
        await ValidateAsync(createPurchaseReturnValidator, dto);
        var order = await BuildBaseOrderAsync(
            StockOutOrderType.PurchaseReturn,
            dto.WareId,
            dto.OutTime,
            dto.Remark);
        await ApplySupplierAsync(order, dto.SupplierId);
        await ApplyDepartmentAsync(order, dto.DepartmentId);
        await BuildDetailsAsync(order, dto.Details);
        await ValidateSourceRelationshipsAsync(order, lockSaleOrder: false);
        return await PersistNewOrderAsync(order);
    }

    /// <inheritdoc />
    public async Task<StockOutOrderDto> UpdatePurchaseReturnAsync(UpdatePurchaseReturnStockOutDto dto)
    {
        await ValidateAsync(updatePurchaseReturnValidator, dto);
        StockOutOrder? updatedOrder = null;
        await ExecuteInTransactionAsync(async () =>
        {
            var order = await GetRequiredOrderForUpdateAsync(StockOutOrderType.PurchaseReturn, dto.Id);
            EnsureEditable(order, "编辑");
            var details = await PrepareDetailsAsync(order, dto.Details, dto.WareId);
            await ApplyWareAsync(order, dto.WareId);
            order.OutTime = dto.OutTime;
            order.Remark = Normalize(dto.Remark);
            await ApplySupplierAsync(order, dto.SupplierId);
            await ApplyDepartmentAsync(order, dto.DepartmentId);
            SynchronizeDetails(order, details);
            await ValidateSourceRelationshipsAsync(order, lockSaleOrder: false);
            ApplyUpdateAudit(order);
            await stockOutOrderRepository.UpdateAsync(order);
            updatedOrder = order;
        });

        var completedOrder = updatedOrder!;
        logger.LogInformation(
            "采购退货出库单更新成功: {StockOutOrderId}, {OutNo}",
            completedOrder.Id,
            completedOrder.OutNo);
        return mapper.Map<StockOutOrderDto>(
            await GetRequiredOrderAsync(StockOutOrderType.PurchaseReturn, completedOrder.Id));
    }

    /// <inheritdoc />
    public async Task<StockOutOrderDto> CreateOtherAsync(CreateOtherStockOutDto dto)
    {
        await ValidateAsync(createOtherValidator, dto);
        var order = await BuildBaseOrderAsync(StockOutOrderType.Other, dto.WareId, dto.OutTime, dto.Remark);
        await ApplyDepartmentAsync(order, dto.DepartmentId);
        await BuildDetailsAsync(order, dto.Details);
        await ValidateSourceRelationshipsAsync(order, lockSaleOrder: false);
        return await PersistNewOrderAsync(order);
    }

    /// <inheritdoc />
    public async Task<StockOutOrderDto> UpdateOtherAsync(UpdateOtherStockOutDto dto)
    {
        await ValidateAsync(updateOtherValidator, dto);
        StockOutOrder? updatedOrder = null;
        await ExecuteInTransactionAsync(async () =>
        {
            var order = await GetRequiredOrderForUpdateAsync(StockOutOrderType.Other, dto.Id);
            EnsureEditable(order, "编辑");
            var details = await PrepareDetailsAsync(order, dto.Details, dto.WareId);
            await ApplyWareAsync(order, dto.WareId);
            order.OutTime = dto.OutTime;
            order.Remark = Normalize(dto.Remark);
            await ApplyDepartmentAsync(order, dto.DepartmentId);
            SynchronizeDetails(order, details);
            await ValidateSourceRelationshipsAsync(order, lockSaleOrder: false);
            ApplyUpdateAudit(order);
            await stockOutOrderRepository.UpdateAsync(order);
            updatedOrder = order;
        });

        var completedOrder = updatedOrder!;
        logger.LogInformation("其他出库单更新成功: {StockOutOrderId}, {OutNo}", completedOrder.Id, completedOrder.OutNo);
        return mapper.Map<StockOutOrderDto>(
            await GetRequiredOrderAsync(StockOutOrderType.Other, completedOrder.Id));
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(StockOutOrderType orderType, Guid id)
    {
        StockOutOrder? deletedOrder = null;
        await ExecuteInTransactionAsync(async () =>
        {
            var order = await GetRequiredOrderForUpdateAsync(orderType, id);
            EnsureEditable(order, "删除");
            await stockOutOrderRepository.DeleteAsync(order);
            deletedOrder = order;
        });

        var completedOrder = deletedOrder!;
        logger.LogInformation("出库单删除成功: {StockOutOrderId}, {OutNo}", completedOrder.Id, completedOrder.OutNo);
        return true;
    }

    /// <inheritdoc />
    public async Task<StockOutOrderDto> AuditAsync(StockOutOrderType orderType, Guid id, string? remark)
    {
        var auditTime = DateTime.UtcNow;
        var normalizedRemark = Normalize(remark);
        StockOutOrder? auditedOrder = null;
        await ExecuteInTransactionAsync(async () =>
        {
            var order = await GetRequiredOrderForUpdateAsync(orderType, id);
            if (order.BusinessStatus is not (StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit))
            {
                throw new BusinessException($"出库单 {order.OutNo} 当前状态不允许审核");
            }

            if (order.Details.Count == 0)
            {
                throw new BusinessException($"出库单 {order.OutNo} 没有商品明细，无法审核");
            }

            var saleOrder = await ValidateSourceRelationshipsAsync(order, lockSaleOrder: true);
            var batchCache = new Dictionary<Guid, StockBatch>();
            foreach (var detail in order.Details
                         .OrderBy(detail => detail.StockBatchId)
                         .ThenBy(detail => detail.Id))
            {
                var batch = await ResolveBatchForOutboundAsync(order, detail, batchCache);
                ApplyOutboundToBatch(batch, detail, auditTime);
                ApplyUpdateAudit(detail);
                await stockBatchRepository.UpdateAsync(batch);
                await stockLedgerRepository.AddAsync(
                    CreateOutboundLedger(order, detail, batch, auditTime, normalizedRemark));
            }

            order.BusinessStatus = StockDocumentStatus.Audited;
            order.AuditUserId = currentUserService.GetUserId();
            order.AuditUserNameSnapshot = currentUserService.GetUserName();
            order.AuditTime = auditTime;
            ApplyUpdateAudit(order);
            await stockOutOrderRepository.UpdateAsync(order);

            if (saleOrder is not null)
            {
                await RefreshSaleOrderOutboundStatusAsync(saleOrder, order.Id, order.Details, order.OutTime);
            }

            auditedOrder = order;
        });

        var completedOrder = auditedOrder!;
        logger.LogInformation("出库单审核扣减库存成功: {StockOutOrderId}, {OutNo}", completedOrder.Id, completedOrder.OutNo);
        return mapper.Map<StockOutOrderDto>(await GetRequiredOrderAsync(orderType, completedOrder.Id));
    }

    /// <inheritdoc />
    public async Task<StockOutOrderDto> ReverseAuditAsync(StockOutOrderType orderType, Guid id, string? remark)
    {
        var reverseTime = DateTime.UtcNow;
        var normalizedRemark = Normalize(remark);
        StockOutOrder? reversedOrder = null;
        await ExecuteInTransactionAsync(async () =>
        {
            var order = await GetRequiredOrderForUpdateAsync(orderType, id);
            if (order.BusinessStatus != StockDocumentStatus.Audited)
            {
                throw new BusinessException($"出库单 {order.OutNo} 未处于已审核状态，无法反审核");
            }

            SaleOrder? saleOrder = null;
            if (order.OrderType == StockOutOrderType.Sale && order.SaleOrderId.HasValue)
            {
                saleOrder = await saleOrderRepository.GetByIdForUpdateAsync(order.SaleOrderId.Value)
                            ?? throw new BusinessException("来源销售订单不存在，无法反审核");
            }

            var activeLedgers = await stockLedgerRepository.GetActiveBySourceOrderAsync(order.Id);
            if (activeLedgers.Count == 0)
            {
                throw new BusinessException($"出库单 {order.OutNo} 没有可回滚的库存流水");
            }

            foreach (var ledger in activeLedgers
                         .OrderBy(ledger => ledger.StockBatchId)
                         .ThenBy(ledger => ledger.Id))
            {
                if (ledger.Direction != StockLedgerDirection.Decrease)
                {
                    throw new BusinessException("出库单存在非扣减流水，无法反审核");
                }

                var batch = await stockBatchRepository.GetByIdentityForUpdateAsync(
                    ledger.WareId,
                    ledger.GoodsId,
                    ledger.BatchNoSnapshot);
                if (batch is null || batch.Id != ledger.StockBatchId)
                {
                    throw new BusinessException("库存批次不存在，无法反审核");
                }

                ApplyReversalToBatch(batch, ledger, reverseTime);
                await stockBatchRepository.UpdateAsync(batch);
                await stockLedgerRepository.AddAsync(
                    CreateReversalLedger(ledger, batch, reverseTime, normalizedRemark));
            }

            order.BusinessStatus = StockDocumentStatus.Reversed;
            order.ReverseUserId = currentUserService.GetUserId();
            order.ReverseUserNameSnapshot = currentUserService.GetUserName();
            order.ReverseTime = reverseTime;
            ApplyUpdateAudit(order);
            await stockOutOrderRepository.UpdateAsync(order);

            if (saleOrder is not null)
            {
                await RefreshSaleOrderOutboundStatusAsync(saleOrder, order.Id, [], null);
            }

            reversedOrder = order;
        });

        var completedOrder = reversedOrder!;
        logger.LogInformation("出库单反审核成功: {StockOutOrderId}, {OutNo}", completedOrder.Id, completedOrder.OutNo);
        return mapper.Map<StockOutOrderDto>(await GetRequiredOrderAsync(orderType, completedOrder.Id));
    }

    private async Task<StockOutOrder> BuildBaseOrderAsync(
        StockOutOrderType orderType,
        Guid wareId,
        DateTime outTime,
        string? remark)
    {
        var order = new StockOutOrder
        {
            Id = Guid.NewGuid(),
            OutNo = await GenerateOutNoAsync(),
            OrderType = orderType,
            BusinessStatus = StockDocumentStatus.Draft,
            OutTime = outTime,
            Remark = Normalize(remark)
        };
        await ApplyWareAsync(order, wareId);
        ApplyCreateAudit(order);
        return order;
    }

    private async Task<StockOutOrderDto> PersistNewOrderAsync(StockOutOrder order)
    {
        await ExecuteInTransactionAsync(async () => await stockOutOrderRepository.AddAsync(order));
        logger.LogInformation("出库单创建成功: {StockOutOrderId}, {OutNo}, {OrderType}", order.Id, order.OutNo, order.OrderType);
        return mapper.Map<StockOutOrderDto>(await GetRequiredOrderAsync(order.OrderType, order.Id));
    }

    private async Task BuildDetailsAsync(StockOutOrder order, IEnumerable<CreateStockOutDetailDto> details)
    {
        foreach (var detailDto in details)
        {
            var prepared = await PrepareDetailAsync(detailDto, order.WareId);
            var detail = CreateDetail(order.Id, prepared);
            ApplyCreateAudit(detail);
            order.Details.Add(detail);
        }

        RecalculateTotals(order);
    }

    private async Task<List<PreparedDetail>> PrepareDetailsAsync(
        StockOutOrder order,
        IReadOnlyCollection<UpdateStockOutDetailDto> details,
        Guid wareId)
    {
        var existingIds = order.Details.Select(x => x.Id).ToHashSet();
        var requestedIds = details.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToList();
        if (requestedIds.Count != requestedIds.Distinct().Count())
        {
            throw new BusinessException("出库商品行主键不能重复");
        }

        if (requestedIds.Any(idValue => !existingIds.Contains(idValue)))
        {
            throw new BusinessException("出库商品行不属于当前出库单");
        }

        var prepared = new List<PreparedDetail>(details.Count);
        foreach (var detailDto in details)
        {
            prepared.Add(await PrepareDetailAsync(detailDto, wareId, detailDto.Id));
        }

        return prepared;
    }

    private async Task<PreparedDetail> PrepareDetailAsync(
        CreateStockOutDetailDto dto,
        Guid wareId,
        Guid? existingId = null)
    {
        var batch = await stockBatchRepository.GetByIdAsync(dto.StockBatchId)
                    ?? throw new BusinessException("库存批次不存在");
        if (batch.WareId != wareId)
        {
            throw new BusinessException($"批次 {batch.BatchNo} 不属于当前出库仓库");
        }

        var unit = await goodsUnitRepository.GetByIdAsync(dto.GoodsUnitId);
        if (unit is null || unit.GoodsId != batch.GoodsId)
        {
            throw new BusinessException($"出库单位不属于批次商品 {batch.GoodsNameSnapshot}");
        }

        if (unit.ConversionRate <= 0m)
        {
            throw new BusinessException($"商品 {batch.GoodsNameSnapshot} 的出库单位换算比例必须大于零");
        }

        return new PreparedDetail(dto, existingId, batch, unit);
    }

    private static StockOutDetail CreateDetail(Guid orderId, PreparedDetail prepared)
    {
        var detail = new StockOutDetail
        {
            Id = Guid.NewGuid(),
            StockOutOrderId = orderId
        };
        ApplyDetailValues(detail, prepared);
        return detail;
    }

    private static void ApplyDetailValues(StockOutDetail detail, PreparedDetail prepared)
    {
        var dto = prepared.Dto;
        detail.SaleOrderDetailId = dto.SaleOrderDetailId;
        detail.StockBatchId = prepared.Batch.Id;
        detail.GoodsId = prepared.Batch.GoodsId;
        detail.GoodsNameSnapshot = prepared.Batch.GoodsNameSnapshot;
        detail.GoodsCodeSnapshot = prepared.Batch.GoodsCodeSnapshot;
        detail.GoodsUnitId = prepared.Unit.Id;
        detail.GoodsUnitNameSnapshot = prepared.Unit.Name;
        detail.ConversionRate = prepared.Unit.ConversionRate;
        detail.Quantity = dto.Quantity;
        detail.BaseQuantity = RoundQuantity(dto.Quantity * prepared.Unit.ConversionRate);
        detail.UnitPrice = dto.UnitPrice;
        detail.TotalPrice = RoundMoney(dto.Quantity * dto.UnitPrice);
        detail.BatchNoSnapshot = prepared.Batch.BatchNo;
        detail.Remark = Normalize(dto.Remark);
    }

    private void SynchronizeDetails(StockOutOrder order, IReadOnlyCollection<PreparedDetail> preparedDetails)
    {
        var existingById = order.Details.ToDictionary(x => x.Id);
        var retainedIds = preparedDetails
            .Where(x => x.ExistingId.HasValue)
            .Select(x => x.ExistingId!.Value)
            .ToHashSet();
        foreach (var removed in order.Details.Where(x => !retainedIds.Contains(x.Id)).ToList())
        {
            order.Details.Remove(removed);
        }

        foreach (var prepared in preparedDetails)
        {
            if (prepared.ExistingId.HasValue)
            {
                var detail = existingById[prepared.ExistingId.Value];
                ApplyDetailValues(detail, prepared);
                ApplyUpdateAudit(detail);
            }
            else
            {
                var detail = CreateDetail(order.Id, prepared);
                ApplyCreateAudit(detail);
                order.Details.Add(detail);
            }
        }

        RecalculateTotals(order);
    }

    private async Task<StockBatch> ResolveBatchForOutboundAsync(
        StockOutOrder order,
        StockOutDetail detail,
        IDictionary<Guid, StockBatch> cache)
    {
        if (!detail.StockBatchId.HasValue)
        {
            throw new BusinessException($"出库商品 {detail.GoodsNameSnapshot} 未选择库存批次");
        }

        if (cache.TryGetValue(detail.StockBatchId.Value, out var cached))
        {
            return cached;
        }

        var batch = await stockBatchRepository.GetByIdentityForUpdateAsync(
            order.WareId,
            detail.GoodsId,
            detail.BatchNoSnapshot);
        if (batch is null || batch.Id != detail.StockBatchId.Value)
        {
            throw new BusinessException($"库存批次 {detail.BatchNoSnapshot} 不存在或已变更");
        }

        cache[batch.Id] = batch;
        return batch;
    }

    private void ApplyOutboundToBatch(StockBatch batch, StockOutDetail detail, DateTime auditTime)
    {
        if (batch.AvailableQuantity + QuantityTolerance < detail.BaseQuantity
            || batch.CurrentQuantity + QuantityTolerance < detail.BaseQuantity)
        {
            throw new BusinessException(
                $"批次 {batch.BatchNo} 可用库存不足，需要 {detail.BaseQuantity}，可用 {batch.AvailableQuantity}");
        }

        batch.CurrentQuantity = RoundQuantity(batch.CurrentQuantity - detail.BaseQuantity);
        batch.AvailableQuantity = RoundQuantity(batch.AvailableQuantity - detail.BaseQuantity);
        batch.LastMovementTime = auditTime;
        ApplyUpdateAudit(batch);
    }

    private void ApplyReversalToBatch(StockBatch batch, StockLedger source, DateTime reverseTime)
    {
        batch.CurrentQuantity = RoundQuantity(batch.CurrentQuantity + source.ChangeQuantity);
        batch.AvailableQuantity = RoundQuantity(batch.AvailableQuantity + source.ChangeQuantity);
        batch.LastMovementTime = reverseTime;
        ApplyUpdateAudit(batch);
    }

    private StockLedger CreateOutboundLedger(
        StockOutOrder order,
        StockOutDetail detail,
        StockBatch batch,
        DateTime auditTime,
        string? remark)
    {
        var ledger = new StockLedger
        {
            Id = Guid.NewGuid(),
            StockBatchId = batch.Id,
            WareId = order.WareId,
            WareNameSnapshot = order.WareNameSnapshot,
            GoodsId = detail.GoodsId,
            GoodsNameSnapshot = detail.GoodsNameSnapshot,
            GoodsCodeSnapshot = detail.GoodsCodeSnapshot,
            BatchNoSnapshot = batch.BatchNo,
            BaseUnitNameSnapshot = batch.BaseUnitNameSnapshot,
            Direction = StockLedgerDirection.Decrease,
            SourceType = ResolveSourceType(order.OrderType),
            SourceOrderId = order.Id,
            SourceDetailId = detail.Id,
            ChangeQuantity = detail.BaseQuantity,
            BalanceQuantity = batch.CurrentQuantity,
            UnitCost = batch.UnitCost,
            TotalCost = RoundMoney(detail.BaseQuantity * batch.UnitCost),
            OccurredTime = auditTime,
            Remark = remark
        };
        ApplyCreateAudit(ledger);
        return ledger;
    }

    private StockLedger CreateReversalLedger(
        StockLedger source,
        StockBatch batch,
        DateTime reverseTime,
        string? remark)
    {
        var ledger = new StockLedger
        {
            Id = Guid.NewGuid(),
            StockBatchId = batch.Id,
            WareId = source.WareId,
            WareNameSnapshot = source.WareNameSnapshot,
            GoodsId = source.GoodsId,
            GoodsNameSnapshot = source.GoodsNameSnapshot,
            GoodsCodeSnapshot = source.GoodsCodeSnapshot,
            BatchNoSnapshot = source.BatchNoSnapshot,
            BaseUnitNameSnapshot = source.BaseUnitNameSnapshot,
            Direction = StockLedgerDirection.Increase,
            SourceType = source.SourceType,
            SourceOrderId = source.SourceOrderId,
            SourceDetailId = source.SourceDetailId,
            ChangeQuantity = source.ChangeQuantity,
            BalanceQuantity = batch.CurrentQuantity,
            UnitCost = source.UnitCost,
            TotalCost = source.TotalCost,
            OccurredTime = reverseTime,
            ReversedFromLedgerId = source.Id,
            Remark = remark
        };
        ApplyCreateAudit(ledger);
        return ledger;
    }

    private static StockLedgerSourceType ResolveSourceType(StockOutOrderType orderType)
    {
        return orderType switch
        {
            StockOutOrderType.Sale => StockLedgerSourceType.SalesOutbound,
            StockOutOrderType.PurchaseReturn => StockLedgerSourceType.PurchaseReturnOutbound,
            StockOutOrderType.Other => StockLedgerSourceType.OtherOutbound,
            _ => throw new BusinessException("未知的出库业务类型")
        };
    }

    private async Task<SaleOrder?> ValidateSourceRelationshipsAsync(
        StockOutOrder order,
        bool lockSaleOrder)
    {
        if (order.OrderType != StockOutOrderType.Sale)
        {
            if (order.SaleOrderId.HasValue
                || order.CustomerId.HasValue
                || order.Details.Any(detail => detail.SaleOrderDetailId.HasValue))
            {
                throw new BusinessException("非销售出库不能关联销售订单、客户或订单商品明细");
            }

            return null;
        }

        if (!order.CustomerId.HasValue)
        {
            throw new BusinessException("销售出库必须选择客户");
        }

        if (!order.SaleOrderId.HasValue)
        {
            if (order.Details.Any(detail => detail.SaleOrderDetailId.HasValue))
            {
                throw new BusinessException("关联销售订单商品明细前必须选择来源销售订单");
            }

            return null;
        }

        var sourceOrder = lockSaleOrder
            ? await saleOrderRepository.GetByIdForUpdateAsync(order.SaleOrderId.Value)
            : await saleOrderRepository.GetByIdAsync(order.SaleOrderId.Value);
        if (sourceOrder is null)
        {
            throw new BusinessException("来源销售订单不存在");
        }

        if (sourceOrder.OrderStatus is SaleOrderStatus.PendingAudit or SaleOrderStatus.Rejected)
        {
            throw new BusinessException($"销售订单 {sourceOrder.OrderNo} 未审核通过，不能办理出库");
        }

        if (sourceOrder.CustomerId != order.CustomerId)
        {
            throw new BusinessException("出库客户必须与来源销售订单一致");
        }

        if (sourceOrder.WareId.HasValue && sourceOrder.WareId != order.WareId)
        {
            throw new BusinessException("出库仓库必须与来源销售订单一致");
        }

        if (order.Details.Any(detail => !detail.SaleOrderDetailId.HasValue))
        {
            throw new BusinessException("关联来源销售订单时，每个出库商品行都必须选择订单商品明细");
        }

        var sourceDetails = sourceOrder.Details.ToDictionary(detail => detail.Id);
        foreach (var detail in order.Details)
        {
            if (!sourceDetails.TryGetValue(detail.SaleOrderDetailId!.Value, out var sourceDetail))
            {
                throw new BusinessException("出库商品行不属于当前来源销售订单");
            }

            if (sourceDetail.GoodsId != detail.GoodsId)
            {
                throw new BusinessException($"出库批次商品 {detail.GoodsNameSnapshot} 与来源订单商品不一致");
            }
        }

        var outboundQuantities = await stockOutOrderRepository.GetOutboundBaseQuantitiesAsync(
            sourceDetails.Keys.ToArray(),
            order.Id);
        foreach (var requestedGroup in order.Details.GroupBy(detail => detail.SaleOrderDetailId!.Value))
        {
            var sourceDetail = sourceDetails[requestedGroup.Key];
            var outboundBaseQuantity = outboundQuantities.GetValueOrDefault(requestedGroup.Key);
            var requestedBaseQuantity = RoundQuantity(requestedGroup.Sum(detail => detail.BaseQuantity));
            if (outboundBaseQuantity + requestedBaseQuantity > sourceDetail.BaseQuantity + QuantityTolerance)
            {
                throw new BusinessException(
                    $"销售订单商品 {sourceDetail.GoodsNameSnapshot} 出库数量超过剩余可出库数量");
            }
        }

        return sourceOrder;
    }

    private async Task RefreshSaleOrderOutboundStatusAsync(
        SaleOrder saleOrder,
        Guid excludeOrderId,
        IEnumerable<StockOutDetail> additionalDetails,
        DateTime? currentOutTime)
    {
        var sourceDetailIds = saleOrder.Details.Select(detail => detail.Id).ToArray();
        var quantities = (await stockOutOrderRepository.GetOutboundBaseQuantitiesAsync(
                sourceDetailIds,
                excludeOrderId))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        foreach (var group in additionalDetails
                     .Where(detail => detail.SaleOrderDetailId.HasValue)
                     .GroupBy(detail => detail.SaleOrderDetailId!.Value))
        {
            quantities[group.Key] = RoundQuantity(
                quantities.GetValueOrDefault(group.Key) + group.Sum(detail => detail.BaseQuantity));
        }

        var hasOutbound = quantities.Values.Any(quantity => quantity > QuantityTolerance);
        var allGenerated = saleOrder.Details.Count > 0
                           && saleOrder.Details.All(detail =>
                               quantities.GetValueOrDefault(detail.Id) + QuantityTolerance >= detail.BaseQuantity);
        var latestOutTime = await stockOutOrderRepository.GetLatestOutboundTimeAsync(
            saleOrder.Id,
            excludeOrderId);
        if (currentOutTime.HasValue
            && (!latestOutTime.HasValue || currentOutTime.Value > latestOutTime.Value))
        {
            latestOutTime = currentOutTime;
        }

        saleOrder.HasOutSale = hasOutbound;
        saleOrder.OutStorageStatus = !hasOutbound
            ? OrderOutStorageStatus.NotGenerated
            : allGenerated
                ? OrderOutStorageStatus.Generated
                : OrderOutStorageStatus.PartiallyGenerated;
        saleOrder.OutDate = hasOutbound ? latestOutTime : null;
        ApplyUpdateAudit(saleOrder);
        await saleOrderRepository.UpdateAsync(saleOrder);
    }

    private static void RecalculateTotals(StockOutOrder order)
    {
        order.TotalBaseQuantity = RoundQuantity(order.Details.Sum(x => x.BaseQuantity));
        order.TotalAmount = RoundMoney(order.Details.Sum(x => x.TotalPrice));
    }

    private async Task ApplyWareAsync(StockOutOrder order, Guid wareId)
    {
        var ware = await wareRepository.GetByIdAsync(wareId)
                   ?? throw new BusinessException("仓库不存在");
        order.WareId = ware.Id;
        order.WareNameSnapshot = ware.Name;
    }

    private async Task ApplySaleOrderSourceAsync(StockOutOrder order, Guid? saleOrderId)
    {
        if (!saleOrderId.HasValue)
        {
            order.SaleOrderId = null;
            return;
        }

        var sourceOrder = await saleOrderRepository.GetByIdAsync(saleOrderId.Value)
                          ?? throw new BusinessException("来源销售订单不存在");
        order.SaleOrderId = sourceOrder.Id;
    }

    private async Task ApplyCustomerAsync(StockOutOrder order, Guid customerId)
    {
        var customer = await customerRepository.GetByIdAsync(customerId)
                       ?? throw new BusinessException("客户不存在");
        order.CustomerId = customer.Id;
        order.CustomerNameSnapshot = customer.Name;
    }

    private async Task ApplySupplierAsync(StockOutOrder order, Guid supplierId)
    {
        var supplier = await supplierRepository.GetByIdAsync(supplierId)
                       ?? throw new BusinessException("供应商不存在");
        order.SupplierId = supplier.Id;
        order.SupplierNameSnapshot = supplier.Name;
    }

    private async Task ApplyDepartmentAsync(StockOutOrder order, Guid? departmentId)
    {
        if (!departmentId.HasValue)
        {
            order.DepartmentId = null;
            order.DepartmentNameSnapshot = null;
            return;
        }

        var department = await departmentRepository.GetByIdAsync(departmentId.Value)
                         ?? throw new BusinessException("部门不存在");
        order.DepartmentId = department.Id;
        order.DepartmentNameSnapshot = department.Name;
    }

    private static void EnsureEditable(StockOutOrder order, string operation)
    {
        if (order.BusinessStatus is not (StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit))
        {
            throw new BusinessException($"出库单 {order.OutNo} 已审核或已反审核，不能{operation}");
        }
    }

    private async Task<StockOutOrder> GetRequiredOrderAsync(StockOutOrderType orderType, Guid id)
    {
        var order = await stockOutOrderRepository.GetByIdAsync(id);
        if (order is null || order.OrderType != orderType)
        {
            throw new NotFoundException("出库单不存在");
        }

        return order;
    }

    private async Task<StockOutOrder> GetRequiredOrderForUpdateAsync(StockOutOrderType orderType, Guid id)
    {
        var order = await stockOutOrderRepository.GetByIdForUpdateAsync(id);
        if (order is null || order.OrderType != orderType)
        {
            throw new NotFoundException("出库单不存在");
        }

        return order;
    }

    private async Task<string> GenerateOutNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            var outNo = $"OUT{DateTime.UtcNow:yyyyMMddHHmmssfff}{suffix}";
            if (!await stockOutOrderRepository.ExistsOutNoAsync(outNo))
            {
                return outNo;
            }
        }

        throw new BusinessException("出库单编号生成失败，请重试");
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

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record PreparedDetail(
        CreateStockOutDetailDto Dto,
        Guid? ExistingId,
        StockBatch Batch,
        GoodsUnit Unit);
}
