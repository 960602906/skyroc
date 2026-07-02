namespace Domain.Entities.Orders;

/// <summary>
/// 订单商品客户验收状态。
/// </summary>
public enum OrderCustomerCheckStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2
}
