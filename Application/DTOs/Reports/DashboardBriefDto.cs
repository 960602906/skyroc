namespace Application.DTOs.Reports;

/// <summary>
/// 首页经营概览响应，展示统计周期内已签收订单的验收销售结果。
/// </summary>
public class DashboardBriefDto
{
    /// <summary>已签收订单明细的客户验收销售金额，按系统业务币种计量。</summary>
    public decimal SaleAmount { get; set; }

    /// <summary>统计周期内的已签收销售订单数。</summary>
    public int OrderCount { get; set; }

    /// <summary>统计周期内产生已签收订单的去重客户数。</summary>
    public int CustomerCount { get; set; }
}
