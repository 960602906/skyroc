namespace Application.DTOs.Orders;

/// <summary>
/// 创建销售订单 DTO。
/// </summary>
public class CreateSaleOrderDto
{
    public Guid CustomerId { get; set; }

    public Guid? QuotationId { get; set; }

    public Guid? WareId { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? ReceiveDate { get; set; }

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? Remark { get; set; }

    public string? InnerRemark { get; set; }

    public List<CreateSaleOrderDetailDto> Details { get; set; } = [];
}
