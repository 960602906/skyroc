namespace Domain.Entities.Purchases;

/// <summary>
/// 采购单主单，保存供货方、采购责任人、预计到货时间和执行状态快照。
/// </summary>
public class PurchaseOrder : BaseEntity
{
    /// <summary>
    /// 采购单业务编号，在采购单生命周期内保持唯一。
    /// </summary>
    public string PurchaseNo { get; set; } = string.Empty;

    /// <summary>
    /// 关联供应商主键；市场自采且尚未确定供货方时可为空。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 采购单创建或更新时的供应商名称快照。
    /// </summary>
    public string? SupplierNameSnapshot { get; set; }

    /// <summary>
    /// 负责执行采购的采购员主键；草稿阶段尚未分配时可为空。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 采购单创建或更新时的采购员名称快照。
    /// </summary>
    public string? PurchaserNameSnapshot { get; set; }

    /// <summary>
    /// 采购模式：供应商直供或市场自采。
    /// </summary>
    public PurchasePattern PurchasePattern { get; set; } = PurchasePattern.SupplierDirect;

    /// <summary>
    /// 预计到货时间（UTC）；尚未确认到货安排时可为空。
    /// </summary>
    public DateTime? ReceiveTime { get; set; }

    /// <summary>
    /// 采购单当前执行状态，完成或取消后不得再按草稿方式修改。
    /// </summary>
    public PurchaseOrderStatus BusinessStatus { get; set; } = PurchaseOrderStatus.Draft;

    /// <summary>
    /// 业务发生时的供应商联系人姓名快照。
    /// </summary>
    public string? SupplierContactNameSnapshot { get; set; }

    /// <summary>
    /// 业务发生时的供应商联系人电话快照。
    /// </summary>
    public string? SupplierContactPhoneSnapshot { get; set; }

    /// <summary>
    /// 采购单级业务备注，对该单全部商品生效。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 采购单关联的供应商档案。
    /// </summary>
    public virtual Supplier? Supplier { get; set; }

    /// <summary>
    /// 采购单关联的采购员档案。
    /// </summary>
    public virtual Purchaser? Purchaser { get; set; }

    /// <summary>
    /// 采购单商品明细集合；随采购单删除而删除。
    /// </summary>
    public virtual ICollection<PurchaseOrderDetail> Details { get; set; } = new List<PurchaseOrderDetail>();
}
