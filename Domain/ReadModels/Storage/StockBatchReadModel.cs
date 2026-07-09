namespace Domain.ReadModels.Storage;

/// <summary>
/// 单个库存批次的查询投影，仅承载批次分页展示所需的余额、成本、效期及关联资料名称。
/// </summary>
public class StockBatchReadModel
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
    /// 当前移动加权单位成本，按系统业务币种和基础单位计量。
    /// </summary>
    public decimal UnitCost { get; set; }

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
