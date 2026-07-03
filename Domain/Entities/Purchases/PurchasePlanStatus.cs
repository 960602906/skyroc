namespace Domain.Entities.Purchases;

/// <summary>
/// 采购计划生成采购单的状态。
/// </summary>
public enum PurchasePlanStatus
{
    /// <summary>
    /// 尚未生成采购单。
    /// </summary>
    Unpublished = 1,
    /// <summary>
    /// 部分计划数量已生成采购单。
    /// </summary>
    PartiallyGenerated = 2,
    /// <summary>
    /// 全部计划数量已生成采购单。
    /// </summary>
    Generated = 3
}
