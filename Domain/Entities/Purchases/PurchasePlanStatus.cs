namespace Domain.Entities.Purchases;

/// <summary>
/// 采购计划生成采购单的状态。
/// </summary>
public enum PurchasePlanStatus
{
    Unpublished = 1,
    PartiallyGenerated = 2,
    Generated = 3
}
