namespace Domain.Entities.AfterSales;

/// <summary>
/// 售后审核轨迹动作，用于还原提交、审核和反审核过程。
/// </summary>
public enum AfterSaleAuditAction
{
    /// <summary>
    /// 首次提交售后申请。
    /// </summary>
    Submit = 1,

    /// <summary>
    /// 审核通过售后申请。
    /// </summary>
    Approve = 2,

    /// <summary>
    /// 审核驳回售后申请。
    /// </summary>
    Reject = 3,

    /// <summary>
    /// 驳回后重新提交售后申请。
    /// </summary>
    Resubmit = 4,

    /// <summary>
    /// 撤销已通过的审核结论。
    /// </summary>
    Reverse = 5
}
