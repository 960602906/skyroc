namespace Domain.Entities.Finance;

/// <summary>
/// 客户账单结款状态，描述应收账单在客户结款流程中的余额状态。
/// </summary>
public enum CustomerBillStatus
{
    /// <summary>
    /// 待结款，账单尚未发生有效客户结款。
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 部分结款，账单已有结款但仍存在未结余额。
    /// </summary>
    PartiallySettled = 2,

    /// <summary>
    /// 已结款，账单应收金额已全部结清。
    /// </summary>
    Settled = 3
}
