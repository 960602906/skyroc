namespace Application.DTOs.Finance;

/// <summary>
/// 创建客户结款凭证请求，要求所有明细属于同一客户账单集合。
/// </summary>
public class CreateCustomerSettlementDto
{
    /// <summary>客户实际结款日期（UTC）；为空时使用服务端当前时间。</summary>
    public DateTime? SettlementDate { get; set; }

    /// <summary>外部交易流水号；存在实际收款时建议填写。</summary>
    public string? SerialNo { get; set; }

    /// <summary>结款备注，说明收款渠道、优惠原因或人工调整背景。</summary>
    public string? Remark { get; set; }

    /// <summary>待核销账单明细，必须至少包含一行且每行金额不能超过账单余额。</summary>
    public List<CreateCustomerSettlementDetailDto> Details { get; set; } = [];
}
