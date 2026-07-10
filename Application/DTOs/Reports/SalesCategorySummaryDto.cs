namespace Application.DTOs.Reports;

/// <summary>
/// 商品分类维度销售汇总响应，展示已签收订单验收后的销售数量和金额。
/// </summary>
public class SalesCategorySummaryDto
{
    /// <summary>商品分类名称快照；历史明细未记录分类时为“未分类”。</summary>
    public string GoodsTypeName { get; set; } = string.Empty;

    /// <summary>客户验收后的销售基础数量。</summary>
    public decimal SaleBaseQuantity { get; set; }

    /// <summary>客户验收后的销售金额。</summary>
    public decimal SaleAmount { get; set; }

    /// <summary>参与汇总的销售订单数。</summary>
    public int OrderCount { get; set; }

    /// <summary>参与汇总的客户数。</summary>
    public int CustomerCount { get; set; }
}
