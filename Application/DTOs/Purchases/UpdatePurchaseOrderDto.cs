using Domain.Entities.Purchases;
namespace Application.DTOs.Purchases;

/// <summary>
/// 编辑采购单及其全部商品行的请求，仅草稿采购单可执行。
/// </summary>
public class UpdatePurchaseOrderDto
{
    /// <summary>
    /// 待编辑采购单主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 供应商主键；供应商直供模式必填，市场自采模式可为空。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 执行采购的采购员主键；完成采购单前必须分配。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 采购模式：供应商直供或市场自采。
    /// </summary>
    public PurchasePattern PurchasePattern { get; set; }

    /// <summary>
    /// 预计到货时间（UTC）；尚未确认时可为空。
    /// </summary>
    public DateTime? ReceiveTime { get; set; }

    /// <summary>
    /// 供应商联系人姓名快照；未传时使用供应商档案当前值。
    /// </summary>
    public string? SupplierContactName { get; set; }

    /// <summary>
    /// 供应商联系人电话快照；未传时使用供应商档案当前值。
    /// </summary>
    public string? SupplierContactPhone { get; set; }

    /// <summary>
    /// 采购单级备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 替换后的完整商品行集合，至少包含一项。
    /// </summary>
    public List<UpdatePurchaseOrderDetailDto> Details { get; set; } = [];
}
