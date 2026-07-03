using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Purchases;

/// <summary>
/// 采购计划商品明细，保存采购单位和数量快照。
/// </summary>
public class PurchasePlanDetail : BaseEntity
{
    public Guid PurchasePlanId { get; set; }

    public Guid GoodsId { get; set; }

    public string GoodsNameSnapshot { get; set; } = string.Empty;

    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    public Guid PurchaseUnitId { get; set; }

    public string PurchaseUnitNameSnapshot { get; set; } = string.Empty;

    public decimal RequiredQuantity { get; set; }

    public decimal PlannedQuantity { get; set; }

    public decimal PurchasedQuantity { get; set; }

    public string? Remark { get; set; }

    public virtual PurchasePlan PurchasePlan { get; set; } = null!;

    public virtual GoodsEntity Goods { get; set; } = null!;

    public virtual GoodsUnit PurchaseUnit { get; set; } = null!;

    public virtual ICollection<PurchasePlanOrderRelation> OrderRelations { get; set; } = new List<PurchasePlanOrderRelation>();
}
