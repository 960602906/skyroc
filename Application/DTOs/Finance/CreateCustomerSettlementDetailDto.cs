namespace Application.DTOs.Finance;

/// <summary>
/// 创建客户结款凭证明细请求，指定单张账单的收款金额和优惠金额。
/// </summary>
public class CreateCustomerSettlementDetailDto
{
    /// <summary>待核销的客户账单主键。</summary>
    public Guid CustomerBillId { get; set; }

    /// <summary>本次实际收款金额，按系统业务币种计量。</summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>本次优惠减免金额，按系统业务币种计量。</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>单行备注，说明该账单的优惠或差异原因。</summary>
    public string? Remark { get; set; }
}
