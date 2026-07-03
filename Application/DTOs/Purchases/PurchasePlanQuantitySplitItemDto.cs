namespace Application.DTOs.Purchases;

/// <summary>
/// 采购计划商品数量拆分项，指定从一条明细移入新计划的采购数量。
/// </summary>
public class PurchasePlanQuantitySplitItemDto
{
    /// <summary>
    /// 待拆分采购计划明细主键。
    /// </summary>
    public Guid DetailId { get; set; }

    /// <summary>
    /// 移入新计划的数量，按该明细采购单位计量且必须大于零。
    /// </summary>
    public decimal Quantity { get; set; }
}
