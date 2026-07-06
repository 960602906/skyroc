using Domain.Entities.Customers;
using Domain.Entities.AfterSales;
using Domain.Entities.Purchases;

namespace Domain.Entities.Storage;

/// <summary>
/// 入库主单，保存入库来源、仓库、业务方、审核状态和金额数量汇总快照。
/// </summary>
public class StockInOrder : BaseEntity
{
    /// <summary>
    /// 入库单业务编号，在全部入库类型中保持唯一。
    /// </summary>
    public string InNo { get; set; } = string.Empty;

    /// <summary>
    /// 入库业务类型：采购、其他或销售退货。
    /// </summary>
    public StockInOrderType OrderType { get; set; }

    /// <summary>
    /// 单据业务状态，只有审核通过后才增加库存。
    /// </summary>
    public StockDocumentStatus BusinessStatus { get; set; } = StockDocumentStatus.Draft;

    /// <summary>
    /// 接收入库商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 单据创建时的仓库名称快照。
    /// </summary>
    public string WareNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 来源采购单主键；仅采购入库时填写。
    /// </summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>
    /// 来源售后单主键；仅由已完成取货任务生成的销售退货入库填写。
    /// </summary>
    public Guid? AfterSaleId { get; set; }

    /// <summary>
    /// 供货供应商主键；采购入库时通常填写。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 入库业务发生时的供应商名称快照。
    /// </summary>
    public string? SupplierNameSnapshot { get; set; }

    /// <summary>
    /// 退货客户主键；仅销售退货入库时填写。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 入库业务发生时的客户名称快照。
    /// </summary>
    public string? CustomerNameSnapshot { get; set; }

    /// <summary>
    /// 发起入库业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 入库业务发生时的部门名称快照。
    /// </summary>
    public string? DepartmentNameSnapshot { get; set; }

    /// <summary>
    /// 负责采购到货的采购员主键；非采购入库可为空。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 入库业务发生时的采购员名称快照。
    /// </summary>
    public string? PurchaserNameSnapshot { get; set; }

    /// <summary>
    /// 采购入库采用的采购模式；其他入库和销售退货入库为空。
    /// </summary>
    public PurchasePattern? PurchasePattern { get; set; }

    /// <summary>
    /// 计划或实际入库时间（UTC）。
    /// </summary>
    public DateTime InTime { get; set; }

    /// <summary>
    /// 预计到货时间（UTC）；尚未确认时可为空。
    /// </summary>
    public DateTime? ExpectedArrivalTime { get; set; }

    /// <summary>
    /// 入库基础单位数量合计，按各商品基础单位分别求和，仅用于单据展示。
    /// </summary>
    public decimal TotalBaseQuantity { get; set; }

    /// <summary>
    /// 入库金额合计，按系统业务币种计量。
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 单据是否至少完成过一次正式打印。
    /// </summary>
    public StockPrintStatus PrintStatus { get; set; } = StockPrintStatus.NotPrinted;

    /// <summary>
    /// 最近一次审核人的系统用户主键。
    /// </summary>
    public Guid? AuditUserId { get; set; }

    /// <summary>
    /// 最近一次审核时的用户名称快照。
    /// </summary>
    public string? AuditUserNameSnapshot { get; set; }

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
    public string? ReverseUserNameSnapshot { get; set; }

    /// <summary>
    /// 最近一次反审核完成时间（UTC）。
    /// </summary>
    public DateTime? ReverseTime { get; set; }

    /// <summary>
    /// 入库单级业务备注，对全部商品明细生效。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 接收入库商品的仓库。
    /// </summary>
    public virtual Ware Ware { get; set; } = null!;

    /// <summary>
    /// 来源采购单。
    /// </summary>
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    /// <summary>
    /// 来源售后单，用于从退货入库追溯客户售后处理。
    /// </summary>
    public virtual AfterSale? AfterSale { get; set; }

    /// <summary>
    /// 供货供应商档案。
    /// </summary>
    public virtual Supplier? Supplier { get; set; }

    /// <summary>
    /// 退货客户档案。
    /// </summary>
    public virtual Customer? Customer { get; set; }

    /// <summary>
    /// 发起入库业务的部门。
    /// </summary>
    public virtual Department? Department { get; set; }

    /// <summary>
    /// 负责采购到货的采购员。
    /// </summary>
    public virtual Purchaser? Purchaser { get; set; }

    /// <summary>
    /// 入库商品明细集合；随主单删除而删除。
    /// </summary>
    public virtual ICollection<StockInDetail> Details { get; set; } = new List<StockInDetail>();
}
