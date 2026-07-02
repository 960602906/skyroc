using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Pricing;

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

