using Domain.Entities.Goods;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Orders;

/// <summary>
/// 订单商品验收明细，按销售出库行固化交付数量、客户确认数量和结算金额。
/// </summary>
public class OrderCheckDetail : BaseEntity
{
    /// <summary>
    /// 所属签收回单主键。
    /// </summary>
    public Guid OrderReceiptId { get; set; }

    /// <summary>
    /// 来源销售订单商品明细主键。
    /// </summary>
    public Guid SaleOrderDetailId { get; set; }

    /// <summary>
    /// 本次配送对应的销售出库商品明细主键，同一回单内不得重复。
    /// </summary>
    public Guid StockOutDetailId { get; set; }

    /// <summary>
    /// 验收商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 验收时的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 验收时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 本次出库使用的计量单位主键。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 本次出库使用的计量单位名称快照。
    /// </summary>
    public string GoodsUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 本次实际配送数量，按商品基础单位计量。
    /// </summary>
    public decimal DeliveredBaseQuantity { get; set; }

    /// <summary>
    /// 客户实际确认数量，按商品基础单位计量，不能超过本次配送数量。
    /// </summary>
    public decimal AcceptedBaseQuantity { get; set; }

    /// <summary>
    /// 客户对当前出库商品行的验收结论，只允许通过或拒绝。
    /// </summary>
    public OrderCustomerCheckStatus CheckStatus { get; set; }

    /// <summary>
    /// 按客户确认数量和出库价格计算的验收金额，使用系统业务币种。
    /// </summary>
    public decimal AcceptedAmount { get; set; }

    /// <summary>
    /// 当前商品行的验收差异或拒收原因。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属签收回单。
    /// </summary>
    public virtual OrderReceipt OrderReceipt { get; set; } = null!;

    /// <summary>
    /// 来源销售订单商品明细。
    /// </summary>
    public virtual SaleOrderDetail SaleOrderDetail { get; set; } = null!;

    /// <summary>
    /// 来源销售出库商品明细。
    /// </summary>
    public virtual StockOutDetail StockOutDetail { get; set; } = null!;

    /// <summary>
    /// 验收商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 本次出库使用的商品单位。
    /// </summary>
    public virtual GoodsUnit GoodsUnit { get; set; } = null!;
}
