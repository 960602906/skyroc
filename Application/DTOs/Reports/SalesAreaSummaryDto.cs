namespace Application.DTOs.Reports;

/// <summary>
/// 区域维度销售汇总响应，当前按订单配送地址快照聚合。
/// </summary>
public class SalesAreaSummaryDto
{
    /// <summary>配送地址快照；当前模型未维护独立区域时作为区域口径。</summary>
    public string AreaName { get; set; } = string.Empty;

    /// <summary>客户验收后的销售基础数量。</summary>
    public decimal SaleBaseQuantity { get; set; }

    /// <summary>客户验收后的销售金额。</summary>
    public decimal SaleAmount { get; set; }

    /// <summary>参与汇总的销售订单数。</summary>
    public int OrderCount { get; set; }

    /// <summary>参与汇总的客户数。</summary>
    public int CustomerCount { get; set; }
}
