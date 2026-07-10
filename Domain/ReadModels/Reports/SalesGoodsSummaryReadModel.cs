namespace Domain.ReadModels.Reports;

/// <summary>
/// 按商品汇总的销售报表读模型。
/// </summary>
public sealed class SalesGoodsSummaryReadModel
{
    /// <summary>商品主键。</summary>
    public Guid GoodsId { get; init; }

    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; init; } = string.Empty;

    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; init; } = string.Empty;

    /// <summary>商品分类名称快照。</summary>
    public string? GoodsTypeName { get; init; }

    /// <summary>基础单位名称快照。</summary>
    public string? BaseUnitName { get; init; }

    /// <summary>客户验收后的销售基础数量。</summary>
    public decimal SaleBaseQuantity { get; init; }

    /// <summary>客户验收后的销售金额。</summary>
    public decimal SaleAmount { get; init; }

    /// <summary>参与汇总的销售订单数。</summary>
    public int OrderCount { get; init; }

    /// <summary>参与汇总的客户数。</summary>
    public int CustomerCount { get; init; }
}
