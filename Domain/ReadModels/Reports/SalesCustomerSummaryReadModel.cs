namespace Domain.ReadModels.Reports;

/// <summary>
/// 按客户汇总的销售报表读模型。
/// </summary>
public sealed class SalesCustomerSummaryReadModel
{
    /// <summary>客户主键。</summary>
    public Guid CustomerId { get; init; }

    /// <summary>客户名称快照。</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>客户编码快照。</summary>
    public string CustomerCode { get; init; } = string.Empty;

    /// <summary>客户验收后的销售基础数量。</summary>
    public decimal SaleBaseQuantity { get; init; }

    /// <summary>客户验收后的销售金额。</summary>
    public decimal SaleAmount { get; init; }

    /// <summary>参与汇总的销售订单数。</summary>
    public int OrderCount { get; init; }

    /// <summary>参与汇总的商品数。</summary>
    public int GoodsCount { get; init; }
}
