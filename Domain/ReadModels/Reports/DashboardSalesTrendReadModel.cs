namespace Domain.ReadModels.Reports;

/// <summary>首页单日销售趋势只读投影。</summary>
public sealed class DashboardSalesTrendReadModel
{
    /// <summary>统计自然日（UTC）。</summary>
    public DateTime ReportDate { get; init; }

    /// <summary>客户验收销售金额，按系统业务币种计量。</summary>
    public decimal SaleAmount { get; init; }

    /// <summary>已签收销售订单数。</summary>
    public int OrderCount { get; init; }

    /// <summary>产生已签收订单的去重客户数。</summary>
    public int CustomerCount { get; init; }
}
