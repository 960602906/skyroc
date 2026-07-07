namespace Application.DTOs.Finance;

/// <summary>
/// 创建供应商结算单明细请求，指定待结单据及本次付款与优惠金额。
/// </summary>
public class CreateSupplierSettlementDetailDto
{
    /// <summary>被核销的供应商待结单据主键。</summary>
    public Guid SupplierBillId { get; set; }

    /// <summary>本明细实际付款金额，按系统业务币种计量。</summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>本明细优惠减免金额，按系统业务币种计量。</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>单行结款备注。</summary>
    public string? Remark { get; set; }
}
