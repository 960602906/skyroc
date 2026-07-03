using Domain.Entities.Orders;

namespace Application.DTOs.Orders;

/// <summary>
/// 销售订单商品明细 DTO。
/// </summary>
public class SaleOrderDetailDto : BaseDto
{
    public Guid SaleOrderId { get; set; }

    public Guid GoodsId { get; set; }

    public string GoodsName { get; set; } = string.Empty;

    public string GoodsCode { get; set; } = string.Empty;

    public string? GoodsImage { get; set; }

    public string? GoodsTypeName { get; set; }

    public string? GoodsDescription { get; set; }

    public Guid GoodsUnitId { get; set; }

    public string GoodsUnitName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal BaseQuantity { get; set; }

    public Guid? BaseUnitId { get; set; }

    public string? BaseUnitName { get; set; }

    public decimal UnitConversion { get; set; }

    public decimal FixedPrice { get; set; }

    public Guid? FixedGoodsUnitId { get; set; }

    public string? FixedGoodsUnitName { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Remark { get; set; }

    public string? InnerRemark { get; set; }

    public OrderCustomerCheckStatus CustomerCheckStatus { get; set; }

    public decimal? CustomerCheckBaseQuantity { get; set; }

    public decimal? CustomerCheckPrice { get; set; }

    public bool HasPurchasePlan { get; set; }
}
