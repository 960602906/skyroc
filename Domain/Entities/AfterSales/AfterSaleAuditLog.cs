namespace Domain.Entities.AfterSales;

/// <summary>
/// 售后审核记录，保存每次提交、通过、驳回、重提和反审核轨迹。
/// </summary>
public class AfterSaleAuditLog : BaseEntity
{
    /// <summary>
    /// 所属售后单主键。
    /// </summary>
    public Guid AfterSaleId { get; set; }

    /// <summary>
    /// 本次审核轨迹的动作类型。
    /// </summary>
    public AfterSaleAuditAction Action { get; set; }

    /// <summary>
    /// 审核动作发生前的售后单状态。
    /// </summary>
    public AfterSaleStatus PreviousStatus { get; set; }

    /// <summary>
    /// 审核动作完成后的售后单状态。
    /// </summary>
    public AfterSaleStatus CurrentStatus { get; set; }

    /// <summary>
    /// 执行审核动作的系统用户主键；用户删除后保留名称快照。
    /// </summary>
    public Guid? AuditUserId { get; set; }

    /// <summary>
    /// 执行审核动作时的用户名称快照。
    /// </summary>
    public string AuditUserNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 审核动作发生时间（UTC）。
    /// </summary>
    public DateTime AuditTime { get; set; }

    /// <summary>
    /// 审核意见、驳回原因或反审核说明。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属售后单。
    /// </summary>
    public virtual AfterSale AfterSale { get; set; } = null!;

    /// <summary>
    /// 执行审核动作的系统用户。
    /// </summary>
    public virtual User? AuditUser { get; set; }
}
