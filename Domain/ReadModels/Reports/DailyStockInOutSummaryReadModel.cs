namespace Domain.ReadModels.Reports;

/// <summary>
/// 日库存出入库汇总读模型。
/// </summary>
public sealed class DailyStockInOutSummaryReadModel
{
    /// <summary>库存业务日期，UTC 当日零点。</summary>
    public DateTime ReportDate { get; init; }

    /// <summary>已审核入库基础数量合计。</summary>
    public decimal InBaseQuantity { get; init; }

    /// <summary>已审核入库金额合计。</summary>
    public decimal InAmount { get; init; }

    /// <summary>已审核出库基础数量合计。</summary>
    public decimal OutBaseQuantity { get; init; }

    /// <summary>已审核出库金额合计。</summary>
    public decimal OutAmount { get; init; }

    /// <summary>参与汇总的已审核入库单数。</summary>
    public int InOrderCount { get; init; }

    /// <summary>参与汇总的已审核出库单数。</summary>
    public int OutOrderCount { get; init; }
}
