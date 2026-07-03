namespace Application.DTOs.Purchases;

/// <summary>
/// 合并采购计划的请求，将兼容的未发布计划汇总为一张新计划。
/// </summary>
public class MergePurchasePlansDto
{
    /// <summary>
    /// 待合并采购计划主键集合；至少需要两张不同计划。
    /// </summary>
    public List<Guid> PlanIds { get; set; } = [];

    /// <summary>
    /// 合并后计划备注；为空时不保留来源计划备注。
    /// </summary>
    public string? Remark { get; set; }
}
