namespace Domain.ReadModels.Storage;

/// <summary>
/// 单个仓库商品的库存聚合快照，数量按商品基础单位汇总。
/// </summary>
public class StockOverviewReadModel
{
    /// <summary>
    /// 库存所在仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 当前仓库名称。
    /// </summary>
    public string WareName { get; set; } = string.Empty;

    /// <summary>
    /// 商品分类主键。
    /// </summary>
    public Guid GoodsTypeId { get; set; }

    /// <summary>
    /// 当前商品分类名称。
    /// </summary>
    public string GoodsTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 当前商品名称。
    /// </summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 当前商品编码。
    /// </summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>
    /// 商品基础单位主键。
    /// </summary>
    public Guid BaseUnitId { get; set; }

    /// <summary>
    /// 当前商品基础单位名称。
    /// </summary>
    public string BaseUnitName { get; set; } = string.Empty;

    /// <summary>
    /// 汇总的账面数量，按商品基础单位计量。
    /// </summary>
    public decimal CurrentQuantity { get; set; }

    /// <summary>
    /// 汇总的可出库数量，按商品基础单位计量。
    /// </summary>
    public decimal AvailableQuantity { get; set; }

    /// <summary>
    /// 批次数量与单位成本乘积的汇总货值，按系统业务币种计量。
    /// </summary>
    public decimal StockValue { get; set; }

    /// <summary>
    /// 该仓库商品最近一次库存变化时间（UTC）。
    /// </summary>
    public DateTime? LastMovementTime { get; set; }
}
