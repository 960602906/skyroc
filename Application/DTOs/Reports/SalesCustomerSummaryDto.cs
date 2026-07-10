namespace Application.DTOs.Reports;

/// <summary>
/// 客户维度销售汇总响应，展示已签收订单验收后的销售数量和金额。
/// </summary>
public class SalesCustomerSummaryDto
{
    /// <summary>客户主键。</summary>
    public Guid CustomerId { get; set; }

    /// <summary>客户名称快照。</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>客户编码快照。</summary>
    public string CustomerCode { get; set; } = string.Empty;

    /// <summary>客户验收后的销售基础数量。</summary>
    public decimal SaleBaseQuantity { get; set; }

    /// <summary>客户验收后的销售金额。</summary>
    public decimal SaleAmount { get; set; }

    /// <summary>参与汇总的销售订单数。</summary>
    public int OrderCount { get; set; }

    /// <summary>参与汇总的商品数。</summary>
    public int GoodsCount { get; set; }
}
