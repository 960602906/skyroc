using Domain.Entities.Storage;

namespace Application.Interfaces;

/// <summary>
///     库存批次与成本核算：入库解析/创建批次、移动加权、反审核回冲与流水构造。
/// </summary>
public interface IInventoryCostingService
{
    /// <summary>
    ///     按仓库+商品+批次号锁定或创建批次（同一审核事务内可用 cache 去重）。
    /// </summary>
    Task<StockBatch> ResolveOrCreateBatchForInboundAsync(
        StockInOrder order,
        StockInDetail detail,
        DateTime auditTime,
        IDictionary<(Guid GoodsId, string BatchNo), StockBatch> cache);

    /// <summary>
    ///     将入库明细数量与成本按移动加权计入批次。
    /// </summary>
    void ApplyInboundToBatch(StockBatch batch, StockInDetail detail, DateTime auditTime);

    /// <summary>
    ///     按原入库流水回冲批次数量与成本。
    /// </summary>
    void ApplyReversalToBatch(StockBatch batch, StockLedger source, DateTime reverseTime);

    /// <summary>
    ///     构造入库增加方向库存流水。
    /// </summary>
    StockLedger CreateInboundLedger(
        StockInOrder order,
        StockInDetail detail,
        StockBatch batch,
        DateTime auditTime,
        string? remark);

    /// <summary>
    ///     构造反审核减少方向库存流水。
    /// </summary>
    StockLedger CreateReversalLedger(
        StockLedger source,
        StockBatch batch,
        DateTime reverseTime,
        string? remark);
}
