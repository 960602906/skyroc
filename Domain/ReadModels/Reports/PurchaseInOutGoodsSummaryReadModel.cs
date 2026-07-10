namespace Domain.ReadModels.Reports;

/// <summary>
/// 采购出入库商品汇总读模型。
/// </summary>
public sealed class PurchaseInOutGoodsSummaryReadModel
{
    /// <summary>商品主键。</summary>
    public Guid GoodsId { get; init; }

    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; init; } = string.Empty;

    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; init; } = string.Empty;

    /// <summary>计量单位名称快照。</summary>
    public string? BaseUnitName { get; init; }

    /// <summary>采购入库基础数量合计。</summary>
    public decimal InBaseQuantity { get; init; }

    /// <summary>采购入库金额合计。</summary>
    public decimal InAmount { get; init; }

    /// <summary>采购退货出库基础数量合计。</summary>
    public decimal OutBaseQuantity { get; init; }

    /// <summary>采购退货出库金额合计。</summary>
    public decimal OutAmount { get; init; }
}
