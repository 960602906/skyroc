namespace Domain.Entities.Purchases;

/// <summary>
/// 采购计划主单，保存交期、采购模式和执行状态。
/// </summary>
public class PurchasePlan : BaseEntity
{
    public string PlanNo { get; set; } = string.Empty;

    public DateTime PlanDate { get; set; }

    public PurchasePattern PurchasePattern { get; set; } = PurchasePattern.SupplierDirect;

    public PurchasePlanStatus PurchaseStatus { get; set; } = PurchasePlanStatus.Unpublished;

    public Guid? SupplierId { get; set; }

    public string? SupplierNameSnapshot { get; set; }

    public Guid? PurchaserId { get; set; }

    public string? PurchaserNameSnapshot { get; set; }

    public string? Remark { get; set; }

    public virtual Supplier? Supplier { get; set; }

    public virtual Purchaser? Purchaser { get; set; }

    public virtual ICollection<PurchasePlanDetail> Details { get; set; } = new List<PurchasePlanDetail>();
}
