namespace Application.DTOs.Storage;

/// <summary>
/// 库存盘点批次明细 DTO，返回盘点时点的账面、实盘、差异数量及成本快照。
/// </summary>
public class StocktakingDetailDto : BaseDto
{
    /// <summary>
    /// 所属盘点主单主键。
    /// </summary>
    public Guid StocktakingOrderId { get; set; }

    /// <summary>
    /// 被盘点库存批次主键。
    /// </summary>
    public Guid StockBatchId { get; set; }

    /// <summary>
    /// 被盘点商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 盘点创建时的商品名称快照。
    /// </summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 盘点创建时的商品编码快照。
    /// </summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>
    /// 盘点创建时的批次号快照。
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 商品基础计量单位主键，明细数量均按该单位计量。
    /// </summary>
    public Guid BaseUnitId { get; set; }

    /// <summary>
    /// 盘点创建时的基础单位名称快照。
    /// </summary>
    public string BaseUnitName { get; set; } = string.Empty;

    /// <summary>
    /// 盘点快照时的批次账面数量，按商品基础单位计量。
    /// </summary>
    public decimal BookQuantity { get; set; }

    /// <summary>
    /// 盘点人员录入的实际数量，按商品基础单位计量。
    /// </summary>
    public decimal ActualQuantity { get; set; }

    /// <summary>
    /// 实盘减账面的差异数量；正数盘盈、负数盘亏。
    /// </summary>
    public decimal DifferenceQuantity { get; set; }

    /// <summary>
    /// 盘点时的批次单位成本，按系统业务币种和基础单位计量。
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// 差异数量乘以单位成本的金额，可为负数。
    /// </summary>
    public decimal DifferenceAmount { get; set; }

    /// <summary>
    /// 当前批次的盘点差异说明。
    /// </summary>
    public string? Remark { get; set; }
}
