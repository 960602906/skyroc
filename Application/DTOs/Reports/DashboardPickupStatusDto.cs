using Domain.Entities.AfterSales;

namespace Application.DTOs.Reports;

/// <summary>
/// 首页取货状态统计响应，按取货任务创建时间和当前履约状态计数。
/// </summary>
public class DashboardPickupStatusDto
{
    /// <summary>取货任务当前履约状态。</summary>
    public PickupTaskStatus PickupStatus { get; set; }

    /// <summary>该状态下的取货任务数。</summary>
    public int TaskCount { get; set; }
}
