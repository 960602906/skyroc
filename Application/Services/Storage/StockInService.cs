using Application.DTOs.Storage;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Goods;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using GoodsEntity = Domain.Entities.Goods.Goods;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 入库应用服务，实现采购、其他和销售退货入库的事务化维护，
/// 并在审核和反审核时原子更新库存批次并只追加库存流水。
/// </summary>
public class StockInService(
    IStockInOrderRepository stockInOrderRepository,
    IStockBatchRepository stockBatchRepository,
    IStockLedgerRepository stockLedgerRepository,
    IWareRepository wareRepository,
    ISupplierRepository supplierRepository,
    IPurchaserRepository purchaserRepository,
    ICustomerRepository customerRepository,
    IDepartmentRepository departmentRepository,
    IPurchaseOrderRepository purchaseOrderRepository,
    IGoodsRepository goodsRepository,
    IGoodsUnitRepository goodsUnitRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreatePurchaseStockInDto> createPurchaseValidator,
    IValidator<UpdatePurchaseStockInDto> updatePurchaseValidator,
    IValidator<CreateOtherStockInDto> createOtherValidator,
    IValidator<UpdateOtherStockInDto> updateOtherValidator,
    IValidator<CreateSalesReturnStockInDto> createSalesReturnValidator,
    IValidator<UpdateSalesReturnStockInDto> updateSalesReturnValidator,
    ILogger<StockInService> logger) : IStockInService
{
    private const decimal QuantityTolerance = 0.000001m;
    private const decimal MoneyTolerance = 0.0001m;

    /// <inheritdoc />
    public async Task<PagedResult<StockInOrderDto>> GetPagedAsync(
        StockInOrderType orderType,
        StockInOrderQueryParameters parameters)
    {
        var result = await stockInOrderRepository.GetPagedAsync(
            parameters.QueryBuild(orderType),
            parameters.Current,
            parameters.Size,
            x => x.InTime,
            true);
        return mapper.ToPagedResult<StockInOrder, StockInOrderDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<StockInOrderDto> GetByIdAsync(StockInOrderType orderType, Guid id)
    {
        return mapper.Map<StockInOrderDto>(await GetRequiredOrderAsync(orderType, id));
    }

    /// <inheritdoc />
    public async Task<StockInOrderDto> CreatePurchaseAsync(CreatePurchaseStockInDto dto)
    {
        await ValidateAsync(createPurchaseValidator, dto);
        var order = await BuildBaseOrderAsync(StockInOrderType.Purchase, dto.WareId, dto.InTime, dto.Remark);
        order.ExpectedArrivalTime = dto.ExpectedArrivalTime;
        order.PurchasePattern = dto.PurchasePattern;
        await ApplyPurchaseOrderSourceAsync(order, dto.PurchaseOrderId);
        await ApplySupplierAsync(order, dto.SupplierId);
        await ApplyPurchaserAsync(order, dto.PurchaserId);
        await ApplyDepartmentAsync(order, dto.DepartmentId);
        await BuildDetailsAsync(order, dto.Details);
        await ValidateSourceRelationshipsAsync(order, lockPurchaseOrder: false);
        return await PersistNewOrderAsync(order);
    }

    /// <inheritdoc />
    public async Task<StockInOrderDto> UpdatePurchaseAsync(UpdatePurchaseStockInDto dto)
    {
        await ValidateAsync(updatePurchaseValidator, dto);
        var order = await GetRequiredOrderAsync(StockInOrderType.Purchase, dto.Id);
        EnsureEditable(order, "编辑");
        var details = await PrepareDetailsAsync(order, dto.Details);

        await ExecuteInTransactionAsync(async () =>
        {
            await ApplyWareAsync(order, dto.WareId);
            order.InTime = dto.InTime;
            order.ExpectedArrivalTime = dto.ExpectedArrivalTime;
            order.PurchasePattern = dto.PurchasePattern;
            order.Remark = Normalize(dto.Remark);
            await ApplyPurchaseOrderSourceAsync(order, dto.PurchaseOrderId);
            await ApplySupplierAsync(order, dto.SupplierId);
            await ApplyPurchaserAsync(order, dto.PurchaserId);
            await ApplyDepartmentAsync(order, dto.DepartmentId);
            SynchronizeDetails(order, details);
            await ValidateSourceRelationshipsAsync(order, lockPurchaseOrder: false);
            ApplyUpdateAudit(order);
            await stockInOrderRepository.UpdateAsync(order);
        });

        logger.LogInformation("采购入库单更新成功: {StockInOrderId}, {InNo}", order.Id, order.InNo);
        return mapper.Map<StockInOrderDto>(await GetRequiredOrderAsync(StockInOrderType.Purchase, order.Id));
    }

    /// <inheritdoc />
    public async Task<StockInOrderDto> CreateOtherAsync(CreateOtherStockInDto dto)
    {
        await ValidateAsync(createOtherValidator, dto);
        var order = await BuildBaseOrderAsync(StockInOrderType.Other, dto.WareId, dto.InTime, dto.Remark);
        await ApplyDepartmentAsync(order, dto.DepartmentId);
        await BuildDetailsAsync(order, dto.Details);
        await ValidateSourceRelationshipsAsync(order, lockPurchaseOrder: false);
        return await PersistNewOrderAsync(order);
    }

    /// <inheritdoc />
    public async Task<StockInOrderDto> UpdateOtherAsync(UpdateOtherStockInDto dto)
    {
        await ValidateAsync(updateOtherValidator, dto);
        var order = await GetRequiredOrderAsync(StockInOrderType.Other, dto.Id);
        EnsureEditable(order, "编辑");
        var details = await PrepareDetailsAsync(order, dto.Details);

        await ExecuteInTransactionAsync(async () =>
        {
            await ApplyWareAsync(order, dto.WareId);
            order.InTime = dto.InTime;
            order.Remark = Normalize(dto.Remark);
            await ApplyDepartmentAsync(order, dto.DepartmentId);
            SynchronizeDetails(order, details);
            await ValidateSourceRelationshipsAsync(order, lockPurchaseOrder: false);
            ApplyUpdateAudit(order);
            await stockInOrderRepository.UpdateAsync(order);
        });

        logger.LogInformation("其他入库单更新成功: {StockInOrderId}, {InNo}", order.Id, order.InNo);
        return mapper.Map<StockInOrderDto>(await GetRequiredOrderAsync(StockInOrderType.Other, order.Id));
    }

    /// <inheritdoc />
    public async Task<StockInOrderDto> CreateSalesReturnAsync(CreateSalesReturnStockInDto dto)
    {
        await ValidateAsync(createSalesReturnValidator, dto);
        var order = await BuildBaseOrderAsync(StockInOrderType.SalesReturn, dto.WareId, dto.InTime, dto.Remark);
        await ApplyCustomerAsync(order, dto.CustomerId);
        await ApplyDepartmentAsync(order, dto.DepartmentId);
        await BuildDetailsAsync(order, dto.Details);
        await ValidateSourceRelationshipsAsync(order, lockPurchaseOrder: false);
        return await PersistNewOrderAsync(order);
    }

    /// <inheritdoc />
    public async Task<StockInOrderDto> UpdateSalesReturnAsync(UpdateSalesReturnStockInDto dto)
    {
        await ValidateAsync(updateSalesReturnValidator, dto);
        var order = await GetRequiredOrderAsync(StockInOrderType.SalesReturn, dto.Id);
        EnsureEditable(order, "编辑");
        var details = await PrepareDetailsAsync(order, dto.Details);

        await ExecuteInTransactionAsync(async () =>
        {
            await ApplyWareAsync(order, dto.WareId);
            order.InTime = dto.InTime;
            order.Remark = Normalize(dto.Remark);
            await ApplyCustomerAsync(order, dto.CustomerId);
            await ApplyDepartmentAsync(order, dto.DepartmentId);
            SynchronizeDetails(order, details);
            await ValidateSourceRelationshipsAsync(order, lockPurchaseOrder: false);
            ApplyUpdateAudit(order);
            await stockInOrderRepository.UpdateAsync(order);
        });

        logger.LogInformation("销售退货入库单更新成功: {StockInOrderId}, {InNo}", order.Id, order.InNo);
        return mapper.Map<StockInOrderDto>(await GetRequiredOrderAsync(StockInOrderType.SalesReturn, order.Id));
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(StockInOrderType orderType, Guid id)
    {
        var order = await GetRequiredOrderAsync(orderType, id);
        EnsureEditable(order, "删除");
        await ExecuteInTransactionAsync(async () => await stockInOrderRepository.DeleteAsync(order));
        logger.LogInformation("入库单删除成功: {StockInOrderId}, {InNo}", order.Id, order.InNo);
        return true;
    }

    /// <inheritdoc />
    public async Task<StockInOrderDto> AuditAsync(StockInOrderType orderType, Guid id, string? remark)
    {
        var auditTime = DateTime.UtcNow;
        var normalizedRemark = Normalize(remark);
        StockInOrder? auditedOrder = null;
        await ExecuteInTransactionAsync(async () =>
        {
            var order = await GetRequiredOrderForUpdateAsync(orderType, id);
            if (order.BusinessStatus is not (StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit))
            {
                throw new BusinessException($"入库单 {order.InNo} 当前状态不允许审核");
            }

            if (order.Details.Count == 0)
            {
                throw new BusinessException($"入库单 {order.InNo} 没有商品明细，无法审核");
            }

            await ValidateSourceRelationshipsAsync(order, lockPurchaseOrder: true);
            var batchCache = new Dictionary<(Guid GoodsId, string BatchNo), StockBatch>();
            foreach (var detail in order.Details
                         .OrderBy(detail => detail.GoodsId)
                         .ThenBy(detail => detail.BatchNo, StringComparer.Ordinal))
            {
                var batch = await ResolveBatchForInboundAsync(order, detail, auditTime, batchCache);
                ApplyInboundToBatch(batch, detail, auditTime);
                detail.StockBatchId = batch.Id;
                ApplyUpdateAudit(detail);
                await stockLedgerRepository.AddAsync(
                    CreateInboundLedger(order, detail, batch, auditTime, normalizedRemark));
            }

            order.BusinessStatus = StockDocumentStatus.Audited;
            order.AuditUserId = currentUserService.GetUserId();
            order.AuditUserNameSnapshot = currentUserService.GetUserName();
            order.AuditTime = auditTime;
            ApplyUpdateAudit(order);
            await stockInOrderRepository.UpdateAsync(order);
            auditedOrder = order;
        });

        var completedOrder = auditedOrder!;
        logger.LogInformation("入库单审核入库成功: {StockInOrderId}, {InNo}", completedOrder.Id, completedOrder.InNo);
        return mapper.Map<StockInOrderDto>(await GetRequiredOrderAsync(orderType, completedOrder.Id));
    }

    /// <inheritdoc />
    public async Task<StockInOrderDto> ReverseAuditAsync(StockInOrderType orderType, Guid id, string? remark)
    {
        var reverseTime = DateTime.UtcNow;
        var normalizedRemark = Normalize(remark);
        StockInOrder? reversedOrder = null;
        await ExecuteInTransactionAsync(async () =>
        {
            var order = await GetRequiredOrderForUpdateAsync(orderType, id);
            if (order.BusinessStatus != StockDocumentStatus.Audited)
            {
                throw new BusinessException($"入库单 {order.InNo} 未处于已审核状态，无法反审核");
            }

            var activeLedgers = await stockLedgerRepository.GetActiveBySourceOrderAsync(order.Id);
            if (activeLedgers.Count == 0)
            {
                throw new BusinessException($"入库单 {order.InNo} 没有可回滚的库存流水");
            }

            foreach (var ledger in activeLedgers
                         .OrderBy(ledger => ledger.GoodsId)
                         .ThenBy(ledger => ledger.BatchNoSnapshot, StringComparer.Ordinal))
            {
                var batch = await stockBatchRepository.GetByIdentityForUpdateAsync(
                    ledger.WareId,
                    ledger.GoodsId,
                    ledger.BatchNoSnapshot);
                if (batch is null || batch.Id != ledger.StockBatchId)
                {
                    throw new BusinessException("库存批次不存在，无法反审核");
                }

                if (batch.AvailableQuantity + QuantityTolerance < ledger.ChangeQuantity
                    || batch.CurrentQuantity + QuantityTolerance < ledger.ChangeQuantity)
                {
                    throw new BusinessException(
                        $"批次 {batch.BatchNo} 可用库存不足，入库数量已被出库或占用，无法反审核");
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
            await stockInOrderRepository.UpdateAsync(order);
            reversedOrder = order;
        });

        var completedOrder = reversedOrder!;
        logger.LogInformation("入库单反审核成功: {StockInOrderId}, {InNo}", completedOrder.Id, completedOrder.InNo);
        return mapper.Map<StockInOrderDto>(await GetRequiredOrderAsync(orderType, completedOrder.Id));
    }

    private async Task<StockInOrder> BuildBaseOrderAsync(
        StockInOrderType orderType,
        Guid wareId,
        DateTime inTime,
        string? remark)
    {
        var order = new StockInOrder
        {
            Id = Guid.NewGuid(),
            InNo = await GenerateInNoAsync(),
            OrderType = orderType,
            BusinessStatus = StockDocumentStatus.Draft,
            InTime = inTime,
            Remark = Normalize(remark)
        };
        await ApplyWareAsync(order, wareId);
        ApplyCreateAudit(order);
        return order;
    }

    private async Task<StockInOrderDto> PersistNewOrderAsync(StockInOrder order)
    {
        await ExecuteInTransactionAsync(async () => await stockInOrderRepository.AddAsync(order));
        logger.LogInformation("入库单创建成功: {StockInOrderId}, {InNo}, {OrderType}", order.Id, order.InNo, order.OrderType);
        return mapper.Map<StockInOrderDto>(await GetRequiredOrderAsync(order.OrderType, order.Id));
    }

    private async Task BuildDetailsAsync(StockInOrder order, IEnumerable<CreateStockInDetailDto> details)
    {
        foreach (var detailDto in details)
        {
            var prepared = await PrepareDetailAsync(detailDto);
            var detail = CreateDetail(order.Id, prepared);
            ApplyCreateAudit(detail);
            order.Details.Add(detail);
        }

        RecalculateTotals(order);
    }

    private async Task<List<PreparedDetail>> PrepareDetailsAsync(
        StockInOrder order,
        IReadOnlyCollection<UpdateStockInDetailDto> details)
    {
        var existingIds = order.Details.Select(x => x.Id).ToHashSet();
        var requestedIds = details
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToList();
        if (requestedIds.Count != requestedIds.Distinct().Count())
        {
            throw new BusinessException("入库商品行主键不能重复");
        }

        if (requestedIds.Any(idValue => !existingIds.Contains(idValue)))
        {
            throw new BusinessException("入库商品行不属于当前入库单");
        }

        var prepared = new List<PreparedDetail>(details.Count);
        foreach (var detailDto in details)
        {
            prepared.Add(await PrepareDetailAsync(detailDto, detailDto.Id));
        }

        return prepared;
    }

    private async Task<PreparedDetail> PrepareDetailAsync(CreateStockInDetailDto dto, Guid? existingId = null)
    {
        var goods = await goodsRepository.GetByIdAsync(dto.GoodsId)
                    ?? throw new BusinessException("商品不存在");
        var unit = await goodsUnitRepository.GetByIdAsync(dto.GoodsUnitId);
        if (unit is null || unit.GoodsId != goods.Id)
        {
            throw new BusinessException($"入库单位不属于商品 {goods.Name}");
        }

        if (unit.ConversionRate <= 0m)
        {
            throw new BusinessException($"商品 {goods.Name} 的入库单位换算比例必须大于零");
        }

        var baseUnit = goods.BaseUnit
                       ?? throw new BusinessException($"商品 {goods.Name} 未配置基础单位");
        return new PreparedDetail(dto, existingId, goods, unit, baseUnit);
    }

    private static StockInDetail CreateDetail(Guid orderId, PreparedDetail prepared)
    {
        var detail = new StockInDetail
        {
            Id = Guid.NewGuid(),
            StockInOrderId = orderId
        };
        ApplyDetailValues(detail, prepared);
        return detail;
    }

    private static void ApplyDetailValues(StockInDetail detail, PreparedDetail prepared)
    {
        var dto = prepared.Dto;
        var baseQuantity = RoundQuantity(dto.Quantity * prepared.Unit.ConversionRate);
        detail.PurchaseOrderDetailId = dto.PurchaseOrderDetailId;
        detail.GoodsId = prepared.Goods.Id;
        detail.GoodsNameSnapshot = prepared.Goods.Name;
        detail.GoodsCodeSnapshot = prepared.Goods.Code;
        detail.GoodsUnitId = prepared.Unit.Id;
        detail.GoodsUnitNameSnapshot = prepared.Unit.Name;
        detail.ConversionRate = prepared.Unit.ConversionRate;
        detail.Quantity = dto.Quantity;
        detail.BaseQuantity = baseQuantity;
        detail.UnitPrice = dto.UnitPrice;
        detail.TotalPrice = RoundMoney(dto.Quantity * dto.UnitPrice);
        detail.BatchNo = dto.BatchNo.Trim();
        detail.ProductDate = dto.ProductDate;
        detail.ExpireDate = dto.ExpireDate;
        detail.Remark = Normalize(dto.Remark);
    }

    private void SynchronizeDetails(StockInOrder order, IReadOnlyCollection<PreparedDetail> preparedDetails)
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

    private async Task<StockBatch> ResolveBatchForInboundAsync(
        StockInOrder order,
        StockInDetail detail,
        DateTime auditTime,
        IDictionary<(Guid GoodsId, string BatchNo), StockBatch> cache)
    {
        var key = (detail.GoodsId, detail.BatchNo);
        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var existing = await stockBatchRepository.GetByIdentityForUpdateAsync(
            order.WareId,
            detail.GoodsId,
            detail.BatchNo);
        if (existing is not null)
        {
            cache[key] = existing;
            return existing;
        }

        var goods = detail.Goods ?? await goodsRepository.GetByIdAsync(detail.GoodsId)
            ?? throw new BusinessException("商品不存在");
        var baseUnit = goods.BaseUnit
                       ?? throw new BusinessException($"商品 {goods.Name} 未配置基础单位");
        var batch = new StockBatch
        {
            Id = Guid.NewGuid(),
            WareId = order.WareId,
            GoodsId = detail.GoodsId,
            GoodsNameSnapshot = detail.GoodsNameSnapshot,
            GoodsCodeSnapshot = detail.GoodsCodeSnapshot,
            BatchNo = detail.BatchNo,
            BaseUnitId = baseUnit.Id,
            BaseUnitNameSnapshot = baseUnit.Name,
            CurrentQuantity = 0m,
            AvailableQuantity = 0m,
            UnitCost = 0m,
            ProductDate = detail.ProductDate,
            ExpireDate = detail.ExpireDate,
            LastMovementTime = auditTime
        };
        ApplyCreateAudit(batch);
        await stockBatchRepository.AddAsync(batch);
        cache[key] = batch;
        return batch;
    }

    private void ApplyInboundToBatch(StockBatch batch, StockInDetail detail, DateTime auditTime)
    {
        var inboundQuantity = detail.BaseQuantity;
        var inboundUnitCost = UnitCostPerBase(detail);
        var newQuantity = RoundQuantity(batch.CurrentQuantity + inboundQuantity);
        if (newQuantity > QuantityTolerance)
        {
            var totalCost = batch.CurrentQuantity * batch.UnitCost + inboundQuantity * inboundUnitCost;
            batch.UnitCost = RoundMoney(totalCost / newQuantity);
        }

        batch.CurrentQuantity = newQuantity;
        batch.AvailableQuantity = RoundQuantity(batch.AvailableQuantity + inboundQuantity);
        if (batch.ProductDate is null && detail.ProductDate is not null)
        {
            batch.ProductDate = detail.ProductDate;
        }

        if (batch.ExpireDate is null && detail.ExpireDate is not null)
        {
            batch.ExpireDate = detail.ExpireDate;
        }

        batch.LastMovementTime = auditTime;
        ApplyUpdateAudit(batch);
    }

    private void ApplyReversalToBatch(StockBatch batch, StockLedger source, DateTime reverseTime)
    {
        var remainingQuantity = RoundQuantity(batch.CurrentQuantity - source.ChangeQuantity);
        var remainingAvailableQuantity = RoundQuantity(batch.AvailableQuantity - source.ChangeQuantity);
        var remainingInventoryCost = RoundMoney(
            batch.CurrentQuantity * batch.UnitCost - source.TotalCost);
        if (remainingInventoryCost < -MoneyTolerance)
        {
            throw new BusinessException(
                $"批次 {batch.BatchNo} 剩余库存成本不足，入库成本已被后续业务消耗，无法反审核");
        }

        batch.CurrentQuantity = remainingQuantity;
        batch.AvailableQuantity = remainingAvailableQuantity;
        batch.UnitCost = remainingQuantity <= QuantityTolerance
            ? 0m
            : RoundMoney(Math.Max(remainingInventoryCost, 0m) / remainingQuantity);
        batch.LastMovementTime = reverseTime;
        ApplyUpdateAudit(batch);
    }

    private StockLedger CreateInboundLedger(
        StockInOrder order,
        StockInDetail detail,
        StockBatch batch,
        DateTime auditTime,
        string? remark)
    {
        var unitCost = UnitCostPerBase(detail);
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
            Direction = StockLedgerDirection.Increase,
            SourceType = ResolveSourceType(order.OrderType),
            SourceOrderId = order.Id,
            SourceDetailId = detail.Id,
            ChangeQuantity = detail.BaseQuantity,
            BalanceQuantity = batch.CurrentQuantity,
            UnitCost = unitCost,
            TotalCost = RoundMoney(detail.BaseQuantity * unitCost),
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
            Direction = StockLedgerDirection.Decrease,
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

    private static decimal UnitCostPerBase(StockInDetail detail)
    {
        return RoundMoney(detail.UnitPrice / detail.ConversionRate);
    }

    private static StockLedgerSourceType ResolveSourceType(StockInOrderType orderType)
    {
        return orderType switch
        {
            StockInOrderType.Purchase => StockLedgerSourceType.PurchaseInbound,
            StockInOrderType.Other => StockLedgerSourceType.OtherInbound,
            StockInOrderType.SalesReturn => StockLedgerSourceType.SalesReturnInbound,
            _ => throw new BusinessException("未知的入库业务类型")
        };
    }

    private static void RecalculateTotals(StockInOrder order)
    {
        order.TotalBaseQuantity = RoundQuantity(order.Details.Sum(x => x.BaseQuantity));
        order.TotalAmount = RoundMoney(order.Details.Sum(x => x.TotalPrice));
    }

    private async Task ApplyWareAsync(StockInOrder order, Guid wareId)
    {
        var ware = await wareRepository.GetByIdAsync(wareId)
                   ?? throw new BusinessException("仓库不存在");
        order.WareId = ware.Id;
        order.WareNameSnapshot = ware.Name;
    }

    private async Task ApplyPurchaseOrderSourceAsync(StockInOrder order, Guid? purchaseOrderId)
    {
        if (!purchaseOrderId.HasValue)
        {
            order.PurchaseOrderId = null;
            return;
        }

        var purchaseOrder = await purchaseOrderRepository.GetByIdAsync(purchaseOrderId.Value)
                            ?? throw new BusinessException("来源采购单不存在");
        order.PurchaseOrderId = purchaseOrder.Id;
        if (!order.SupplierId.HasValue && purchaseOrder.SupplierId.HasValue)
        {
            order.SupplierId = purchaseOrder.SupplierId;
            order.SupplierNameSnapshot = purchaseOrder.SupplierNameSnapshot;
        }

        if (!order.PurchaserId.HasValue && purchaseOrder.PurchaserId.HasValue)
        {
            order.PurchaserId = purchaseOrder.PurchaserId;
            order.PurchaserNameSnapshot = purchaseOrder.PurchaserNameSnapshot;
        }
    }

    private async Task ValidateSourceRelationshipsAsync(StockInOrder order, bool lockPurchaseOrder)
    {
        if (order.OrderType != StockInOrderType.Purchase)
        {
            if (order.PurchaseOrderId.HasValue
                || order.Details.Any(detail => detail.PurchaseOrderDetailId.HasValue))
            {
                throw new BusinessException("非采购入库不能关联采购单或采购单商品明细");
            }

            return;
        }

        if (!order.PurchaseOrderId.HasValue)
        {
            if (order.Details.Any(detail => detail.PurchaseOrderDetailId.HasValue))
            {
                throw new BusinessException("关联采购单商品明细前必须选择来源采购单");
            }

            return;
        }

        var purchaseOrder = lockPurchaseOrder
            ? await purchaseOrderRepository.GetByIdForUpdateAsync(order.PurchaseOrderId.Value)
            : await purchaseOrderRepository.GetByIdAsync(order.PurchaseOrderId.Value);
        if (purchaseOrder is null)
        {
            throw new BusinessException("来源采购单不存在");
        }

        if (purchaseOrder.BusinessStatus != PurchaseOrderStatus.Completed)
        {
            throw new BusinessException($"采购单 {purchaseOrder.PurchaseNo} 未完成或已取消，不能办理入库");
        }

        if (purchaseOrder.PurchasePattern != order.PurchasePattern)
        {
            throw new BusinessException("入库采购模式必须与来源采购单一致");
        }

        if (purchaseOrder.SupplierId.HasValue && purchaseOrder.SupplierId != order.SupplierId)
        {
            throw new BusinessException("入库供应商必须与来源采购单一致");
        }

        if (purchaseOrder.PurchaserId.HasValue && purchaseOrder.PurchaserId != order.PurchaserId)
        {
            throw new BusinessException("入库采购员必须与来源采购单一致");
        }

        if (order.Details.Any(detail => !detail.PurchaseOrderDetailId.HasValue))
        {
            throw new BusinessException("关联来源采购单时，每个入库商品行都必须选择采购单商品明细");
        }

        var sourceDetails = purchaseOrder.Details.ToDictionary(detail => detail.Id);
        foreach (var detail in order.Details)
        {
            if (!sourceDetails.TryGetValue(detail.PurchaseOrderDetailId!.Value, out var sourceDetail))
            {
                throw new BusinessException("入库商品行不属于当前来源采购单");
            }

            if (sourceDetail.GoodsId != detail.GoodsId)
            {
                throw new BusinessException($"入库商品 {detail.GoodsNameSnapshot} 与来源采购明细商品不一致");
            }

            if (sourceDetail.PurchaseUnit.ConversionRate <= 0m)
            {
                throw new BusinessException($"采购单商品 {detail.GoodsNameSnapshot} 的单位换算比例必须大于零");
            }
        }

        var sourceDetailIds = sourceDetails.Keys.ToArray();
        var receivedQuantities = await stockInOrderRepository.GetReceivedBaseQuantitiesAsync(
            sourceDetailIds,
            order.Id);
        foreach (var requestedGroup in order.Details.GroupBy(detail => detail.PurchaseOrderDetailId!.Value))
        {
            var sourceDetail = sourceDetails[requestedGroup.Key];
            var orderedBaseQuantity = RoundQuantity(
                sourceDetail.PurchaseQuantity * sourceDetail.PurchaseUnit.ConversionRate);
            var receivedBaseQuantity = receivedQuantities.GetValueOrDefault(requestedGroup.Key);
            var requestedBaseQuantity = RoundQuantity(requestedGroup.Sum(detail => detail.BaseQuantity));
            if (receivedBaseQuantity + requestedBaseQuantity > orderedBaseQuantity + QuantityTolerance)
            {
                throw new BusinessException(
                    $"采购单商品 {sourceDetail.GoodsNameSnapshot} 入库数量超过剩余可入库数量");
            }
        }
    }

    private async Task ApplySupplierAsync(StockInOrder order, Guid? supplierId)
    {
        if (!supplierId.HasValue)
        {
            order.SupplierId = null;
            order.SupplierNameSnapshot = null;
            return;
        }

        var supplier = await supplierRepository.GetByIdAsync(supplierId.Value)
                       ?? throw new BusinessException("供应商不存在");
        order.SupplierId = supplier.Id;
        order.SupplierNameSnapshot = supplier.Name;
    }

    private async Task ApplyPurchaserAsync(StockInOrder order, Guid? purchaserId)
    {
        if (!purchaserId.HasValue)
        {
            order.PurchaserId = null;
            order.PurchaserNameSnapshot = null;
            return;
        }

        var purchaser = await purchaserRepository.GetByIdAsync(purchaserId.Value)
                        ?? throw new BusinessException("采购员不存在");
        order.PurchaserId = purchaser.Id;
        order.PurchaserNameSnapshot = purchaser.Name;
    }

    private async Task ApplyCustomerAsync(StockInOrder order, Guid customerId)
    {
        var customer = await customerRepository.GetByIdAsync(customerId)
                       ?? throw new BusinessException("客户不存在");
        order.CustomerId = customer.Id;
        order.CustomerNameSnapshot = customer.Name;
    }

    private async Task ApplyDepartmentAsync(StockInOrder order, Guid? departmentId)
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

    private static void EnsureEditable(StockInOrder order, string operation)
    {
        if (order.BusinessStatus is not (StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit))
        {
            throw new BusinessException($"入库单 {order.InNo} 已审核或已反审核，不能{operation}");
        }
    }

    private async Task<StockInOrder> GetRequiredOrderAsync(StockInOrderType orderType, Guid id)
    {
        var order = await stockInOrderRepository.GetByIdAsync(id);
        if (order is null || order.OrderType != orderType)
        {
            throw new NotFoundException("入库单不存在");
        }

        return order;
    }

    private async Task<StockInOrder> GetRequiredOrderForUpdateAsync(StockInOrderType orderType, Guid id)
    {
        var order = await stockInOrderRepository.GetByIdForUpdateAsync(id);
        if (order is null || order.OrderType != orderType)
        {
            throw new NotFoundException("入库单不存在");
        }

        return order;
    }

    private async Task<string> GenerateInNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            var inNo = $"IN{DateTime.UtcNow:yyyyMMddHHmmssfff}{suffix}";
            if (!await stockInOrderRepository.ExistsInNoAsync(inNo))
            {
                return inNo;
            }
        }

        throw new BusinessException("入库单编号生成失败，请重试");
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

    private static decimal RoundQuantity(decimal quantity)
    {
        return decimal.Round(quantity, 6, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundMoney(decimal money)
    {
        return decimal.Round(money, 4, MidpointRounding.AwayFromZero);
    }

    private sealed record PreparedDetail(
        CreateStockInDetailDto Dto,
        Guid? ExistingId,
        GoodsEntity Goods,
        GoodsUnit Unit,
        GoodsUnit BaseUnit);
}
