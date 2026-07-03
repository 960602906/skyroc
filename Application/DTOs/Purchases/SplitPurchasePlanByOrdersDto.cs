namespace Application.DTOs.Purchases;

/// <summary>
/// 按来源销售订单拆分采购计划的请求。
/// </summary>
public class SplitPurchasePlanByOrdersDto
{
    /// <summary>
    /// 待拆分采购计划主键。
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// 移入新采购计划的来源销售订单主键集合。
    /// </summary>
    public List<Guid> SaleOrderIds { get; set; } = [];

    /// <summary>
    /// 拆分后新计划备注；为空时沿用原计划备注。
    /// </summary>
    public string? Remark { get; set; }
}
