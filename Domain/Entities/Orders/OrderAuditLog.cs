namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单审核轨迹，记录每次提交、通过、驳回和重提。
/// </summary>
public class OrderAuditLog : BaseEntity
{
    /// <summary>
    /// 来源销售订单主键。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 审核动作类型。
    /// </summary>
    public OrderAuditAction Action { get; set; }

    /// <summary>
    /// 审核动作发生前的订单状态。
    /// </summary>
    public SaleOrderStatus PreviousStatus { get; set; }

    /// <summary>
    /// 审核动作完成后的订单状态。
    /// </summary>
    public SaleOrderStatus CurrentStatus { get; set; }

    /// <summary>
    /// 执行审核的系统用户主键。
    /// </summary>
    public Guid? AuditUserId { get; set; }

    /// <summary>
    /// 审核时的用户名称快照。
    /// </summary>
    public string AuditUserNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 审核动作发生时间（UTC）。
    /// </summary>
    public DateTime AuditTime { get; set; }

    /// <summary>
    /// 业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 关联的销售订单。
    /// </summary>
    public virtual SaleOrder SaleOrder { get; set; } = null!;

    /// <summary>
    /// 执行审核的系统用户。
    /// </summary>
    public virtual User? AuditUser { get; set; }
}
