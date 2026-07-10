namespace Application.DTOs.Reports;

/// <summary>
/// 首页商品分类销售排行响应，按已签收订单的客户验收金额降序排列。
/// </summary>
public class DashboardGoodsTypeSalesRankDto
{
    /// <summary>订单明细保存的商品分类名称快照；未分类商品返回“未分类”。</summary>
    public string GoodsTypeName { get; set; } = string.Empty;

    /// <summary>客户验收后的销售金额，按系统业务币种计量。</summary>
    public decimal SaleAmount { get; set; }

    /// <summary>参与排行的已签收订单数。</summary>
    public int OrderCount { get; set; }
}
