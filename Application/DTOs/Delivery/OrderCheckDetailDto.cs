using Domain.Entities.Orders;

namespace Application.DTOs.Delivery;

/// <summary>
/// 订单商品验收响应，返回出库商品快照、配送数量、客户确认数量和结算金额。
/// </summary>
public class OrderCheckDetailDto : BaseDto
{
    /// <summary>
    /// 来源销售订单商品明细主键。
    /// </summary>
    public Guid SaleOrderDetailId { get; set; }

    /// <summary>
    /// 来源销售出库商品明细主键。
    /// </summary>
    public Guid StockOutDetailId { get; set; }

    /// <summary>
    /// 验收商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 验收时的商品名称快照。
    /// </summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 验收时的商品编码快照。
    /// </summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>
    /// 本次出库使用的计量单位主键。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 本次出库使用的计量单位名称快照。
    /// </summary>
    public string GoodsUnitName { get; set; } = string.Empty;

    /// <summary>
    /// 本次实际配送数量，按商品基础单位计量。
    /// </summary>
    public decimal DeliveredBaseQuantity { get; set; }

    /// <summary>
    /// 客户实际确认数量，按商品基础单位计量。
    /// </summary>
    public decimal AcceptedBaseQuantity { get; set; }

    /// <summary>
    /// 客户验收结论。
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
}
