using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Storage;

/// <summary>
/// 库存批次，保存单个仓库内商品批次的当前数量、可用数量和成本快照。
/// </summary>
public class StockBatch : BaseEntity
{
    /// <summary>
    /// 批次所在仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 批次所属商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 入库时记录的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 入库时记录的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 仓库内商品批次号；同一仓库和商品下保持唯一。
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 商品基础计量单位主键，批次数量均按该单位保存。
    /// </summary>
    public Guid BaseUnitId { get; set; }

    /// <summary>
    /// 批次建立时的基础单位名称快照。
    /// </summary>
    public string BaseUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 当前账面数量，按商品基础单位计量。
    /// </summary>
    public decimal CurrentQuantity { get; set; }

    /// <summary>
    /// 扣除已占用数量后的可出库数量，按商品基础单位计量。
    /// </summary>
    public decimal AvailableQuantity { get; set; }

    /// <summary>
    /// 当前移动加权单位成本，按系统业务币种和基础单位计量。
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// 商品生产日期，仅记录自然日；未知时可为空。
    /// </summary>
    public DateOnly? ProductDate { get; set; }

    /// <summary>
    /// 商品到期日期，仅记录自然日；无保质期或未知时可为空。
    /// </summary>
    public DateOnly? ExpireDate { get; set; }

    /// <summary>
    /// 最近一次审核库存变更发生时间（UTC）。
    /// </summary>
    public DateTime? LastMovementTime { get; set; }

    /// <summary>
    /// 批次所在仓库。
    /// </summary>
    public virtual Ware Ware { get; set; } = null!;

    /// <summary>
    /// 批次所属商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 批次数量采用的商品基础单位。
    /// </summary>
    public virtual GoodsUnit BaseUnit { get; set; } = null!;

    /// <summary>
    /// 影响该批次余额的库存流水集合。
    /// </summary>
    public virtual ICollection<StockLedger> Ledgers { get; set; } = new List<StockLedger>();
}
