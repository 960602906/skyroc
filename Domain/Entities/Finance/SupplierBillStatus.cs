namespace Domain.Entities.Finance;

/// <summary>
/// 供应商待结单据状态，描述应付单据在供应商结算流程中的余额状态。
/// </summary>
public enum SupplierBillStatus
{
    /// <summary>
    /// 待结款，单据尚未发生有效供应商结算。
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 部分结款，单据已有结算但仍存在未结余额。
    /// </summary>
    PartiallySettled = 2,

    /// <summary>
    /// 已结款，单据应付金额已全部结清。
    /// </summary>
    Settled = 3
}
