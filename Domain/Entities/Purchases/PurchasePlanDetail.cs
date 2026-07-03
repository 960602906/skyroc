using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Purchases;

/// <summary>
/// 采购计划商品明细，保存采购单位和数量快照。
/// </summary>
public class PurchasePlanDetail : BaseEntity
{
    /// <summary>
    /// 采购计划主键。
    /// </summary>
    public Guid PurchasePlanId { get; set; }

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
    /// 采购计量单位主键。
    /// </summary>
    public Guid PurchaseUnitId { get; set; }

    /// <summary>
    /// 计划生成时的采购单位名称快照。
    /// </summary>
    public string PurchaseUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 来源订单需求数量，按采购单位计量。
    /// </summary>
    public decimal RequiredQuantity { get; set; }

    /// <summary>
    /// 计划采购数量，按采购单位计量。
    /// </summary>
    public decimal PlannedQuantity { get; set; }

    /// <summary>
    /// 已生成采购单的数量，按采购单位计量。
    /// </summary>
    public decimal PurchasedQuantity { get; set; }

    /// <summary>
    /// 业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属采购计划。
    /// </summary>
    public virtual PurchasePlan PurchasePlan { get; set; } = null!;

    /// <summary>
    /// 关联商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 采购计量单位。
    /// </summary>
    public virtual GoodsUnit PurchaseUnit { get; set; } = null!;

    /// <summary>
    /// 来源销售订单明细关系集合。
    /// </summary>
    public virtual ICollection<PurchasePlanOrderRelation> OrderRelations { get; set; } = new List<PurchasePlanOrderRelation>();

    /// <summary>
    /// 已占用当前计划数量的采购单明细关系集合。
    /// </summary>
    public virtual ICollection<PurchaseOrderPlanRelation> PurchaseOrderRelations { get; set; } = new List<PurchaseOrderPlanRelation>();
}
