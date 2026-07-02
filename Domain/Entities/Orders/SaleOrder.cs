using Domain.Entities.Customers;
using Domain.Entities.Pricing;
using Domain.Entities.Storage;

namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单主单，保存客户、报价和履约状态及必要历史快照。
/// </summary>
public class SaleOrder : BaseEntity
{
    public string OrderNo { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerNameSnapshot { get; set; } = string.Empty;

    public string CustomerCodeSnapshot { get; set; } = string.Empty;

    public Guid? QuotationId { get; set; }

    public Guid? WareId { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? ReceiveDate { get; set; }

    public DateTime? OutDate { get; set; }

    public SaleOrderStatus OrderStatus { get; set; } = SaleOrderStatus.PendingAudit;

    public OrderReturnStatus ReturnStatus { get; set; } = OrderReturnStatus.NotReturned;

    public OrderPrintStatus PrintStatus { get; set; } = OrderPrintStatus.NotPrinted;

    public OrderOutStorageStatus OutStorageStatus { get; set; } = OrderOutStorageStatus.NotGenerated;

    public decimal OrderPrice { get; set; }

    public decimal SettlementPrice { get; set; }

    public bool HasOutSale { get; set; }

    public bool UpdateStatus { get; set; }

    public bool HasPurchasePlan { get; set; }

    public string? ContactNameSnapshot { get; set; }

    public string? ContactPhoneSnapshot { get; set; }

    public string? DeliveryAddressSnapshot { get; set; }

    public string? Remark { get; set; }

    public string? InnerRemark { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Quotation? Quotation { get; set; }

    public virtual Ware? Ware { get; set; }

    public virtual ICollection<SaleOrderDetail> Details { get; set; } = new List<SaleOrderDetail>();

    public virtual ICollection<OrderAuditLog> AuditLogs { get; set; } = new List<OrderAuditLog>();
}
