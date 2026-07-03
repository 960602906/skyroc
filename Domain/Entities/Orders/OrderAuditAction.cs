namespace Domain.Entities.Orders;

/// <summary>
/// 订单审核轨迹动作。
/// </summary>
public enum OrderAuditAction
{
    /// <summary>
    /// 首次提交订单审核。
    /// </summary>
    Submit = 0,
    /// <summary>
    /// 审核通过订单。
    /// </summary>
    Approve = 1,
    /// <summary>
    /// 审核驳回订单。
    /// </summary>
    Reject = 2,
    /// <summary>
    /// 驳回后重新提交审核。
    /// </summary>
    Resubmit = 3
}
