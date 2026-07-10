using Domain.Entities.AfterSales;

namespace Domain.ReadModels.Reports;

/// <summary>首页取货状态只读投影。</summary>
public sealed class DashboardPickupStatusReadModel
{
    /// <summary>取货任务当前履约状态。</summary>
    public PickupTaskStatus PickupStatus { get; init; }

    /// <summary>该状态下的取货任务数。</summary>
    public int TaskCount { get; init; }
}
