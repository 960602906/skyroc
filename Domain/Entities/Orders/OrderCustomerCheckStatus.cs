namespace Domain.Entities.Orders;

/// <summary>
/// 订单商品客户验收状态。
/// </summary>
public enum OrderCustomerCheckStatus
{
    /// <summary>
    /// 等待客户验收。
    /// </summary>
    Pending = 0,
    /// <summary>
    /// 客户验收通过。
    /// </summary>
    Accepted = 1,
    /// <summary>
    /// 客户验收拒绝。
    /// </summary>
    Rejected = 2
}
