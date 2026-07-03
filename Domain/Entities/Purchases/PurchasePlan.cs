namespace Domain.Entities.Purchases;

/// <summary>
/// 采购计划主单，保存交期、采购模式和执行状态。
/// </summary>
public class PurchasePlan : BaseEntity
{
    /// <summary>
    /// 采购计划业务编号。
    /// </summary>
    public string PlanNo { get; set; } = string.Empty;

    /// <summary>
    /// 计划采购交期（UTC）。
    /// </summary>
    public DateTime PlanDate { get; set; }

    /// <summary>
    /// 采购模式：供应商直供或市场自采。
    /// </summary>
    public PurchasePattern PurchasePattern { get; set; } = PurchasePattern.SupplierDirect;

    /// <summary>
    /// 采购单生成进度状态。
    /// </summary>
    public PurchasePlanStatus PurchaseStatus { get; set; } = PurchasePlanStatus.Unpublished;

    /// <summary>
    /// 关联供应商主键。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 计划生成时的供应商名称快照。
    /// </summary>
    public string? SupplierNameSnapshot { get; set; }

    /// <summary>
    /// 负责采购的采购员主键。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 计划生成时的采购员名称快照。
    /// </summary>
    public string? PurchaserNameSnapshot { get; set; }

    /// <summary>
    /// 业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 计划供应商导航属性。
    /// </summary>
    public virtual Supplier? Supplier { get; set; }

    /// <summary>
    /// 计划采购员导航属性。
    /// </summary>
    public virtual Purchaser? Purchaser { get; set; }

    /// <summary>
    /// 采购计划商品明细集合。
    /// </summary>
    public virtual ICollection<PurchasePlanDetail> Details { get; set; } = new List<PurchasePlanDetail>();
}
