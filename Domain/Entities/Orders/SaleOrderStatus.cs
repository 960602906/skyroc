namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单业务状态。
/// </summary>
public enum SaleOrderStatus
{
    /// <summary>
    /// 待审核。
    /// </summary>
    PendingAudit = -1,
    /// <summary>
    /// 待分拣。
    /// </summary>
    SortingPending = 1,
    /// <summary>
    /// 分拣中。
    /// </summary>
    Sorting = 2,
    /// <summary>
    /// 分拣完成。
    /// </summary>
    SortingCompleted = 3,
    /// <summary>
    /// 配送中。
    /// </summary>
    Delivering = 4,
    /// <summary>
    /// 已签收。
    /// </summary>
    Signed = 5,
    /// <summary>
    /// 审核已驳回。
    /// </summary>
    Rejected = 6
}
