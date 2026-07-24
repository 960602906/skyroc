using Domain.Entities.Orders;
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

    public DateTime AuditTime { get; set; }

    public string? Remark { get; set; }
}
