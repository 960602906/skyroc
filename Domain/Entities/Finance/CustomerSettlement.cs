using Domain.Entities.Customers;

namespace Domain.Entities.Finance;

/// <summary>
/// 客户结款凭证，记录客户付款流水、优惠金额和对客户账单余额的核销结果。
/// </summary>
public class CustomerSettlement : BaseEntity
{
    /// <summary>
    /// 客户结款凭证业务唯一编号。
    /// </summary>
    public string SettlementNo { get; set; } = string.Empty;

    /// <summary>
    /// 结款客户主键；一张凭证只能核销同一客户的账单。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 结款发生时的客户名称快照。
    /// </summary>
    public string CustomerNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 客户实际结款日期（UTC），用于财务筛选和对账。
    /// </summary>
    public DateTime SettlementDate { get; set; }

    /// <summary>
    /// 银行、现金或第三方支付的外部交易流水号；人工优惠可为空。
    /// </summary>
    public string? SerialNo { get; set; }

    /// <summary>
    /// 本凭证覆盖账单在结款前的待结金额合计，按系统业务币种计量。
    /// </summary>
    public decimal ShouldAmount { get; set; }

    /// <summary>
    /// 本凭证实际收款金额合计，按系统业务币种计量。
    /// </summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>
    /// 本凭证优惠减免金额合计，按系统业务币种计量。
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// 本凭证对客户账单已结金额的增加值，等于收款金额与优惠金额合计。
    /// </summary>
    public decimal AppliedAmount { get; set; }

    /// <summary>
    /// 凭证创建后所选账单剩余待结金额合计，按系统业务币种计量。
    /// </summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>
    /// 客户结款凭证状态，作废后不得再次用于核销账单。
    /// </summary>
    public CustomerSettlementStatus SettlementStatus { get; set; } = CustomerSettlementStatus.Pending;

    /// <summary>
    /// 凭证作废时间（UTC）；未作废时为空。
    /// </summary>
    public DateTime? VoidedTime { get; set; }

    /// <summary>
    /// 作废操作人主键；未作废时为空。
    /// </summary>
    public Guid? VoidedBy { get; set; }

    /// <summary>
    /// 作废操作人名称快照；未作废时为空。
    /// </summary>
    public string? VoidedByNameSnapshot { get; set; }

    /// <summary>
    /// 结款或作废备注，记录人工说明、收款渠道或异常处理原因。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 结款客户档案。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// 结款凭证明细集合，每行对应一张被核销的客户账单。
    /// </summary>
    public virtual ICollection<CustomerSettlementDetail> Details { get; set; } = new List<CustomerSettlementDetail>();
}
