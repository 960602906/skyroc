using Domain.Entities.Customers;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;

namespace Domain.Entities.Storage;

/// <summary>
/// 出库主单，保存出库来源、仓库、业务方、审核状态和金额数量汇总快照。
/// </summary>
public class StockOutOrder : BaseEntity
{
    /// <summary>
    /// 出库单业务编号，在全部出库类型中保持唯一。
    /// </summary>
    public string OutNo { get; set; } = string.Empty;

    /// <summary>
    /// 出库业务类型：销售、采购退货或其他。
    /// </summary>
    public StockOutOrderType OrderType { get; set; }

    /// <summary>
    /// 单据业务状态，只有审核通过后才扣减库存。
    /// </summary>
    public StockDocumentStatus BusinessStatus { get; set; } = StockDocumentStatus.Draft;

    /// <summary>
    /// 发出商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 单据创建时的仓库名称快照。
    /// </summary>
    public string WareNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 来源销售订单主键；销售出库时填写。
    /// </summary>
    public Guid? SaleOrderId { get; set; }

    /// <summary>
    /// 收货客户主键；销售出库时通常填写。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 出库业务发生时的客户名称快照。
    /// </summary>
    public string? CustomerNameSnapshot { get; set; }

    /// <summary>
    /// 退货供应商主键；采购退货出库时填写。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 出库业务发生时的供应商名称快照。
    /// </summary>
    public string? SupplierNameSnapshot { get; set; }

    /// <summary>
    /// 发起出库业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 出库业务发生时的部门名称快照。
    /// </summary>
    public string? DepartmentNameSnapshot { get; set; }

    /// <summary>
    /// 计划或实际出库时间（UTC）。
    /// </summary>
    public DateTime OutTime { get; set; }

    /// <summary>
    /// 出库基础单位数量合计，按各商品基础单位分别求和，仅用于单据展示。
    /// </summary>
    public decimal TotalBaseQuantity { get; set; }

    /// <summary>
    /// 出库金额合计，按系统业务币种计量。
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
    /// 出库单级业务备注，对全部商品明细生效。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 发出商品的仓库。
    /// </summary>
    public virtual Ware Ware { get; set; } = null!;

    /// <summary>
    /// 来源销售订单。
    /// </summary>
    public virtual SaleOrder? SaleOrder { get; set; }

    /// <summary>
    /// 收货客户档案。
    /// </summary>
    public virtual Customer? Customer { get; set; }

    /// <summary>
    /// 退货供应商档案。
    /// </summary>
    public virtual Supplier? Supplier { get; set; }

    /// <summary>
    /// 发起出库业务的部门。
    /// </summary>
    public virtual Department? Department { get; set; }

    /// <summary>
    /// 出库商品明细集合；随主单删除而删除。
    /// </summary>
    public virtual ICollection<StockOutDetail> Details { get; set; } = new List<StockOutDetail>();
}
