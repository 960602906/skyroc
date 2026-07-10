namespace Application.QueryParameters.Reports;

/// <summary>
/// 首页驾驶舱查询条件，统一限定销售、对账和取货统计的 UTC 业务周期与排行条数。
/// </summary>
public class DashboardQueryParameters
{
    /// <summary>统计周期起点（UTC，包含）；为空时不限制起点。</summary>
    public DateTime? DateStart { get; set; }

    /// <summary>统计周期终点（UTC，包含）；为空时不限制终点。</summary>
    public DateTime? DateEnd { get; set; }

    /// <summary>客户和商品分类排行返回条数，默认 10，最大 100。</summary>
    public int RankSize { get; set; } = 10;
}
