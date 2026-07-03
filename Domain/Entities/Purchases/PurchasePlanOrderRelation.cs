using Domain.Entities.Orders;

namespace Domain.Entities.Purchases;

/// <summary>
/// 采购计划明细与来源销售订单明细的关联记录。
/// </summary>
public class PurchasePlanOrderRelation : BaseEntity
{
    public Guid PurchasePlanDetailId { get; set; }

    public Guid SaleOrderId { get; set; }

    public Guid SaleOrderDetailId { get; set; }

    public decimal RequiredQuantity { get; set; }

    public virtual PurchasePlanDetail PurchasePlanDetail { get; set; } = null!;

    public virtual SaleOrder SaleOrder { get; set; } = null!;

    public virtual SaleOrderDetail SaleOrderDetail { get; set; } = null!;
}
