using Domain.Entities.Storage;
namespace Application.DTOs.Storage;

/// <summary>
/// 出库单 DTO，返回出库来源、仓库、业务方、审核状态及商品批次明细快照。
/// </summary>
public class StockOutOrderDto : BaseDto
{
    /// <summary>
    /// 出库单业务编号。
    /// </summary>
    public string OutNo { get; set; } = string.Empty;

    /// <summary>
    /// 出库业务类型：销售、采购退货或其他。
    /// </summary>
    public StockOutOrderType OrderType { get; set; }

    /// <summary>
    /// 单据业务状态：草稿、待审核、已审核、已反审核或已删除。
    /// </summary>
    public StockDocumentStatus BusinessStatus { get; set; }

    /// <summary>
    /// 发出商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 单据创建时的仓库名称快照。
    /// </summary>
    public string WareName { get; set; } = string.Empty;

    /// <summary>
    /// 来源销售订单主键；手工销售出库及非销售出库为空。
    /// </summary>
    public Guid? SaleOrderId { get; set; }

    /// <summary>
    /// 收货客户主键；销售出库时填写。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 出库业务发生时的客户名称快照。
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 退货供应商主键；采购退货出库时填写。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 出库业务发生时的供应商名称快照。
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// 发起出库业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 出库业务发生时的部门名称快照。
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 计划或实际出库时间（UTC）。
    /// </summary>
    public DateTime OutTime { get; set; }

    /// <summary>
    /// 出库基础单位数量合计，仅用于展示。
    /// </summary>
    public decimal TotalBaseQuantity { get; set; }

    /// <summary>
    /// 出库业务金额合计，按系统业务币种计量。
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
    /// 出库单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 出库商品批次明细集合。
    /// </summary>
    public List<StockOutDetailDto> Details { get; set; } = [];
}
