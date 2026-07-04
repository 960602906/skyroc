using Shared.Constants;

namespace Application.DTOs.Storage;

/// <summary>
/// 库存批次详情，展示仓库商品在单一批次下的余额、成本和效期。
/// </summary>
public class StockBatchDto
{
    /// <summary>
    /// 库存批次主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 批次所在仓库主键。
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
    /// 批次所属商品主键。
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
    /// 仓库内商品批次号。
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 商品基础单位主键。
    /// </summary>
    public Guid BaseUnitId { get; set; }

    /// <summary>
    /// 商品基础单位名称；批次数量均按该单位计量。
    /// </summary>
    public string BaseUnitName { get; set; } = string.Empty;

    /// <summary>
    /// 当前账面数量，按基础单位计量。
    /// </summary>
    public decimal CurrentQuantity { get; set; }

    /// <summary>
    /// 可出库数量，按基础单位计量。
    /// </summary>
    public decimal AvailableQuantity { get; set; }

    /// <summary>
    /// 已占用数量，为账面数量减可用数量。
    /// </summary>
    public decimal OccupiedQuantity => NumericPrecision.RoundQuantity(CurrentQuantity - AvailableQuantity);

    /// <summary>
    /// 当前移动加权单位成本，按系统业务币种和基础单位计量。
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// 批次当前货值，为账面数量乘单位成本。
    /// </summary>
    public decimal StockValue => NumericPrecision.RoundMoney(CurrentQuantity * UnitCost);

    /// <summary>
    /// 商品生产日期；未知时为空。
    /// </summary>
    public DateOnly? ProductDate { get; set; }

    /// <summary>
    /// 商品到期日期；无保质期或未知时为空。
    /// </summary>
    public DateOnly? ExpireDate { get; set; }

    /// <summary>
    /// 最近一次审核库存变化时间（UTC）。
    /// </summary>
    public DateTime? LastMovementTime { get; set; }
}
