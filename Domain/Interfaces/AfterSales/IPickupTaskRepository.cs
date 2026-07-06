using System.Linq.Expressions;
using Domain.Entities.AfterSales;

namespace Domain.Interfaces;

/// <summary>
/// 售后取货任务仓储接口，负责来源幂等查询、履约状态锁定和完整聚合读取。
/// </summary>
public interface IPickupTaskRepository : IRepository<PickupTask>
{
    /// <summary>
    /// 在当前事务内锁定并读取取货任务，防止司机分配、状态流转和退货入库并发冲突。
    /// </summary>
    /// <param name="id">待锁定的取货任务主键。</param>
    /// <returns>包含售后单、售后商品和司机的任务；不存在时返回 <c>null</c>。</returns>
    Task<PickupTask?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 按主键集合读取取货任务完整聚合。
    /// </summary>
    /// <param name="ids">取货任务主键集合。</param>
    /// <param name="forUpdate">是否在 PostgreSQL 事务内按稳定顺序锁定任务。</param>
    /// <returns>存在的取货任务聚合。</returns>
    Task<IReadOnlyList<PickupTask>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, bool forUpdate = false);

    /// <summary>
    /// 按条件分页查询取货任务及售后来源、商品和司机快照。
    /// </summary>
    /// <param name="predicate">售后单、客户、司机、状态和计划时间筛选条件。</param>
    /// <param name="pageNumber">从 1 开始的页码。</param>
    /// <param name="pageSize">每页记录数。</param>
    /// <param name="orderBy">排序字段。</param>
    /// <param name="isDescending">是否倒序。</param>
    /// <returns>取货任务数据和总记录数。</returns>
    new Task<(IEnumerable<PickupTask> Data, int Total)> GetPagedAsync(
        Expression<Func<PickupTask, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<PickupTask, object>>? orderBy = null,
        bool isDescending = false);
}
