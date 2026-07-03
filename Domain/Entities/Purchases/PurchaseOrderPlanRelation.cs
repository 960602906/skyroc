namespace Domain.Entities.Purchases;

/// <summary>
/// 采购单明细与来源采购计划明细的关联记录，保存本次采购占用的计划数量。
/// </summary>
public class PurchaseOrderPlanRelation : BaseEntity
{
    /// <summary>
    /// 采购单商品明细主键。
    /// </summary>
    public Guid PurchaseOrderDetailId { get; set; }

    /// <summary>
    /// 来源采购计划商品明细主键。
    /// </summary>
    public Guid PurchasePlanDetailId { get; set; }

    /// <summary>
    /// 本采购单从来源计划明细占用的数量，按双方共同的采购单位计量。
    /// </summary>
    public decimal AllocatedQuantity { get; set; }

    /// <summary>
    /// 关联采购单商品明细。
    /// </summary>
    public virtual PurchaseOrderDetail PurchaseOrderDetail { get; set; } = null!;

    /// <summary>
    /// 关联来源采购计划商品明细；被引用后不得物理删除。
    /// </summary>
    public virtual PurchasePlanDetail PurchasePlanDetail { get; set; } = null!;
}
