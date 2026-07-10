namespace Domain.ReadModels.Reports;

/// <summary>首页客户销售排行只读投影。</summary>
public sealed class DashboardCustomerSalesRankReadModel
{
    /// <summary>客户主键。</summary>
    public Guid CustomerId { get; init; }

    /// <summary>客户名称快照。</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>客户验收销售金额，按系统业务币种计量。</summary>
    public decimal SaleAmount { get; init; }

    /// <summary>已签收销售订单数。</summary>
    public int OrderCount { get; init; }
}
