namespace Application.DTOs.Purchases;

/// <summary>
/// 批量分配采购计划供应商的请求；供应商为空时表示清除尚未发布计划的供应商。
/// </summary>
public class AssignPurchasePlanSupplierDto
{
    /// <summary>
    /// 待分配的采购计划主键集合；重复主键会被合并处理。
    /// </summary>
    public List<Guid> PlanIds { get; set; } = [];

    /// <summary>
    /// 目标供应商主键；为空时清除供应商及其名称快照。
    /// </summary>
    public Guid? SupplierId { get; set; }
}
