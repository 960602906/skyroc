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
    /// 批量只读销售订单主单及商品明细快照，用于订单打印等场景。
    /// </summary>
    /// <param name="ids">待读取的销售订单主键集合。</param>
    /// <returns>存在的销售订单聚合集合。</returns>
    Task<List<SaleOrder>> GetByIdsAsync(IEnumerable<Guid> ids);

    /// <summary>原子标记指定销售订单已完成正式打印。</summary>
    /// <param name="ids">待标记的销售订单主键集合。</param>
    /// <param name="updatedBy">确认打印的操作人主键。</param>
    /// <param name="updateName">确认打印的操作人名称快照。</param>
    /// <returns>实际标记成功的订单数量。</returns>
    Task<int> MarkPrintedAsync(IReadOnlyCollection<Guid> ids, Guid? updatedBy, string? updateName);
}
