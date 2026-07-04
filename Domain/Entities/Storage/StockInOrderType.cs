namespace Domain.Entities.Storage;

/// <summary>
/// 入库业务类型，用于区分采购到货、手工调整和销售退货来源。
/// </summary>
public enum StockInOrderType
{
    /// <summary>
    /// 采购入库，来源于已确认的采购单。
    /// </summary>
    Purchase = 1,

    /// <summary>
    /// 其他入库，由授权人员手工增加库存。
    /// </summary>
    Other = 2,

    /// <summary>
    /// 销售退货入库，来源于客户退回商品。
    /// </summary>
    SalesReturn = 3
}
