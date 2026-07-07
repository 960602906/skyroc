using Domain.Entities.Storage;

namespace Domain.Entities.Finance;

/// <summary>
/// 供应商结算单明细，记录单张供应商待结单据在本次付款和优惠后的余额变化。
/// </summary>
public class SupplierSettlementDetail : BaseEntity
{
    /// <summary>
    /// 所属供应商结算单主键。
    /// </summary>
    public Guid SupplierSettlementId { get; set; }

    /// <summary>
    /// 被核销的供应商待结单据主键。
    /// </summary>
    public Guid SupplierBillId { get; set; }

    /// <summary>
    /// 被核销待结单据编号快照。
    /// </summary>
    public string SupplierBillNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 被核销单据来源类型快照。
    /// </summary>
    public SupplierBillSourceType SourceType { get; set; }

    /// <summary>
    /// 被核销单据来源出入库单编号快照。
    /// </summary>
    public string SourceDocumentNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 来源采购入库单主键；采购退货单据为空。
    /// </summary>
    public Guid? StockInOrderId { get; set; }

    /// <summary>
    /// 来源采购退货出库单主键；采购入库单据为空。
    /// </summary>
    public Guid? StockOutOrderId { get; set; }

    /// <summary>
    /// 结款时单据应付金额快照，按系统业务币种计量。
    /// </summary>
    public decimal PayableAmountSnapshot { get; set; }

    /// <summary>
    /// 本次结款前单据已结金额快照，按系统业务币种计量。
    /// </summary>
    public decimal PreviousSettledAmount { get; set; }

    /// <summary>
    /// 本明细实际付款金额，按系统业务币种计量。
    /// </summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>
    /// 本明细优惠减免金额，按系统业务币种计量。
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// 本明细对单据已结金额的增加值，等于付款金额与优惠金额合计。
    /// </summary>
    public decimal AppliedAmount { get; set; }

    /// <summary>
    /// 本次结款后单据已结金额快照，按系统业务币种计量。
    /// </summary>
    public decimal CurrentSettledAmount { get; set; }

    /// <summary>
    /// 本次结款后单据剩余待结绝对金额快照，按系统业务币种计量。
    /// </summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>
    /// 单行结款备注，记录该单据的差异说明或优惠原因。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属供应商结算单。
    /// </summary>
    public virtual SupplierSettlement SupplierSettlement { get; set; } = null!;

    /// <summary>
    /// 被核销的供应商待结单据。
    /// </summary>
    public virtual SupplierBill SupplierBill { get; set; } = null!;

    /// <summary>
    /// 来源采购入库单。
    /// </summary>
    public virtual StockInOrder? StockInOrder { get; set; }

    /// <summary>
    /// 来源采购退货出库单。
    /// </summary>
    public virtual StockOutOrder? StockOutOrder { get; set; }
}
