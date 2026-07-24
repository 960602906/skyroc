using Domain.Entities.Purchases;
namespace Application.DTOs.Purchases;

/// <summary>
/// 采购单 DTO，返回供货责任快照、执行状态及采购商品明细。
/// </summary>
public class PurchaseOrderDto : BaseDto
{
    /// <summary>
    /// 采购单业务编号。
    /// </summary>
    public string PurchaseNo { get; set; } = string.Empty;

    /// <summary>
    /// 供应商主键；市场自采且未指定供货方时为空。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 采购单保存时的供应商名称快照。
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// 执行采购的采购员主键；未分配时为空。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 采购单保存时的采购员名称快照。
    /// </summary>
    public string? PurchaserName { get; set; }

    /// <summary>
    /// 采购模式：供应商直供或市场自采。
    /// </summary>
    public PurchasePattern PurchasePattern { get; set; }

    /// <summary>
    /// 预计到货时间（UTC）；尚未确认时为空。
    /// </summary>
    public DateTime? ReceiveTime { get; set; }

    /// <summary>
    /// 采购单执行状态：草稿、已完成或已取消。
    /// </summary>
    public PurchaseOrderStatus BusinessStatus { get; set; }

    /// <summary>
    /// 业务发生时的供应商联系人姓名快照。
    /// </summary>
    public string? SupplierContactName { get; set; }

    /// <summary>
    /// 业务发生时的供应商联系人电话快照。
    /// </summary>
    public string? SupplierContactPhone { get; set; }

    /// <summary>
    /// 采购单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 采购商品明细集合。
    /// </summary>
    public List<PurchaseOrderDetailDto> Details { get; set; } = [];
}
