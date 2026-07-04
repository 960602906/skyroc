using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Storage;

/// <summary>
/// 库存盘点明细，保存批次在盘点时点的账面数、实盘数和差异成本。
/// </summary>
public class StocktakingDetail : BaseEntity
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
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 盘点创建时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 盘点批次号快照。
    /// </summary>
    public string BatchNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 商品基础计量单位主键，盘点数量均按该单位保存。
    /// </summary>
    public Guid BaseUnitId { get; set; }

    /// <summary>
    /// 盘点创建时的基础单位名称快照。
    /// </summary>
    public string BaseUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 盘点快照时的批次账面数量，按基础单位计量。
    /// </summary>
    public decimal BookQuantity { get; set; }

    /// <summary>
    /// 盘点人员录入的实际数量，按基础单位计量。
    /// </summary>
    public decimal ActualQuantity { get; set; }

    /// <summary>
    /// 实盘数量减账面数量，正数盘盈、负数盘亏。
    /// </summary>
    public decimal DifferenceQuantity { get; set; }

    /// <summary>
    /// 盘点时的批次单位成本，按系统业务币种和基础单位计量。
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// 差异数量乘以单位成本的金额快照，可为负数。
    /// </summary>
    public decimal DifferenceAmount { get; set; }

    /// <summary>
    /// 当前批次的盘点差异说明。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属盘点主单。
    /// </summary>
    public virtual StocktakingOrder StocktakingOrder { get; set; } = null!;

    /// <summary>
    /// 被盘点库存批次。
    /// </summary>
    public virtual StockBatch StockBatch { get; set; } = null!;

    /// <summary>
    /// 被盘点商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 盘点数量采用的商品基础单位。
    /// </summary>
    public virtual GoodsUnit BaseUnit { get; set; } = null!;
}
