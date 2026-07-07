using Domain.Entities.Goods;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Finance;

/// <summary>
/// 供应商待结单据明细，记录单个入库或退货商品行对供应商应付的影响。
/// </summary>
public class SupplierBillDetail : BaseEntity
{
    /// <summary>
    /// 所属供应商待结单据主键。
    /// </summary>
    public Guid SupplierBillId { get; set; }

    /// <summary>
    /// 明细来源类型，决定金额方向。
    /// </summary>
    public SupplierBillSourceType SourceType { get; set; }

    /// <summary>
    /// 来源出入库主单主键。
    /// </summary>
    public Guid SourceDocumentId { get; set; }

    /// <summary>
    /// 来源出入库商品明细主键，用于幂等识别。
    /// </summary>
    public Guid SourceDetailId { get; set; }

    /// <summary>
    /// 来源采购入库单主键；采购退货明细为空。
    /// </summary>
    public Guid? StockInOrderId { get; set; }

    /// <summary>
    /// 来源采购入库商品明细主键；采购退货明细为空。
    /// </summary>
    public Guid? StockInDetailId { get; set; }

    /// <summary>
    /// 来源采购退货出库单主键；采购入库明细为空。
    /// </summary>
    public Guid? StockOutOrderId { get; set; }

    /// <summary>
    /// 来源采购退货出库商品明细主键；采购入库明细为空。
    /// </summary>
    public Guid? StockOutDetailId { get; set; }

    /// <summary>
    /// 关联商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 业务发生时的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 业务发生时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 业务发生时的商品分类名称快照。
    /// </summary>
    public string? GoodsTypeNameSnapshot { get; set; }

    /// <summary>
    /// 当前明细使用的商品单位主键。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 当前明细使用的商品单位名称快照。
    /// </summary>
    public string GoodsUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 商品基础单位主键；商品未配置基础单位时可为空。
    /// </summary>
    public Guid? BaseUnitId { get; set; }

    /// <summary>
    /// 业务发生时的基础单位名称快照。
    /// </summary>
    public string? BaseUnitNameSnapshot { get; set; }

    /// <summary>
    /// 账单数量，正数表示采购入库、负数表示采购退货，按当前商品单位计量。
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 账单基础数量，正数表示采购入库、负数表示采购退货，按基础单位计量。
    /// </summary>
    public decimal BaseQuantity { get; set; }

    /// <summary>
    /// 当前单位换算为基础单位的比例，必须大于零。
    /// </summary>
    public decimal ConversionRate { get; set; } = 1m;

    /// <summary>
    /// 账单采用的商品单价，按当前商品单位和系统业务币种计量。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 当前明细对应的应付金额，正数增加应付、负数冲减应付。
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 来源业务发生时间（UTC），取出入库审核时间。
    /// </summary>
    public DateTime BusinessTime { get; set; }

    /// <summary>
    /// 账单明细备注，记录价格差异或退货原因。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属供应商待结单据。
    /// </summary>
    public virtual SupplierBill SupplierBill { get; set; } = null!;

    /// <summary>
    /// 来源采购入库单。
    /// </summary>
    public virtual StockInOrder? StockInOrder { get; set; }

    /// <summary>
    /// 来源采购入库商品明细。
    /// </summary>
    public virtual StockInDetail? StockInDetail { get; set; }

    /// <summary>
    /// 来源采购退货出库单。
    /// </summary>
    public virtual StockOutOrder? StockOutOrder { get; set; }

    /// <summary>
    /// 来源采购退货出库商品明细。
    /// </summary>
    public virtual StockOutDetail? StockOutDetail { get; set; }

    /// <summary>
    /// 关联商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 当前明细使用的商品单位。
    /// </summary>
    public virtual GoodsUnit GoodsUnit { get; set; } = null!;

    /// <summary>
    /// 商品基础单位。
    /// </summary>
    public virtual GoodsUnit? BaseUnit { get; set; }
}
