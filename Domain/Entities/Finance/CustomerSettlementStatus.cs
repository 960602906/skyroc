namespace Domain.Entities.Finance;

/// <summary>
/// 客户结款凭证状态，描述凭证对所选账单余额的处理结果。
/// </summary>
public enum CustomerSettlementStatus
{
    /// <summary>
    /// 作废，凭证已反向回滚对客户账单的结款金额影响。
    /// </summary>
    Voided = -1,

    /// <summary>
    /// 待结款，凭证尚未形成有效结款金额；保留给后续分步收款流程。
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 部分结款，凭证已核销部分账单余额但仍有未结金额。
    /// </summary>
    PartiallySettled = 2,

    /// <summary>
    /// 已结款，凭证覆盖的账单余额已全部通过收款或优惠结清。
    /// </summary>
    Settled = 3
}
