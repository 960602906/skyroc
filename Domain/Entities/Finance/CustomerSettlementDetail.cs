using Domain.Entities.Orders;

namespace Domain.Entities.Finance;

/// <summary>
/// 客户结款凭证明细，记录单张客户账单在本次收款和优惠后的余额变化。
/// </summary>
public class CustomerSettlementDetail : BaseEntity
{
    /// <summary>
    /// 所属客户结款凭证主键。
    /// </summary>
    public Guid CustomerSettlementId { get; set; }

    /// <summary>
    /// 被核销的客户账单主键。
    /// </summary>
    public Guid CustomerBillId { get; set; }

    /// <summary>
    /// 被核销账单编号快照。
    /// </summary>
    public string CustomerBillNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 账单来源销售订单主键，用于凭证追溯。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 账单来源销售订单编号快照。
    /// </summary>
    public string SaleOrderNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 结款时账单应收金额快照，按系统业务币种计量。
    /// </summary>
    public decimal ReceivableAmountSnapshot { get; set; }

    /// <summary>
    /// 本次结款前账单已结金额快照，按系统业务币种计量。
    /// </summary>
    public decimal PreviousSettledAmount { get; set; }

    /// <summary>
    /// 本明细实际收款金额，按系统业务币种计量。
    /// </summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>
    /// 本明细优惠减免金额，按系统业务币种计量。
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// 本明细对账单已结金额的增加值，等于收款金额与优惠金额合计。
    /// </summary>
    public decimal AppliedAmount { get; set; }

    /// <summary>
    /// 本次结款后账单已结金额快照，按系统业务币种计量。
    /// </summary>
    public decimal CurrentSettledAmount { get; set; }

    /// <summary>
    /// 本次结款后账单剩余待结金额快照，按系统业务币种计量。
    /// </summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>
    /// 单行结款备注，记录该账单的差异说明或优惠原因。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属客户结款凭证。
    /// </summary>
    public virtual CustomerSettlement CustomerSettlement { get; set; } = null!;

    /// <summary>
    /// 被核销的客户账单。
    /// </summary>
    public virtual CustomerBill CustomerBill { get; set; } = null!;

    /// <summary>
    /// 账单来源销售订单。
    /// </summary>
    public virtual SaleOrder SaleOrder { get; set; } = null!;
}
