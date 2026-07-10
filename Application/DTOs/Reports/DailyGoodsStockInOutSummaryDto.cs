namespace Application.DTOs.Reports;

/// <summary>
/// 日商品库存出入库汇总响应，按自然日和商品汇总已审核库存流入流出。
/// </summary>
public class DailyGoodsStockInOutSummaryDto
{
    /// <summary>库存业务自然日；入库使用入库时间，出库使用出库时间。</summary>
    public DateOnly ReportDate { get; set; }

    /// <summary>商品主键。</summary>
    public Guid GoodsId { get; set; }

    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>计量单位名称快照；数量字段已按基础数量口径汇总。</summary>
    public string? BaseUnitName { get; set; }

    /// <summary>已审核入库基础数量合计。</summary>
    public decimal InBaseQuantity { get; set; }

    /// <summary>已审核入库金额合计，按系统业务币种计量。</summary>
    public decimal InAmount { get; set; }

    /// <summary>已审核出库基础数量合计。</summary>
    public decimal OutBaseQuantity { get; set; }

    /// <summary>已审核出库金额合计，按系统业务币种计量。</summary>
    public decimal OutAmount { get; set; }

    /// <summary>入库金额减出库金额后的净发生额，按系统业务币种计量。</summary>
    public decimal NetAmount { get; set; }
}
