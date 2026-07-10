namespace Domain.ReadModels.Reports;

/// <summary>
/// 日商品库存出入库汇总读模型。
/// </summary>
public sealed class DailyGoodsStockInOutSummaryReadModel
{
    /// <summary>库存业务日期，UTC 当日零点。</summary>
    public DateTime ReportDate { get; init; }

    /// <summary>商品主键。</summary>
    public Guid GoodsId { get; init; }

    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; init; } = string.Empty;

    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; init; } = string.Empty;

    /// <summary>计量单位名称快照。</summary>
    public string? BaseUnitName { get; init; }

    /// <summary>已审核入库基础数量合计。</summary>
    public decimal InBaseQuantity { get; init; }

    /// <summary>已审核入库金额合计。</summary>
    public decimal InAmount { get; init; }

    /// <summary>已审核出库基础数量合计。</summary>
    public decimal OutBaseQuantity { get; init; }

    /// <summary>已审核出库金额合计。</summary>
    public decimal OutAmount { get; init; }
}
