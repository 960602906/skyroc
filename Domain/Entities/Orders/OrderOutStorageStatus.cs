namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单出库生成状态。
/// </summary>
public enum OrderOutStorageStatus
{
    NotGenerated = 0,
    PartiallyGenerated = 1,
    Generated = 2
}
