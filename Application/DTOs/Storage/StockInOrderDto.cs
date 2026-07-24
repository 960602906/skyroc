using Domain.Entities.Purchases;
using Domain.Entities.Storage;
namespace Application.DTOs.Storage;

/// <summary>
/// 入库单 DTO，返回入库来源、仓库、业务方、审核状态及商品明细快照。
/// </summary>
public class StockInOrderDto : BaseDto
{
    /// <summary>
    /// 入库单业务编号。
    /// </summary>
    public string InNo { get; set; } = string.Empty;

    /// <summary>
    /// 入库业务类型：采购、其他或销售退货。
    /// </summary>
    public StockInOrderType OrderType { get; set; }

    /// <summary>
    /// 单据业务状态：草稿、待审核、已审核、已反审核或已删除。
    /// </summary>
    public StockDocumentStatus BusinessStatus { get; set; }

    /// <summary>
    /// 接收入库商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 单据创建时的仓库名称快照。
    /// </summary>
    public string WareName { get; set; } = string.Empty;

    /// <summary>
    /// 来源采购单主键；仅采购入库时填写。
    /// </summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>
    /// 来源售后单主键；手工销售退货入库以及其他入库类型为空。
    /// </summary>
    public Guid? AfterSaleId { get; set; }

    /// <summary>
    /// 供货供应商主键；采购入库时通常填写。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 入库业务发生时的供应商名称快照。
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// 退货客户主键；仅销售退货入库时填写。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 入库业务发生时的客户名称快照。
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 发起入库业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 入库业务发生时的部门名称快照。
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 负责采购到货的采购员主键；非采购入库可为空。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 入库业务发生时的采购员名称快照。
    /// </summary>
    public string? PurchaserName { get; set; }

    /// <summary>
    /// 采购入库采用的采购模式；其他入库和销售退货入库为空。
    /// </summary>
    public PurchasePattern? PurchasePattern { get; set; }

    /// <summary>
    /// 计划或实际入库时间（UTC）。
    /// </summary>
    public DateTime InTime { get; set; }

    /// <summary>
    /// 预计到货时间（UTC）；尚未确认时为空。
    /// </summary>
    public DateTime? ExpectedArrivalTime { get; set; }

    /// <summary>
    /// 入库基础单位数量合计，仅用于展示。
    /// </summary>
    public decimal TotalBaseQuantity { get; set; }

    /// <summary>
    /// 入库金额合计，按系统业务币种计量。
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 单据打印状态：0 未打印，1 已打印。
    /// </summary>
    public StockPrintStatus PrintStatus { get; set; }

    /// <summary>
    /// 最近一次审核人的系统用户主键。
    /// </summary>
    public Guid? AuditUserId { get; set; }

    /// <summary>
    /// 最近一次审核时的用户名称快照。
    /// </summary>
    public string? AuditUserName { get; set; }

    /// <summary>
    /// 最近一次审核通过时间（UTC）。
    /// </summary>
    public DateTime? AuditTime { get; set; }

    /// <summary>
    /// 最近一次反审核人的系统用户主键。
    /// </summary>
    public Guid? ReverseUserId { get; set; }

    /// <summary>
    /// 最近一次反审核时的用户名称快照。
    /// </summary>
    public string? ReverseUserName { get; set; }

    /// <summary>
    /// 最近一次反审核完成时间（UTC）。
    /// </summary>
    public DateTime? ReverseTime { get; set; }

    /// <summary>
    /// 入库单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 入库商品明细集合。
    /// </summary>
    public List<StockInDetailDto> Details { get; set; } = [];
}
