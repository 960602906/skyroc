using Domain.Entities.Purchases;

namespace Domain.Entities.Finance;

/// <summary>
/// 供应商结算单，记录向供应商付款流水、优惠金额和对供应商待结单据余额的核销结果。
/// </summary>
public class SupplierSettlement : BaseEntity
{
    /// <summary>
    /// 供应商结算单业务唯一编号。
    /// </summary>
    public string SettlementNo { get; set; } = string.Empty;

    /// <summary>
    /// 结算供应商主键；一张结算单只能核销同一供应商的单据。
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// 结算发生时的供应商名称快照。
    /// </summary>
    public string SupplierNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 供应商实际结款日期（UTC），用于财务筛选和对账。
    /// </summary>
    public DateTime SettlementDate { get; set; }

    /// <summary>
    /// 银行、现金或第三方支付的外部交易流水号；人工优惠可为空。
    /// </summary>
    public string? SerialNo { get; set; }

    /// <summary>
    /// 本结算单覆盖单据在结款前的待结金额合计，按系统业务币种计量。
    /// </summary>
    public decimal ShouldAmount { get; set; }

    /// <summary>
    /// 本结算单实际付款金额合计，按系统业务币种计量。
    /// </summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>
    /// 本结算单优惠减免金额合计，按系统业务币种计量。
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// 本结算单对供应商待结单据已结金额的增加值，等于付款金额与优惠金额合计。
    /// </summary>
    public decimal AppliedAmount { get; set; }

    /// <summary>
    /// 结算单创建后所选单据剩余待结金额合计，按系统业务币种计量。
    /// </summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>
    /// 供应商结算单状态，作废后不得再次用于核销待结单据。
    /// </summary>
    public SupplierSettlementStatus SettlementStatus { get; set; } = SupplierSettlementStatus.Pending;

    /// <summary>
    /// 结算单作废时间（UTC）；未作废时为空。
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
    /// 结款或作废备注，记录人工说明、付款渠道或异常处理原因。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 结算供应商档案。
    /// </summary>
    public virtual Supplier Supplier { get; set; } = null!;

    /// <summary>
    /// 结算单明细集合，每行对应一张被核销的供应商待结单据。
    /// </summary>
    public virtual ICollection<SupplierSettlementDetail> Details { get; set; } = new List<SupplierSettlementDetail>();
}
