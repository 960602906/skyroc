using Application.Exceptions;
using Application.Extensions;
using Application.Interfaces;
using Domain.Entities.Storage;
using Domain.Interfaces;
using static Shared.Constants.NumericPrecision;

namespace Application.Services;

/// <summary>
///     入库场景的库存批次解析、移动加权成本与流水构造。
/// </summary>
public class InventoryCostingService(
    IStockBatchRepository stockBatchRepository,
    IGoodsRepository goodsRepository,
    ICurrentUserService currentUserService) : IInventoryCostingService
{
    private const decimal QuantityTolerance = 0.000001m;
    private const decimal MoneyTolerance = 0.0001m;

    /// <inheritdoc />
    public async Task<StockBatch> ResolveOrCreateBatchForInboundAsync(
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
        batch.ApplyCreateAudit(currentUserService);
        await stockBatchRepository.AddAsync(batch);
        cache[key] = batch;
        return batch;
    }

    /// <inheritdoc />
    public void ApplyInboundToBatch(StockBatch batch, StockInDetail detail, DateTime auditTime)
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
        batch.ApplyUpdateAudit(currentUserService);
    }

    /// <inheritdoc />
    public void ApplyReversalToBatch(StockBatch batch, StockLedger source, DateTime reverseTime)
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
        batch.ApplyUpdateAudit(currentUserService);
    }

    /// <inheritdoc />
    public StockLedger CreateInboundLedger(
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
        ledger.ApplyCreateAudit(currentUserService);
        return ledger;
    }

    /// <inheritdoc />
    public StockLedger CreateReversalLedger(
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
        ledger.ApplyCreateAudit(currentUserService);
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
}
