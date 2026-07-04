namespace Domain.Entities.Delivery;

/// <summary>
/// 司机实体，维护执行配送任务的司机基础资料，可绑定所属承运商。
/// </summary>
public class Driver : BaseEntity
{
    /// <summary>
    /// 司机姓名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 司机编码，全局唯一。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 联系电话。
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 所属承运商主键；自营司机可为空。
    /// </summary>
    public Guid? CarrierId { get; set; }

    /// <summary>
    /// 司机车牌号。
    /// </summary>
    public string? PlateNumber { get; set; }

    /// <summary>
    /// 司机驾驶证号。
    /// </summary>
    public string? LicenseNo { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属承运商。
    /// </summary>
    public virtual Carrier? Carrier { get; set; }
}
