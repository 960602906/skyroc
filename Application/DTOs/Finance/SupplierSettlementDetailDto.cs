using Domain.Entities.Finance;

namespace Application.DTOs.Finance;

/// <summary>
/// 供应商结算单明细响应，记录单张待结单据在本次付款和优惠后的余额变化。
/// </summary>
public class SupplierSettlementDetailDto : BaseDto
{
    /// <summary>被核销的供应商待结单据主键。</summary>
    public Guid SupplierBillId { get; set; }

    /// <summary>被核销待结单据编号快照。</summary>
    public string SupplierBillNo { get; set; } = string.Empty;

    /// <summary>被核销单据来源类型快照。</summary>
    public SupplierBillSourceType SourceType { get; set; }

    /// <summary>被核销单据来源出入库单编号快照。</summary>
    public string SourceDocumentNo { get; set; } = string.Empty;

    /// <summary>来源采购入库单主键；采购退货单据为空。</summary>
    public Guid? StockInOrderId { get; set; }

    /// <summary>来源采购退货出库单主键；采购入库单据为空。</summary>
    public Guid? StockOutOrderId { get; set; }

    /// <summary>结款时单据应付金额快照。</summary>
    public decimal PayableAmount { get; set; }

    /// <summary>本次结款前单据已结金额快照。</summary>
    public decimal PreviousSettledAmount { get; set; }

    /// <summary>本明细实际付款金额。</summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>本明细优惠减免金额。</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>本明细对单据已结金额的增加值。</summary>
    public decimal AppliedAmount { get; set; }

    /// <summary>本次结款后单据已结金额快照。</summary>
    public decimal CurrentSettledAmount { get; set; }

    /// <summary>本次结款后单据剩余待结绝对金额快照。</summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>单行结款备注。</summary>
    public string? Remark { get; set; }
}
