namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单回单状态。
/// </summary>
public enum OrderReturnStatus
{
    /// <summary>
    /// 客户尚未回单。
    /// </summary>
    NotReturned = 0,
    /// <summary>
    /// 客户已经回单。
    /// </summary>
    Returned = 1
}
