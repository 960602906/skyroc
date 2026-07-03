using Domain.Entities.Orders;

namespace Domain.Interfaces;

/// <summary>
/// 销售订单仓储接口。
/// </summary>
public interface ISaleOrderRepository : IRepository<SaleOrder>
{
    /// <summary>
    /// 检查销售订单编号是否已被其他订单使用。
    /// </summary>
    Task<bool> ExistsOrderNoAsync(string orderNo, Guid? excludeId = null);

    /// <summary>
    /// 批量读取包含明细和审核轨迹的销售订单。
    /// </summary>
    Task<List<SaleOrder>> GetByIdsAsync(IEnumerable<Guid> ids);
}
