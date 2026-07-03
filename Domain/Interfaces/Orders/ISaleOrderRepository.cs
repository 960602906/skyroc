using Domain.Entities.Orders;

namespace Domain.Interfaces;

/// <summary>
/// 销售订单仓储接口。
/// </summary>
public interface ISaleOrderRepository : IRepository<SaleOrder>
{
    Task<bool> ExistsOrderNoAsync(string orderNo, Guid? excludeId = null);

    Task<List<SaleOrder>> GetByIdsAsync(IEnumerable<Guid> ids);
}
