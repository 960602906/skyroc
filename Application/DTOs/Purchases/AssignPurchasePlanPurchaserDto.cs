namespace Application.DTOs.Purchases;

/// <summary>
/// 批量分配采购计划采购员的请求；采购员为空时表示清除尚未发布计划的负责人。
/// </summary>
public class AssignPurchasePlanPurchaserDto
{
    /// <summary>
    /// 待分配的采购计划主键集合；重复主键会被合并处理。
    /// </summary>
    public List<Guid> PlanIds { get; set; } = [];

    /// <summary>
    /// 目标采购员主键；为空时清除采购员及其名称快照。
    /// </summary>
    public Guid? PurchaserId { get; set; }
}
