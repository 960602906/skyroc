using Domain.Entities.Customers;
using Domain.Entities.Pricing;
using Domain.Entities.Storage;

namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单主单，保存客户、报价和履约状态及必要历史快照。
/// </summary>
public class SaleOrder : BaseEntity
{
    /// <summary>
    /// 销售订单业务编号。
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 关联客户主键。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 业务发生时的客户名称快照。
    /// </summary>
    public string CustomerNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 下单时的客户编码快照。
    /// </summary>
    public string CustomerCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 销售报价单主键。
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    /// 关联仓库主键。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    /// 客户下单时间（UTC）。
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// 客户要求收货时间（UTC）。
    /// </summary>
    public DateTime? ReceiveDate { get; set; }

    /// <summary>
    /// 计划或实际出库时间（UTC）。
    /// </summary>
    public DateTime? OutDate { get; set; }

    /// <summary>
    /// 销售订单当前业务状态。
    /// </summary>
    public SaleOrderStatus OrderStatus { get; set; } = SaleOrderStatus.PendingAudit;

    /// <summary>
    /// 订单回单状态。
    /// </summary>
    public OrderReturnStatus ReturnStatus { get; set; } = OrderReturnStatus.NotReturned;

    /// <summary>
    /// 订单打印状态。
    /// </summary>
    public OrderPrintStatus PrintStatus { get; set; } = OrderPrintStatus.NotPrinted;

    /// <summary>
    /// 销售出库单生成状态。
    /// </summary>
    public OrderOutStorageStatus OutStorageStatus { get; set; } = OrderOutStorageStatus.NotGenerated;

    /// <summary>
    /// 订单销售总金额。
    /// </summary>
    public decimal OrderPrice { get; set; }

    /// <summary>
    /// 订单最终结算金额。
    /// </summary>
    public decimal SettlementPrice { get; set; }

    /// <summary>
    /// 是否已生成销售出库单。
    /// </summary>
    public bool HasOutSale { get; set; }

    /// <summary>
    /// 订单审核通过后是否发生过修改。
    /// </summary>
    public bool UpdateStatus { get; set; }

    /// <summary>
    /// 是否已生成采购计划。
    /// </summary>
    public bool HasPurchasePlan { get; set; }

    /// <summary>
    /// 业务发生时的联系人姓名快照。
    /// </summary>
    public string? ContactNameSnapshot { get; set; }

    /// <summary>
    /// 业务发生时的联系人电话快照。
    /// </summary>
    public string? ContactPhoneSnapshot { get; set; }

    /// <summary>
    /// 下单时的配送地址快照。
    /// </summary>
    public string? DeliveryAddressSnapshot { get; set; }

    /// <summary>
    /// 业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 仅内部人员可见的备注。
    /// </summary>
    public string? InnerRemark { get; set; }

    /// <summary>
    /// 下单客户导航属性。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// 订单采用的报价单导航属性。
    /// </summary>
    public virtual Quotation? Quotation { get; set; }

    /// <summary>
    /// 订单履约仓库导航属性。
    /// </summary>
    public virtual Ware? Ware { get; set; }

    /// <summary>
    /// 订单商品明细集合。
    /// </summary>
    public virtual ICollection<SaleOrderDetail> Details { get; set; } = new List<SaleOrderDetail>();

    /// <summary>
    /// 订单审核轨迹集合。
    /// </summary>
    public virtual ICollection<OrderAuditLog> AuditLogs { get; set; } = new List<OrderAuditLog>();
}
