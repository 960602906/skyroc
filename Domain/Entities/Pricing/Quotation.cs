using Domain.Entities.Customers;

namespace Domain.Entities.Pricing;

/// <summary>
/// 报价单实体，定义面向客户下单的商品价格集合。
/// </summary>
public class Quotation : BaseEntity
{
    /// <summary>
    /// 报价单名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 报价单编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 报价单描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 生效开始时间。
    /// </summary>
    public DateTime? EffectiveStart { get; set; }

    /// <summary>
    /// 生效结束时间。
    /// </summary>
    public DateTime? EffectiveEnd { get; set; }

    /// <summary>
    /// 是否已审核。
    /// </summary>
    public bool IsAudited { get; set; }

    /// <summary>
    /// 报价商品明细集合。
    /// </summary>
    public virtual ICollection<QuotationGoods> Goods { get; set; } = new List<QuotationGoods>();

    /// <summary>
    /// 绑定该报价单的客户关系集合。
    /// </summary>
    public virtual ICollection<CustomerQuotation> CustomerQuotations { get; set; } = new List<CustomerQuotation>();

    /// <summary>
    /// 将该报价单作为默认报价的客户集合。
    /// </summary>
    public virtual ICollection<Customer> DefaultCustomers { get; set; } = new List<Customer>();

    /// <summary>
    /// 基于该报价单创建的客户协议价集合。
    /// </summary>
    public virtual ICollection<CustomerProtocol> CustomerProtocols { get; set; } = new List<CustomerProtocol>();
}
