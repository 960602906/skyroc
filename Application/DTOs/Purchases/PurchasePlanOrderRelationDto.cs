namespace Application.DTOs.Purchases;

/// <summary>
/// 采购计划明细与来源销售订单明细的关联 DTO。
/// </summary>
public class PurchasePlanOrderRelationDto : BaseDto
{
    /// <summary>
    /// 采购计划商品明细主键。
    /// </summary>
    public Guid PurchasePlanDetailId { get; set; }

    /// <summary>
    /// 来源销售订单主键。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 来源销售订单编号快照。
    /// </summary>
    public string? SaleOrderNo { get; set; }

    /// <summary>
    /// 来源销售订单商品明细主键。
    /// </summary>
    public Guid SaleOrderDetailId { get; set; }

    /// <summary>
    /// 来源订单需求数量，按采购单位计量。
    /// </summary>
    public decimal RequiredQuantity { get; set; }
}
