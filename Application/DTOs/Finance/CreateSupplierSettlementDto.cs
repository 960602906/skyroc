namespace Application.DTOs.Finance;

/// <summary>
/// 创建供应商结算单请求，要求所有明细属于同一供应商的待结单据集合。
/// </summary>
public class CreateSupplierSettlementDto
{
    /// <summary>供应商实际结款日期（UTC）；为空时使用服务端当前时间。</summary>
    public DateTime? SettlementDate { get; set; }

    /// <summary>外部交易流水号；存在实际付款时建议填写。</summary>
    public string? SerialNo { get; set; }

    /// <summary>结款备注，说明付款渠道、优惠原因或人工调整背景。</summary>
    public string? Remark { get; set; }

    /// <summary>待核销待结单据明细，必须至少包含一行且每行金额不能超过单据余额。</summary>
    public List<CreateSupplierSettlementDetailDto> Details { get; set; } = [];
}
