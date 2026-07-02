namespace Domain.Entities.Orders;

/// <summary>
/// 订单审核轨迹动作。
/// </summary>
public enum OrderAuditAction
{
    Submit = 0,
    Approve = 1,
    Reject = 2,
    Resubmit = 3
}
