namespace Application.DTOs.Purchases;

/// <summary>
/// 按商品采购数量拆分采购计划的请求。
/// </summary>
public class SplitPurchasePlanByQuantityDto
{
    /// <summary>
    /// 待拆分采购计划主键。
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// 各商品明细移入新计划的采购数量；同一明细只能出现一次。
    /// </summary>
    public List<PurchasePlanQuantitySplitItemDto> Details { get; set; } = [];

    /// <summary>
    /// 拆分后新计划备注；为空时沿用原计划备注。
    /// </summary>
    public string? Remark { get; set; }
}
