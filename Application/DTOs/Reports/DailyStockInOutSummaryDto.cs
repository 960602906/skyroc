namespace Application.DTOs.Reports;

/// <summary>
/// 日库存出入库汇总响应，按自然日汇总已审核入库和出库单据的数量、金额和单据数。
/// </summary>
public class DailyStockInOutSummaryDto
{
    /// <summary>库存业务自然日；入库使用入库时间，出库使用出库时间。</summary>
    public DateOnly ReportDate { get; set; }

    /// <summary>已审核入库基础数量合计，按商品基础单位分别换算后汇总。</summary>
    public decimal InBaseQuantity { get; set; }

    /// <summary>已审核入库金额合计，按系统业务币种计量。</summary>
    public decimal InAmount { get; set; }

    /// <summary>已审核出库基础数量合计，按商品基础单位分别换算后汇总。</summary>
    public decimal OutBaseQuantity { get; set; }

    /// <summary>已审核出库金额合计，按系统业务币种计量。</summary>
    public decimal OutAmount { get; set; }

    /// <summary>入库金额减出库金额后的净发生额，按系统业务币种计量。</summary>
    public decimal NetAmount { get; set; }

    /// <summary>参与汇总的已审核入库单数。</summary>
    public int InOrderCount { get; set; }

    /// <summary>参与汇总的已审核出库单数。</summary>
    public int OutOrderCount { get; set; }
}
