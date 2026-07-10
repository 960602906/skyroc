namespace Domain.ReadModels.Reports;

/// <summary>
/// 按商品分类汇总的销售报表读模型。
/// </summary>
public sealed class SalesCategorySummaryReadModel
{
    /// <summary>商品分类名称快照；历史明细未记录分类时使用“未分类”。</summary>
    public string GoodsTypeName { get; init; } = string.Empty;

    /// <summary>客户验收后的销售基础数量。</summary>
    public decimal SaleBaseQuantity { get; init; }

    /// <summary>客户验收后的销售金额。</summary>
    public decimal SaleAmount { get; init; }

    /// <summary>参与汇总的销售订单数。</summary>
    public int OrderCount { get; init; }

    /// <summary>参与汇总的客户数。</summary>
    public int CustomerCount { get; init; }
}
