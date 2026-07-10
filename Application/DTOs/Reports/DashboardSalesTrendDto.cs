namespace Application.DTOs.Reports;

/// <summary>
/// 首页销售趋势单日响应，按订单日期归属已签收订单的验收结果。
/// </summary>
public class DashboardSalesTrendDto
{
    /// <summary>统计自然日（UTC）。</summary>
    public DateOnly ReportDate { get; set; }

    /// <summary>当日已签收订单的客户验收销售金额，按系统业务币种计量。</summary>
    public decimal SaleAmount { get; set; }

    /// <summary>当日已签收销售订单数。</summary>
    public int OrderCount { get; set; }

    /// <summary>当日产生已签收订单的去重客户数。</summary>
    public int CustomerCount { get; set; }
}
