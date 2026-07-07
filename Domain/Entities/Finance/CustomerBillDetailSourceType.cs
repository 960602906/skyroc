namespace Domain.Entities.Finance;

/// <summary>
/// 客户账单明细来源类型，区分订单签收形成的正向应收和售后完成形成的负向调整。
/// </summary>
public enum CustomerBillDetailSourceType
{
    /// <summary>
    /// 订单验收明细，表示客户签收后确认的商品应收金额。
    /// </summary>
    OrderAcceptance = 1,

    /// <summary>
    /// 售后调整明细，表示退款、减免或账单核算造成的应收冲减。
    /// </summary>
    AfterSaleAdjustment = 2
}
