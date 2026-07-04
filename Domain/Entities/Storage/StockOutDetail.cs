using Domain.Entities.Goods;
using Domain.Entities.Orders;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Storage;

/// <summary>
/// 出库商品明细，保存来源销售行、商品单位、扣减批次、数量和价格快照。
/// </summary>
public class StockOutDetail : BaseEntity
{
    /// <summary>
    /// 所属出库主单主键。
    /// </summary>
    public Guid StockOutOrderId { get; set; }

    /// <summary>
    /// 来源销售订单商品明细主键；非销售出库时为空。
    /// </summary>
    public Guid? SaleOrderDetailId { get; set; }

    /// <summary>
    /// 扣减的库存批次主键；提交审核前必须确定。
    /// </summary>
    public Guid? StockBatchId { get; set; }

    /// <summary>
    /// 出库商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 出库发生时的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 出库发生时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 出库计量单位主键。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 出库发生时的计量单位名称快照。
    /// </summary>
    public string GoodsUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 出库单位换算为商品基础单位的比例，必须大于零。
    /// </summary>
    public decimal ConversionRate { get; set; }

    /// <summary>
    /// 按出库单位计量的商品数量。
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 按商品基础单位换算后的出库数量。
    /// </summary>
    public decimal BaseQuantity { get; set; }

    /// <summary>
    /// 出库价格，按系统业务币种和出库单位计量。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 出库金额，为出库数量乘以价格后的金额快照。
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// 选定库存批次的批次号快照。
    /// </summary>
    public string BatchNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 仅针对当前出库商品行的业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属出库主单。
    /// </summary>
    public virtual StockOutOrder StockOutOrder { get; set; } = null!;

    /// <summary>
    /// 来源销售订单商品明细。
    /// </summary>
    public virtual SaleOrderDetail? SaleOrderDetail { get; set; }

    /// <summary>
    /// 被扣减的库存批次。
    /// </summary>
    public virtual StockBatch? StockBatch { get; set; }

    /// <summary>
    /// 出库商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 出库计量单位。
    /// </summary>
    public virtual GoodsUnit GoodsUnit { get; set; } = null!;
}
