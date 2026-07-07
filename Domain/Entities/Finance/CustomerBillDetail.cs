using Domain.Entities.AfterSales;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Finance;

/// <summary>
/// 客户账单明细，记录单个订单商品验收或售后商品调整对客户应收的影响。
/// </summary>
public class CustomerBillDetail : BaseEntity
{
    /// <summary>
    /// 所属客户账单主键。
    /// </summary>
    public Guid CustomerBillId { get; set; }

    /// <summary>
    /// 明细来源类型，决定金额方向和来源单据语义。
    /// </summary>
    public CustomerBillDetailSourceType SourceType { get; set; }

    /// <summary>
    /// 来源业务单据主键；订单验收为销售订单主键，售后调整为售后单主键。
    /// </summary>
    public Guid SourceDocumentId { get; set; }

    /// <summary>
    /// 来源业务明细主键；订单验收为销售订单商品行，售后调整为售后商品行。
    /// </summary>
    public Guid SourceDetailId { get; set; }

    /// <summary>
    /// 来源销售订单商品明细主键，用于追溯原始销售商品行。
    /// </summary>
    public Guid? SaleOrderDetailId { get; set; }

    /// <summary>
    /// 来源售后单主键；订单验收明细为空。
    /// </summary>
    public Guid? AfterSaleId { get; set; }

    /// <summary>
    /// 来源售后商品明细主键；订单验收明细为空。
    /// </summary>
    public Guid? AfterSaleGoodsId { get; set; }

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
    /// 账单数量，正数表示确认销售，负数表示售后冲减，按当前商品单位计量。
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 账单基础数量，正数表示确认销售，负数表示售后冲减，按基础单位计量。
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
    /// 当前明细对应的应收金额，正数增加应收，负数冲减应收。
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// 来源业务发生时间（UTC），订单验收取签收完成时间，售后调整取售后完成时间。
    /// </summary>
    public DateTime BusinessTime { get; set; }

    /// <summary>
    /// 账单明细备注，记录验收差异、售后原因或调整说明。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属客户账单。
    /// </summary>
    public virtual CustomerBill CustomerBill { get; set; } = null!;

    /// <summary>
    /// 来源销售订单商品明细。
    /// </summary>
    public virtual SaleOrderDetail? SaleOrderDetail { get; set; }

    /// <summary>
    /// 来源售后单。
    /// </summary>
    public virtual AfterSale? AfterSale { get; set; }

    /// <summary>
    /// 来源售后商品明细。
    /// </summary>
    public virtual AfterSaleGoods? AfterSaleGoods { get; set; }

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
