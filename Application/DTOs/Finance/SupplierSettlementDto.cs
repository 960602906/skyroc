using Domain.Entities.Finance;

namespace Application.DTOs.Finance;

/// <summary>
/// 供应商结算单响应，包含向供应商付款流水、优惠金额、结算状态和单据核销明细。
/// </summary>
public class SupplierSettlementDto : BaseDto
{
    /// <summary>供应商结算单业务唯一编号。</summary>
    public string SettlementNo { get; set; } = string.Empty;

    /// <summary>结算供应商主键。</summary>
    public Guid SupplierId { get; set; }

    /// <summary>结算发生时的供应商名称快照。</summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>供应商实际结款日期（UTC）。</summary>
    public DateTime SettlementDate { get; set; }

    /// <summary>外部交易流水号；纯优惠结算可为空。</summary>
    public string? SerialNo { get; set; }

    /// <summary>本结算单覆盖单据在结款前的待结金额合计。</summary>
    public decimal ShouldAmount { get; set; }

    /// <summary>本结算单实际付款金额合计。</summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>本结算单优惠减免金额合计。</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>本结算单对供应商待结单据已结金额的增加值。</summary>
    public decimal AppliedAmount { get; set; }

    /// <summary>结算单创建后所选单据剩余待结金额合计。</summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>供应商结算单状态。</summary>
    public SupplierSettlementStatus SettlementStatus { get; set; }

    /// <summary>结算单作废时间（UTC）。</summary>
    public DateTime? VoidedTime { get; set; }

    /// <summary>作废操作人名称快照。</summary>
    public string? VoidedByName { get; set; }

    /// <summary>结款或作废备注。</summary>
    public string? Remark { get; set; }

    /// <summary>结算明细，按待结单据编号稳定排序。</summary>
    public List<SupplierSettlementDetailDto> Details { get; set; } = [];
}
