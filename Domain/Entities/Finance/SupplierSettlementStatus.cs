namespace Domain.Entities.Finance;

/// <summary>
/// 供应商结算单状态，描述结算凭证对所选待结单据余额的处理结果。
/// </summary>
public enum SupplierSettlementStatus
{
    /// <summary>
    /// 作废，结算单已反向回滚对供应商待结单据的结款金额影响。
    /// </summary>
    Voided = -1,

    /// <summary>
    /// 待结款，结算单尚未形成有效结款金额；保留给后续分步付款流程。
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 部分结款，结算单已核销部分单据余额但仍有未结金额。
    /// </summary>
    PartiallySettled = 2,

    /// <summary>
    /// 已结款，结算单覆盖的单据余额已全部通过付款或优惠结清。
    /// </summary>
    Settled = 3
}
