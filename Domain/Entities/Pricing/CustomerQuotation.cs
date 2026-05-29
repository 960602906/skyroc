using Domain.Entities.Customers;

namespace Domain.Entities.Pricing;

/// <summary>
/// 客户报价关系实体，表示客户可使用的报价单。
/// </summary>
public class CustomerQuotation
{
    /// <summary>
    /// 客户 ID。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 报价单 ID。
    /// </summary>
    public Guid QuotationId { get; set; }

    /// <summary>
    /// 是否客户默认报价单。
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 生效开始时间。
    /// </summary>
    public DateTime? EffectiveStart { get; set; }

    /// <summary>
    /// 生效结束时间。
    /// </summary>
    public DateTime? EffectiveEnd { get; set; }

    /// <summary>
    /// 客户。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// 报价单。
    /// </summary>
    public virtual Quotation Quotation { get; set; } = null!;
}
