using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.AfterSales;

/// <summary>
/// 售后商品明细，保存订单商品、单位、供应商、原因、处理方式和退款金额快照。
/// </summary>
public class AfterSaleGoods : BaseEntity
{
    /// <summary>
    /// 所属售后单主键。
    /// </summary>
    public Guid AfterSaleId { get; set; }

    /// <summary>
    /// 来源销售订单商品明细主键；手工录入且未直接关联订单商品时可为空。
    /// </summary>
    public Guid? SaleOrderDetailId { get; set; }

    /// <summary>
    /// 售后商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 售后建单时的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 售后建单时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 售后建单时的商品分类名称快照。
    /// </summary>
    public string? GoodsTypeNameSnapshot { get; set; }

    /// <summary>
    /// 申请退款或退货所使用的商品单位主键。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 售后建单时的申请单位名称快照。
    /// </summary>
    public string GoodsUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 商品基础单位主键；商品未配置基础单位时可为空。
    /// </summary>
    public Guid? BaseUnitId { get; set; }

    /// <summary>
    /// 售后建单时的基础单位名称快照。
    /// </summary>
    public string? BaseUnitNameSnapshot { get; set; }

    /// <summary>
    /// 申请单位换算为商品基础单位的比例，必须大于零。
    /// </summary>
    public decimal ConversionRate { get; set; } = 1m;

    /// <summary>
    /// 售后申请类型，区分仅退款和退货退款。
    /// </summary>
    public AfterSaleType AfterSaleType { get; set; }

    /// <summary>
    /// 最终批准退款或退货的数量，按申请商品单位计量且必须大于零。
    /// </summary>
    public decimal ActualRefundQuantity { get; set; }

    /// <summary>
    /// 最终批准数量换算到商品基础单位后的数量。
    /// </summary>
    public decimal BaseRefundQuantity { get; set; }

    /// <summary>
    /// 售后核算采用的订单单价快照，按申请商品单位计量。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 当前售后商品最终退款或减免金额，按系统业务币种计量。
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 原商品供货供应商主键；来源订单未确定供应商时可为空。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 售后建单时的供应商名称快照。
    /// </summary>
    public string? SupplierNameSnapshot { get; set; }

    /// <summary>
    /// 承担售后责任的部门主键；尚未认定责任时可为空。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 售后建单或定责时的部门名称快照。
    /// </summary>
    public string? DepartmentNameSnapshot { get; set; }

    /// <summary>
    /// 售后原因分类，用于责任统计。
    /// </summary>
    public AfterSaleReasonType ReasonType { get; set; }

    /// <summary>
    /// 售后处理方式，决定后续补货、换货、减免或沟通动作。
    /// </summary>
    public AfterSaleHandleType HandleType { get; set; }

    /// <summary>
    /// 当前售后商品行的原因补充或处理说明。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属售后单。
    /// </summary>
    public virtual AfterSale AfterSale { get; set; } = null!;

    /// <summary>
    /// 来源销售订单商品明细。
    /// </summary>
    public virtual SaleOrderDetail? SaleOrderDetail { get; set; }

    /// <summary>
    /// 售后商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 申请退款或退货所使用的商品单位。
    /// </summary>
    public virtual GoodsUnit GoodsUnit { get; set; } = null!;

    /// <summary>
    /// 商品基础单位。
    /// </summary>
    public virtual GoodsUnit? BaseUnit { get; set; }

    /// <summary>
    /// 原商品供货供应商档案。
    /// </summary>
    public virtual Supplier? Supplier { get; set; }

    /// <summary>
    /// 承担售后责任的部门。
    /// </summary>
    public virtual Department? Department { get; set; }

    /// <summary>
    /// 当前商品需要退货时生成的唯一取货任务；无需取货时为空。
    /// </summary>
    public virtual PickupTask? PickupTask { get; set; }
}
