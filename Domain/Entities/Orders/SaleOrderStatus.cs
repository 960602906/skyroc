namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单业务状态。
/// </summary>
public enum SaleOrderStatus
{
    PendingAudit = -1,
    SortingPending = 1,
    Sorting = 2,
    SortingCompleted = 3,
    Delivering = 4,
    Signed = 5,
    Rejected = 6
}
