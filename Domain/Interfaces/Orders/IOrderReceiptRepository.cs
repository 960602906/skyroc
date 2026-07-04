using Domain.Entities.Orders;

namespace Domain.Interfaces;

/// <summary>
/// 订单签收回单仓储接口，负责回单聚合读取、编号幂等检查和整单验收状态汇总查询。
/// </summary>
public interface IOrderReceiptRepository : IRepository<OrderReceipt>
{
    /// <summary>
    /// 按配送任务读取签收回单及商品验收明细。
    /// </summary>
    /// <param name="deliveryTaskId">配送任务主键。</param>
    /// <returns>对应签收回单；任务尚未签收时返回 <c>null</c>。</returns>
    Task<OrderReceipt?> GetByDeliveryTaskIdAsync(Guid deliveryTaskId);

    /// <summary>
    /// 读取销售订单已有的全部商品验收明细，供整单完成时汇总客户确认结果。
    /// </summary>
    /// <param name="saleOrderId">销售订单主键。</param>
    /// <returns>已保存的商品验收明细。</returns>
    Task<IReadOnlyList<OrderCheckDetail>> GetCheckDetailsBySaleOrderAsync(Guid saleOrderId);

    /// <summary>
    /// 判断销售订单是否仍有其他尚未归档的签收回单。
    /// </summary>
    /// <param name="saleOrderId">销售订单主键。</param>
    /// <param name="excludeReceiptId">本次正在归档、需从查询中排除的回单主键。</param>
    /// <returns>存在其他未回单记录时返回 <c>true</c>。</returns>
    Task<bool> HasUnreturnedReceiptsAsync(Guid saleOrderId, Guid excludeReceiptId);

    /// <summary>
    /// 检查签收回单编号是否已经存在。
    /// </summary>
    /// <param name="receiptNo">签收回单业务编号。</param>
    /// <returns>存在同号记录时返回 <c>true</c>。</returns>
    Task<bool> ExistsReceiptNoAsync(string receiptNo);
}
