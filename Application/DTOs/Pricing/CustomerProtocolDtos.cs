using Application.Serialization;
using System.Text.Json.Serialization;

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
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime EffectiveStart { get; set; }

    /// <summary>
    ///     生效结束时间。
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
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

/// <summary>
///     创建客户协议价 DTO。
/// </summary>
public class CreateCustomerProtocolDto : CreateNamedCodeDto
{
    /// <summary>
    ///     关联报价单 ID。
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    ///     生效开始时间。
    /// </summary>
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime EffectiveStart { get; set; }

    /// <summary>
    ///     生效结束时间。
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? EffectiveEnd { get; set; }

    /// <summary>
    ///     绑定客户 ID。
    /// </summary>
    public List<Guid>? CustomerIds { get; set; }
}

/// <summary>
///     更新客户协议价 DTO。
/// </summary>
public class UpdateCustomerProtocolDto : CreateCustomerProtocolDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

/// <summary>
///     客户协议价商品 DTO。
/// </summary>
public class CustomerProtocolGoodsDto : BaseDto
{
    /// <summary>
    ///     客户协议价 ID。
    /// </summary>
    public Guid CustomerProtocolId { get; set; }

    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    ///     协议价单位 ID。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    ///     协议单价。
    /// </summary>
    public decimal ProtocolPrice { get; set; }

    /// <summary>
    ///     最小起订数量。
    /// </summary>
    public decimal? MinOrderQuantity { get; set; }

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
    ///     协议价单位名称。
    /// </summary>
    public string? GoodsUnitName { get; set; }
}

/// <summary>
///     创建客户协议价商品 DTO。
/// </summary>
public class CreateCustomerProtocolGoodsDto
{
    /// <summary>
    ///     客户协议价 ID。
    /// </summary>
    public Guid CustomerProtocolId { get; set; }

    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    ///     协议价单位 ID。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    ///     协议单价。
    /// </summary>
    public decimal ProtocolPrice { get; set; }

    /// <summary>
    ///     最小起订数量。
    /// </summary>
    public decimal? MinOrderQuantity { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
///     更新客户协议价商品 DTO。
/// </summary>
public class UpdateCustomerProtocolGoodsDto : CreateCustomerProtocolGoodsDto, IHasId
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}
