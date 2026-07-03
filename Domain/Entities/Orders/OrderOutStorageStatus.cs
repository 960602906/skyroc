namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单出库生成状态。
/// </summary>
public enum OrderOutStorageStatus
{
    /// <summary>
    /// 尚未生成销售出库单。
    /// </summary>
    NotGenerated = 0,
    /// <summary>
    /// 部分商品已生成销售出库单。
    /// </summary>
    PartiallyGenerated = 1,
    /// <summary>
    /// 全部商品已生成销售出库单。
    /// </summary>
    Generated = 2
}
