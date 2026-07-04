namespace Domain.Entities.Storage;

/// <summary>
/// 出库业务类型，用于区分销售履约、采购退货和其他库存扣减。
/// </summary>
public enum StockOutOrderType
{
    /// <summary>
    /// 销售出库，来源于已审核销售订单。
    /// </summary>
    Sale = 1,

    /// <summary>
    /// 采购退货出库，将库存商品退还供应商。
    /// </summary>
    PurchaseReturn = 2,

    /// <summary>
    /// 其他出库，由授权人员手工扣减库存。
    /// </summary>
    Other = 3
}
