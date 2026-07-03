using Application.Serialization;
using Domain.Entities.Orders;
using System.Text.Json.Serialization;

namespace Application.DTOs.Orders;

/// <summary>
/// 订单审核记录 DTO。
/// </summary>
public class OrderAuditLogDto : BaseDto
{
    public Guid SaleOrderId { get; set; }

    public OrderAuditAction Action { get; set; }

    public SaleOrderStatus PreviousStatus { get; set; }

    public SaleOrderStatus CurrentStatus { get; set; }

    public Guid? AuditUserId { get; set; }

    public string AuditUserName { get; set; } = string.Empty;

    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime AuditTime { get; set; }

    public string? Remark { get; set; }
}
