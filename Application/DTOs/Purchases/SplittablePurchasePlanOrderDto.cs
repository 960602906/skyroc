namespace Application.DTOs.Purchases;

/// <summary>
/// 采购计划中可按订单拆分的来源订单摘要。
/// </summary>
public class SplittablePurchasePlanOrderDto
{
    /// <summary>
    /// 来源销售订单主键。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 来源销售订单业务编号。
    /// </summary>
    public string SaleOrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 该订单在当前采购计划中的需求数量合计；不同采购单位的数量仅用于拆分范围提示。
    /// </summary>
    public decimal RequiredQuantity { get; set; }
}
