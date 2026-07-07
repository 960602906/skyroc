using Domain.Entities.Finance;

namespace Application.DTOs.Finance;

/// <summary>
/// 客户结款凭证响应，包含客户付款流水、优惠金额、凭证状态和账单核销明细。
/// </summary>
public class CustomerSettlementDto : BaseDto
{
    /// <summary>客户结款凭证业务唯一编号。</summary>
    public string SettlementNo { get; set; } = string.Empty;

    /// <summary>结款客户主键。</summary>
    public Guid CustomerId { get; set; }

    /// <summary>结款发生时的客户名称快照。</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>客户实际结款日期（UTC）。</summary>
    public DateTime SettlementDate { get; set; }

    /// <summary>外部交易流水号；纯优惠凭证可为空。</summary>
    public string? SerialNo { get; set; }

    /// <summary>本凭证覆盖账单在结款前的待结金额合计。</summary>
    public decimal ShouldAmount { get; set; }

    /// <summary>本凭证实际收款金额合计。</summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>本凭证优惠减免金额合计。</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>本凭证对客户账单已结金额的增加值。</summary>
    public decimal AppliedAmount { get; set; }

    /// <summary>凭证创建后所选账单剩余待结金额合计。</summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>客户结款凭证状态。</summary>
    public CustomerSettlementStatus SettlementStatus { get; set; }

    /// <summary>凭证作废时间（UTC）。</summary>
    public DateTime? VoidedTime { get; set; }

    /// <summary>作废操作人名称快照。</summary>
    public string? VoidedByName { get; set; }

    /// <summary>结款或作废备注。</summary>
    public string? Remark { get; set; }

    /// <summary>凭证明细，按账单编号稳定排序。</summary>
    public List<CustomerSettlementDetailDto> Details { get; set; } = [];
}
