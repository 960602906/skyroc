using Application.DTOs.Goods;
using Application.Serialization;
using System.Text.Json.Serialization;

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
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? EffectiveStart { get; set; }

    /// <summary>
    ///     生效结束时间。
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
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

/// <summary>
///     创建报价单 DTO。
/// </summary>
public class CreateQuotationDto : CreateNamedCodeDto
{
    /// <summary>
    ///     报价单描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     生效开始时间。
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? EffectiveStart { get; set; }

    /// <summary>
    ///     生效结束时间。
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? EffectiveEnd { get; set; }

    /// <summary>
    ///     是否已审核。
    /// </summary>
    public bool IsAudited { get; set; }

    /// <summary>
    ///     绑定客户 ID。
    /// </summary>
    public List<Guid>? CustomerIds { get; set; }
}

/// <summary>
///     更新报价单 DTO。
/// </summary>
public class UpdateQuotationDto : CreateQuotationDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
///     报价商品 DTO。
/// </summary>
public class QuotationGoodsDto : BaseDto
{
    /// <summary>
    ///     报价单 ID。
    /// </summary>
    public Guid QuotationId { get; set; }

    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    ///     报价单位 ID。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    ///     销售单价。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    ///     最小起订数量。
    /// </summary>
    public decimal? MinOrderQuantity { get; set; }

    /// <summary>
    ///     是否在报价单内上架。
    /// </summary>
    public bool IsOnSale { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     商品名称。
    /// </summary>
    public string? GoodsName { get; set; }

    /// <summary>
    ///     商品编码。
    /// </summary>
    public string? GoodsCode { get; set; }

    /// <summary>
    ///     报价单位名称。
    /// </summary>
    public string? GoodsUnitName { get; set; }
}

/// <summary>
///     创建报价商品 DTO。
/// </summary>
public class CreateQuotationGoodsDto
{
    /// <summary>
    ///     报价单 ID。
    /// </summary>
    public Guid QuotationId { get; set; }

    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    ///     报价单位 ID。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    ///     销售单价。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    ///     最小起订数量。
    /// </summary>
    public decimal? MinOrderQuantity { get; set; }

    /// <summary>
    ///     是否在报价单内上架。
    /// </summary>
    public bool IsOnSale { get; set; } = true;

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
///     更新报价商品 DTO。
/// </summary>
public class UpdateQuotationGoodsDto : CreateQuotationGoodsDto, IHasId
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}
