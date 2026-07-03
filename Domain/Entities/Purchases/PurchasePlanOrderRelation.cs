using Domain.Entities.Orders;

namespace Domain.Entities.Purchases;

/// <summary>
/// 采购计划明细与来源销售订单明细的关联记录。
/// </summary>
public class PurchasePlanOrderRelation : BaseEntity
{
    /// <summary>
    /// 采购计划商品明细主键。
    /// </summary>
    public Guid PurchasePlanDetailId { get; set; }

    /// <summary>
    /// 来源销售订单主键。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 来源销售订单商品明细主键。
    /// </summary>
    public Guid SaleOrderDetailId { get; set; }

    /// <summary>
    /// 来源订单需求数量，按采购单位计量。
    /// </summary>
    public decimal RequiredQuantity { get; set; }

    /// <summary>
    /// 关联采购计划商品明细。
    /// </summary>
    public virtual PurchasePlanDetail PurchasePlanDetail { get; set; } = null!;

    /// <summary>
    /// 来源销售订单。
    /// </summary>
    public virtual SaleOrder SaleOrder { get; set; } = null!;

    /// <summary>
    /// 来源销售订单商品明细。
    /// </summary>
    public virtual SaleOrderDetail SaleOrderDetail { get; set; } = null!;
}
