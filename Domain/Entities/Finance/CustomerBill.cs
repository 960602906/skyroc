using Domain.Entities.Customers;
using Domain.Entities.Orders;

namespace Domain.Entities.Finance;

/// <summary>
/// 客户账单主表，按销售订单汇总签收应收、售后调整和后续结款余额。
/// </summary>
public class CustomerBill : BaseEntity
{
    /// <summary>
    /// 客户账单业务唯一编号。
    /// </summary>
    public string BillNo { get; set; } = string.Empty;

    /// <summary>
    /// 账单所属客户主键。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 账单生成时的客户名称快照。
    /// </summary>
    public string CustomerNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 来源销售订单主键；当前账单模型以订单为应收聚合边界。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 来源销售订单业务编号快照。
    /// </summary>
    public string SaleOrderNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 账单生成或最近一次同步的业务日期（UTC）。
    /// </summary>
    public DateTime BillDate { get; set; }

    /// <summary>
    /// 订单签收形成的正向应收金额，按系统业务币种计量。
    /// </summary>
    public decimal OrderAmount { get; set; }

    /// <summary>
    /// 售后完成形成的应收调整金额，负数表示冲减客户应收。
    /// </summary>
    public decimal AfterSaleAdjustmentAmount { get; set; }

    /// <summary>
    /// 当前账单应收金额，等于订单应收与售后调整合计后按金额精度取整。
    /// </summary>
    public decimal ReceivableAmount { get; set; }

    /// <summary>
    /// 已结款金额，后续客户结款流程按此字段回写。
    /// </summary>
    public decimal SettledAmount { get; set; }

    /// <summary>
    /// 客户账单结款状态，初始为待结款。
    /// </summary>
    public CustomerBillStatus BillStatus { get; set; } = CustomerBillStatus.Pending;

    /// <summary>
    /// 账单备注，记录人工调整说明或同步异常说明。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 账单所属客户档案。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// 账单来源销售订单。
    /// </summary>
    public virtual SaleOrder SaleOrder { get; set; } = null!;

    /// <summary>
    /// 账单明细集合，包含订单验收行和售后调整行。
    /// </summary>
    public virtual ICollection<CustomerBillDetail> Details { get; set; } = new List<CustomerBillDetail>();
}
