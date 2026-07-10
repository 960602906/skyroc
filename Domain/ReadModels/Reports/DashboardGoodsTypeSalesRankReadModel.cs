namespace Domain.ReadModels.Reports;

/// <summary>首页商品分类销售排行只读投影。</summary>
public sealed class DashboardGoodsTypeSalesRankReadModel
{
    /// <summary>商品分类名称快照。</summary>
    public string GoodsTypeName { get; init; } = string.Empty;

    /// <summary>客户验收销售金额，按系统业务币种计量。</summary>
    public decimal SaleAmount { get; init; }

    /// <summary>已签收销售订单数。</summary>
    public int OrderCount { get; init; }
}
