namespace Application.DTOs.Finance;

/// <summary>
/// 客户结款凭证明细响应，展示单张账单本次收款、优惠和余额变化。
/// </summary>
public class CustomerSettlementDetailDto : BaseDto
{
    /// <summary>所属客户结款凭证主键。</summary>
    public Guid CustomerSettlementId { get; set; }

    /// <summary>被核销的客户账单主键。</summary>
    public Guid CustomerBillId { get; set; }

    /// <summary>被核销账单编号快照。</summary>
    public string CustomerBillNo { get; set; } = string.Empty;

    /// <summary>账单来源销售订单主键。</summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>账单来源销售订单编号快照。</summary>
    public string SaleOrderNo { get; set; } = string.Empty;

    /// <summary>结款时账单应收金额快照。</summary>
    public decimal ReceivableAmount { get; set; }

    /// <summary>本次结款前账单已结金额快照。</summary>
    public decimal PreviousSettledAmount { get; set; }

    /// <summary>本明细实际收款金额。</summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>本明细优惠减免金额。</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>本明细对账单已结金额的增加值。</summary>
    public decimal AppliedAmount { get; set; }

    /// <summary>本次结款后账单已结金额快照。</summary>
    public decimal CurrentSettledAmount { get; set; }

    /// <summary>本次结款后账单剩余待结金额快照。</summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>单行结款备注。</summary>
    public string? Remark { get; set; }
}
