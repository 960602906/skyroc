using Domain.Entities.Orders;

namespace Domain.Interfaces;

/// <summary>
/// 销售订单仓储接口。
/// </summary>
public interface ISaleOrderRepository : IRepository<SaleOrder>
{
    /// <summary>
    /// 在当前数据库事务内锁定并读取销售订单聚合，供销售出库审核串行校验来源数量。
    /// </summary>
    /// <param name="id">待锁定的销售订单主键。</param>
    /// <returns>包含商品明细的销售订单；不存在时返回 <c>null</c>。</returns>
    Task<SaleOrder?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 检查销售订单编号是否已被其他订单使用。
    /// </summary>
    Task<bool> ExistsOrderNoAsync(string orderNo, Guid? excludeId = null);

    /// <summary>
    /// 批量读取包含明细和审核轨迹的销售订单。
    /// </summary>
    Task<List<SaleOrder>> GetByIdsAsync(IEnumerable<Guid> ids);
}
