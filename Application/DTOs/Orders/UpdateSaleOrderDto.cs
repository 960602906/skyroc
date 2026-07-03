using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Orders;

/// <summary>
/// 更新销售订单 DTO。
/// </summary>
public class UpdateSaleOrderDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? QuotationId { get; set; }

    public Guid? WareId { get; set; }

    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime OrderDate { get; set; }

    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? ReceiveDate { get; set; }

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? Remark { get; set; }

    public string? InnerRemark { get; set; }

    public List<UpdateSaleOrderDetailDto> Details { get; set; } = [];
}
