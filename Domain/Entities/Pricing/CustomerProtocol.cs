namespace Domain.Entities.Pricing;

/// <summary>
/// 客户协议价实体，定义指定客户群在周期内的特殊商品价格。
/// </summary>
public class CustomerProtocol : BaseEntity
{
    /// <summary>
    /// 协议名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 协议编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 关联报价单 ID。
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    /// 生效开始时间。
    /// </summary>
    public DateTime EffectiveStart { get; set; }

    /// <summary>
    /// 生效结束时间。
    /// </summary>
    public DateTime? EffectiveEnd { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 关联报价单。
    /// </summary>
    public virtual Quotation? Quotation { get; set; }

    /// <summary>
    /// 协议价商品明细集合。
    /// </summary>
    public virtual ICollection<CustomerProtocolGoods> Goods { get; set; } = new List<CustomerProtocolGoods>();

    /// <summary>
    /// 绑定客户集合。
    /// </summary>
    public virtual ICollection<CustomerProtocolCustomer> Customers { get; set; } = new List<CustomerProtocolCustomer>();
}
