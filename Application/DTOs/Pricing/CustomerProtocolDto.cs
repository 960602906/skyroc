namespace Application.DTOs.Pricing;

/// <summary>
///     客户协议价 DTO。
/// </summary>
public class CustomerProtocolDto : BaseDto
{
    /// <summary>
    ///     协议名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     协议编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     关联报价单 ID。
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    ///     生效开始时间。
    /// </summary>
    public DateTime EffectiveStart { get; set; }

    /// <summary>
    ///     生效结束时间。
    /// </summary>
    public DateTime? EffectiveEnd { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     报价单名称。
    /// </summary>
    public string? QuotationName { get; set; }

    /// <summary>
    ///     协议价商品明细。
    /// </summary>
    public List<CustomerProtocolGoodsDto>? Goods { get; set; }

    /// <summary>
    ///     绑定客户 ID。
    /// </summary>
    public List<Guid>? CustomerIds { get; set; }
}

