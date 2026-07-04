namespace Domain.Entities.Storage;

/// <summary>
/// 库存流水业务来源，决定来源单据和明细主键的解释方式。
/// </summary>
public enum StockLedgerSourceType
{
    /// <summary>
    /// 采购入库。
    /// </summary>
    PurchaseInbound = 1,

    /// <summary>
    /// 其他入库。
    /// </summary>
    OtherInbound = 2,

    /// <summary>
    /// 销售退货入库。
    /// </summary>
    SalesReturnInbound = 3,

    /// <summary>
    /// 销售出库。
    /// </summary>
    SalesOutbound = 4,

    /// <summary>
    /// 采购退货出库。
    /// </summary>
    PurchaseReturnOutbound = 5,

    /// <summary>
    /// 其他出库。
    /// </summary>
    OtherOutbound = 6,

    /// <summary>
    /// 盘点差异调整。
    /// </summary>
    Stocktaking = 7
}
