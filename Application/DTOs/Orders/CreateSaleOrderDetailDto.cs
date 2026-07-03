namespace Application.DTOs.Orders;

/// <summary>
/// 创建销售订单商品明细 DTO。
/// </summary>
public class CreateSaleOrderDetailDto
{
    public Guid GoodsId { get; set; }

    public Guid GoodsUnitId { get; set; }

    public decimal Quantity { get; set; }

    public decimal FixedPrice { get; set; }

    public Guid FixedGoodsUnitId { get; set; }

    public string? Remark { get; set; }

    public string? InnerRemark { get; set; }
}
