using Domain.Entities.Goods;
using Domain.Entities.AfterSales;
using Domain.Entities.Purchases;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Storage;

/// <summary>
/// 入库商品明细，保存来源采购行、商品单位、批次、数量和成本快照。
/// </summary>
public class StockInDetail : BaseEntity
{
    /// <summary>
    /// 所属入库主单主键。
    /// </summary>
    public Guid StockInOrderId { get; set; }

    /// <summary>
    /// 来源采购单商品明细主键；非采购入库时为空。
    /// </summary>
    public Guid? PurchaseOrderDetailId { get; set; }

    /// <summary>
    /// 来源售后取货任务主键；同一已完成任务最多生成一条销售退货入库明细。
    /// </summary>
    public Guid? PickupTaskId { get; set; }

    /// <summary>
    /// 审核入库后对应的库存批次主键；草稿阶段可为空。
    /// </summary>
    public Guid? StockBatchId { get; set; }

    /// <summary>
    /// 入库商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 入库发生时的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 入库发生时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 入库计量单位主键。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 入库发生时的计量单位名称快照。
    /// </summary>
    public string GoodsUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 入库单位换算为商品基础单位的比例，必须大于零。
    /// </summary>
    public decimal ConversionRate { get; set; }

    /// <summary>
    /// 按入库单位计量的商品数量。
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 按商品基础单位换算后的入库数量。
    /// </summary>
    public decimal BaseQuantity { get; set; }

    /// <summary>
    /// 入库单价，按系统业务币种和入库单位计量。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 入库金额，为入库数量乘以单价后的金额快照。
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// 商品批次号；同仓库同商品下用于定位唯一库存批次。
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 商品生产日期，仅记录自然日；未知时可为空。
    /// </summary>
    public DateOnly? ProductDate { get; set; }

    /// <summary>
    /// 商品到期日期，仅记录自然日；无保质期或未知时可为空。
    /// </summary>
    public DateOnly? ExpireDate { get; set; }

    /// <summary>
    /// 仅针对当前入库商品行的业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属入库主单。
    /// </summary>
    public virtual StockInOrder StockInOrder { get; set; } = null!;

    /// <summary>
    /// 来源采购单商品明细。
    /// </summary>
    public virtual PurchaseOrderDetail? PurchaseOrderDetail { get; set; }

    /// <summary>
    /// 来源售后取货任务，用于校验退回商品、单位和数量并防止重复入库。
    /// </summary>
    public virtual PickupTask? PickupTask { get; set; }

    /// <summary>
    /// 审核入库后对应的库存批次。
    /// </summary>
    public virtual StockBatch? StockBatch { get; set; }

    /// <summary>
    /// 入库商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 入库计量单位。
    /// </summary>
    public virtual GoodsUnit GoodsUnit { get; set; } = null!;
}
