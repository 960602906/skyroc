using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单商品明细，保存商品、单位、价格和验收快照。
/// </summary>
public class SaleOrderDetail : BaseEntity
{
    public Guid SaleOrderId { get; set; }

    public Guid GoodsId { get; set; }

    public string GoodsNameSnapshot { get; set; } = string.Empty;

    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    public string? GoodsImageSnapshot { get; set; }

    public string? GoodsTypeNameSnapshot { get; set; }

    public string? GoodsDescriptionSnapshot { get; set; }

    public Guid GoodsUnitId { get; set; }

    public string GoodsUnitNameSnapshot { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal BaseQuantity { get; set; }

    public Guid? BaseUnitId { get; set; }

    public string? BaseUnitNameSnapshot { get; set; }

    public decimal UnitConversion { get; set; } = 1m;

    public decimal FixedPrice { get; set; }

    public Guid? FixedGoodsUnitId { get; set; }

    public string? FixedGoodsUnitNameSnapshot { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Remark { get; set; }

    public string? InnerRemark { get; set; }

    public OrderCustomerCheckStatus CustomerCheckStatus { get; set; } = OrderCustomerCheckStatus.Pending;

    public decimal? CustomerCheckBaseQuantity { get; set; }

    public decimal? CustomerCheckPrice { get; set; }

    public bool HasPurchasePlan { get; set; }

    public virtual SaleOrder SaleOrder { get; set; } = null!;

    public virtual GoodsEntity Goods { get; set; } = null!;

    public virtual GoodsUnit GoodsUnit { get; set; } = null!;

    public virtual GoodsUnit? BaseUnit { get; set; }

    public virtual GoodsUnit? FixedGoodsUnit { get; set; }
}
