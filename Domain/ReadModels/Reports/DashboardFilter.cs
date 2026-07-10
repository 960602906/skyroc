namespace Domain.ReadModels.Reports;

/// <summary>
/// 首页驾驶舱筛选条件，统一约束销售订单、客户账单和取货任务的 UTC 统计周期与排行条数。
/// </summary>
public sealed class DashboardFilter
{
    /// <summary>统计周期起点（UTC，包含）。</summary>
    public DateTime? DateStart { get; init; }

    /// <summary>统计周期终点（UTC，包含）。</summary>
    public DateTime? DateEnd { get; init; }

    /// <summary>客户和商品分类排行返回条数，范围为 1 到 100。</summary>
    public int RankSize { get; init; }
}
