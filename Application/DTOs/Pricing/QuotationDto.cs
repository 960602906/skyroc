using Application.DTOs.Goods;
namespace Application.DTOs.Pricing;

/// <summary>
///     报价单 DTO。
/// </summary>
public class QuotationDto : BaseDto
{
    /// <summary>
    ///     报价单名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     报价单编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     报价单描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     生效开始时间。
    /// </summary>
    public DateTime? EffectiveStart { get; set; }

    /// <summary>
    ///     生效结束时间。
    /// </summary>
    public DateTime? EffectiveEnd { get; set; }

    /// <summary>
    ///     是否已审核。
    /// </summary>
    public bool IsAudited { get; set; }

    /// <summary>
    ///     报价商品明细。
    /// </summary>
    public List<QuotationGoodsDto>? Goods { get; set; }

    /// <summary>
    ///     绑定客户 ID。
    /// </summary>
    public List<Guid>? CustomerIds { get; set; }
}

