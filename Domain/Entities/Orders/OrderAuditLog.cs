namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单审核轨迹，记录每次提交、通过、驳回和重提。
/// </summary>
public class OrderAuditLog : BaseEntity
{
    public Guid SaleOrderId { get; set; }

    public OrderAuditAction Action { get; set; }

    public SaleOrderStatus PreviousStatus { get; set; }

    public SaleOrderStatus CurrentStatus { get; set; }

    public Guid? AuditUserId { get; set; }

    public string AuditUserNameSnapshot { get; set; } = string.Empty;

    public DateTime AuditTime { get; set; }

    public string? Remark { get; set; }

    public virtual SaleOrder SaleOrder { get; set; } = null!;

    public virtual User? AuditUser { get; set; }
}
