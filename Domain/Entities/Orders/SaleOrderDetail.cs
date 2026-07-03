using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单商品明细，保存商品、单位、价格和验收快照。
/// </summary>
public class SaleOrderDetail : BaseEntity
{
    /// <summary>
    /// 来源销售订单主键。
    /// </summary>
    public Guid SaleOrderId { get; set; }

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
    /// 业务发生时的商品图片地址快照。
    /// </summary>
    public string? GoodsImageSnapshot { get; set; }

    /// <summary>
    /// 业务发生时的商品分类名称快照。
    /// </summary>
    public string? GoodsTypeNameSnapshot { get; set; }

    /// <summary>
    /// 业务发生时的商品描述快照。
    /// </summary>
    public string? GoodsDescriptionSnapshot { get; set; }

    /// <summary>
    /// 下单商品单位主键。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 下单时的商品单位名称快照。
    /// </summary>
    public string GoodsUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 业务数量，按当前商品单位计量。
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 按商品基础单位换算后的数量。
    /// </summary>
    public decimal BaseQuantity { get; set; }

    /// <summary>
    /// 商品基础单位主键。
    /// </summary>
    public Guid? BaseUnitId { get; set; }

    /// <summary>
    /// 业务发生时的基础单位名称快照。
    /// </summary>
    public string? BaseUnitNameSnapshot { get; set; }

    /// <summary>
    /// 下单单位换算为基础单位的比例。
    /// </summary>
    public decimal UnitConversion { get; set; } = 1m;

    /// <summary>
    /// 订单商品固定单价。
    /// </summary>
    public decimal FixedPrice { get; set; }

    /// <summary>
    /// 计价单位主键。
    /// </summary>
    public Guid? FixedGoodsUnitId { get; set; }

    /// <summary>
    /// 下单时的计价单位名称快照。
    /// </summary>
    public string? FixedGoodsUnitNameSnapshot { get; set; }

    /// <summary>
    /// 数量与单价计算后的总金额。
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// 业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 仅内部人员可见的备注。
    /// </summary>
    public string? InnerRemark { get; set; }

    /// <summary>
    /// 客户验收状态。
    /// </summary>
    public OrderCustomerCheckStatus CustomerCheckStatus { get; set; } = OrderCustomerCheckStatus.Pending;

    /// <summary>
    /// 客户验收数量，按基础单位计量。
    /// </summary>
    public decimal? CustomerCheckBaseQuantity { get; set; }

    /// <summary>
    /// 客户验收确认金额。
    /// </summary>
    public decimal? CustomerCheckPrice { get; set; }

    /// <summary>
    /// 是否已生成采购计划。
    /// </summary>
    public bool HasPurchasePlan { get; set; }

    /// <summary>
    /// 所属销售订单。
    /// </summary>
    public virtual SaleOrder SaleOrder { get; set; } = null!;

    /// <summary>
    /// 关联商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 下单商品单位。
    /// </summary>
    public virtual GoodsUnit GoodsUnit { get; set; } = null!;

    /// <summary>
    /// 商品基础单位。
    /// </summary>
    public virtual GoodsUnit? BaseUnit { get; set; }

    /// <summary>
    /// 订单计价单位。
    /// </summary>
    public virtual GoodsUnit? FixedGoodsUnit { get; set; }
}
