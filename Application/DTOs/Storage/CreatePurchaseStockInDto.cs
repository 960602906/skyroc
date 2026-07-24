using Domain.Entities.Purchases;
namespace Application.DTOs.Storage;

/// <summary>
/// 采购入库创建请求，创建结果始终为可编辑草稿。
/// </summary>
public class CreatePurchaseStockInDto
{
    /// <summary>
    /// 接收入库商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 来源采购单主键；用于回填供应商、采购员和采购模式并支持追溯。
    /// </summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>
    /// 供货供应商主键；供应商直供采购入库时必填。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 发起入库业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 负责采购到货的采购员主键。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 采购模式：供应商直供或市场自采。
    /// </summary>
    public PurchasePattern PurchasePattern { get; set; } = PurchasePattern.SupplierDirect;

    /// <summary>
    /// 计划或实际入库时间（UTC）。
    /// </summary>
    public DateTime InTime { get; set; }

    /// <summary>
    /// 预计到货时间（UTC）；尚未确认时可为空。
    /// </summary>
    public DateTime? ExpectedArrivalTime { get; set; }

    /// <summary>
    /// 入库单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 采购入库商品行，至少包含一项。
    /// </summary>
    public List<CreateStockInDetailDto> Details { get; set; } = [];
}
