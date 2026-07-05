namespace Domain.Entities.AfterSales;

/// <summary>
/// 售后商品申请类型，决定是否需要回收实物。
/// </summary>
public enum AfterSaleType
{
    /// <summary>
    /// 仅退款：不回收商品，仅调整客户应收或执行退款。
    /// </summary>
    RefundOnly = 1,

    /// <summary>
    /// 退货退款：需要回收商品，并在验收入库后调整金额。
    /// </summary>
    ReturnAndRefund = 2
}
