using Shared.Constants;

namespace Application.DTOs.Storage;

/// <summary>
/// 仓库商品库存总览，按基础单位汇总全部批次余额和货值。
/// </summary>
public class StockOverviewDto
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
    /// 商品基础单位名称；所有数量均按该单位计量。
    /// </summary>
    public string BaseUnitName { get; set; } = string.Empty;

    /// <summary>
    /// 全部批次的当前账面数量合计，按基础单位计量。
    /// </summary>
    public decimal CurrentQuantity { get; set; }

    /// <summary>
    /// 全部批次的可出库数量合计，按基础单位计量。
    /// </summary>
    public decimal AvailableQuantity { get; set; }

    /// <summary>
    /// 已被业务占用的数量，为账面数量减可用数量。
    /// </summary>
    public decimal OccupiedQuantity => NumericPrecision.RoundQuantity(CurrentQuantity - AvailableQuantity);

    /// <summary>
    /// 按批次数量加权的单位成本，按系统业务币种和基础单位计量。
    /// </summary>
    public decimal WeightedUnitCost => CurrentQuantity == 0m
        ? 0m
        : NumericPrecision.RoundMoney(StockValue / CurrentQuantity);

    /// <summary>
    /// 全部批次数量乘单位成本后的货值合计，按系统业务币种计量。
    /// </summary>
    public decimal StockValue { get; set; }

    /// <summary>
    /// 该仓库商品最近一次库存变化时间（UTC）。
    /// </summary>
    public DateTime? LastMovementTime { get; set; }
}
