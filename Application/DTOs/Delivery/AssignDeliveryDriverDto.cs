namespace Application.DTOs.Delivery;

/// <summary>
/// 批量分配配送司机请求，同一司机将覆盖目标任务原有司机和承运商快照。
/// </summary>
public class AssignDeliveryDriverDto
{
    /// <summary>
    /// 目标配送任务主键集合，空值、空主键和重复项不被接受。
    /// </summary>
    public List<Guid> TaskIds { get; set; } = [];

    /// <summary>
    /// 待分配的启用司机主键。
    /// </summary>
    public Guid DriverId { get; set; }
}
