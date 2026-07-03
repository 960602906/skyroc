using Domain.Entities.Orders;

namespace Domain.Interfaces;

/// <summary>
/// 订单审核记录仓储接口。
/// </summary>
public interface IOrderAuditLogRepository : IRepository<OrderAuditLog>
{
    /// <summary>
    /// 按销售订单读取按时间排列的审核轨迹。
    /// </summary>
    Task<List<OrderAuditLog>> GetBySaleOrderIdAsync(Guid saleOrderId);
}
