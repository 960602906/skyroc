namespace Domain.ReadModels.Reports;

/// <summary>首页经营概览只读投影。</summary>
public sealed class DashboardBriefReadModel
{
    /// <summary>已签收订单明细的客户验收销售金额，按系统业务币种计量。</summary>
    public decimal SaleAmount { get; init; }

    /// <summary>已签收销售订单数。</summary>
    public int OrderCount { get; init; }

    /// <summary>产生已签收订单的去重客户数。</summary>
    public int CustomerCount { get; init; }
}
