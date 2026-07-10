namespace Application.DTOs.Reports;

/// <summary>
/// 商品维度销售汇总响应，展示已签收订单验收后的销售数量和金额。
/// </summary>
public class SalesGoodsSummaryDto
{
    /// <summary>商品主键。</summary>
    public Guid GoodsId { get; set; }

    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>商品分类名称快照。</summary>
    public string? GoodsTypeName { get; set; }

    /// <summary>基础单位名称快照。</summary>
    public string? BaseUnitName { get; set; }

    /// <summary>客户验收后的销售基础数量。</summary>
    public decimal SaleBaseQuantity { get; set; }

    /// <summary>客户验收后的销售金额。</summary>
    public decimal SaleAmount { get; set; }

    /// <summary>参与汇总的销售订单数。</summary>
    public int OrderCount { get; set; }

    /// <summary>参与汇总的客户数。</summary>
    public int CustomerCount { get; set; }
}
