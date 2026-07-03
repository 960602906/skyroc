using Domain.Entities.Orders;

namespace Domain.Interfaces;

/// <summary>
/// 订单审核记录仓储接口。
/// </summary>
public interface IOrderAuditLogRepository : IRepository<OrderAuditLog>
{
    Task<List<OrderAuditLog>> GetBySaleOrderIdAsync(Guid saleOrderId);
}
