namespace Application.DTOs.AfterSales;

/// <summary>
/// 取货任务司机分配请求，仅允许在任务开始前设置启用司机和计划上门时间。
/// </summary>
public class AssignPickupTaskDto
{
    /// <summary>执行取货的启用司机主键。</summary>
    public Guid DriverId { get; set; }

    /// <summary>计划上门取货时间（UTC）；未预约时可为空。</summary>
    public DateTime? PlannedPickupTime { get; set; }

    /// <summary>取货调度备注，最长 500 字符。</summary>
    public string? Remark { get; set; }
}
