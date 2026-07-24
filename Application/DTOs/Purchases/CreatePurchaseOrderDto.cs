using Domain.Entities.Purchases;
namespace Application.DTOs.Purchases;

/// <summary>
/// 手工创建采购单请求，创建结果始终为可编辑草稿。
/// </summary>
public class CreatePurchaseOrderDto
{
    /// <summary>
    /// 供应商主键；供应商直供模式必填，市场自采模式可为空。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 执行采购的采购员主键；草稿阶段可暂不分配。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 采购模式：供应商直供或市场自采。
    /// </summary>
    public PurchasePattern PurchasePattern { get; set; } = PurchasePattern.SupplierDirect;

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
    /// 手工采购商品行，至少包含一项。
    /// </summary>
    public List<CreatePurchaseOrderDetailDto> Details { get; set; } = [];
}
