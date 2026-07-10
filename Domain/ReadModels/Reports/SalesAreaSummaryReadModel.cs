namespace Domain.ReadModels.Reports;

/// <summary>
/// 按订单配送地址快照汇总的销售区域报表读模型。
/// </summary>
public sealed class SalesAreaSummaryReadModel
{
    /// <summary>配送地址快照；当前模型未维护独立区域时作为区域口径。</summary>
    public string AreaName { get; init; } = string.Empty;

    /// <summary>客户验收后的销售基础数量。</summary>
    public decimal SaleBaseQuantity { get; init; }

    /// <summary>客户验收后的销售金额。</summary>
    public decimal SaleAmount { get; init; }

    /// <summary>参与汇总的销售订单数。</summary>
    public int OrderCount { get; init; }

    /// <summary>参与汇总的客户数。</summary>
    public int CustomerCount { get; init; }
}
