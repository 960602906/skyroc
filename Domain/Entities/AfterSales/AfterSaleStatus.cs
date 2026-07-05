namespace Domain.Entities.AfterSales;

/// <summary>
/// 售后单业务状态，控制编辑、审核以及退货或退款处理阶段。
/// </summary>
public enum AfterSaleStatus
{
    /// <summary>
    /// 待提交：售后内容仍可编辑，尚未进入审核。
    /// </summary>
    Draft = 1,

    /// <summary>
    /// 待审核：售后申请已提交，等待审核结论。
    /// </summary>
    PendingAudit = 2,

    /// <summary>
    /// 待退货：审核已通过，等待取货或销售退货入库。
    /// </summary>
    ReturnPending = 3,

    /// <summary>
    /// 待退款：审核已通过，等待财务调整或退款完成。
    /// </summary>
    RefundPending = 4,

    /// <summary>
    /// 已完成：退货、退款或其他处理已经结束。
    /// </summary>
    Completed = 5
}
