using Domain.Entities.Orders;
namespace Application.DTOs.Orders;

/// <summary>
/// 销售订单 DTO。
/// </summary>
public class SaleOrderDto : BaseDto
{
    public string OrderNo { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerCode { get; set; } = string.Empty;

    public Guid? QuotationId { get; set; }

    public Guid? WareId { get; set; }

    public string? WareName { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? ReceiveDate { get; set; }

    public DateTime? OutDate { get; set; }

    public SaleOrderStatus OrderStatus { get; set; }

    public OrderReturnStatus ReturnStatus { get; set; }

    public OrderPrintStatus PrintStatus { get; set; }

    public OrderOutStorageStatus OutStorageStatus { get; set; }

    public decimal OrderPrice { get; set; }

    public decimal SettlementPrice { get; set; }

    public bool HasOutSale { get; set; }

    public bool UpdateStatus { get; set; }

    public bool HasPurchasePlan { get; set; }

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? Remark { get; set; }

    public string? InnerRemark { get; set; }

    public List<SaleOrderDetailDto> Details { get; set; } = [];

    public List<OrderAuditLogDto> AuditLogs { get; set; } = [];
}
