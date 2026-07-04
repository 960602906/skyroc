using System.Linq.Expressions;
using Domain.Entities.Delivery;

namespace Domain.Interfaces;

/// <summary>
/// 配送任务仓储接口，负责按销售出库来源、司机、路线和履约状态读取并锁定任务聚合。
/// </summary>
public interface IDeliveryTaskRepository : IRepository<DeliveryTask>
{
    /// <summary>
    /// 批量读取配送任务完整聚合，供批量更新后一次性构造响应。
    /// </summary>
    /// <param name="ids">配送任务主键集合。</param>
    /// <returns>存在的配送任务聚合。</returns>
    Task<IReadOnlyList<DeliveryTask>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    /// <summary>
    /// 按来源销售出库单查询配送任务，用于保证生成操作幂等。
    /// </summary>
    /// <param name="stockOutOrderId">来源销售出库单主键。</param>
    /// <returns>已生成的配送任务；不存在时返回 <c>null</c>。</returns>
    Task<DeliveryTask?> GetByStockOutOrderIdAsync(Guid stockOutOrderId);

    /// <summary>
    /// 在当前事务内锁定配送任务并加载司机、承运商、路线和来源单据。
    /// </summary>
    /// <param name="id">待锁定的配送任务主键。</param>
    /// <returns>配送任务聚合；不存在时返回 <c>null</c>。</returns>
    Task<DeliveryTask?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 按条件分页查询配送任务完整聚合。
    /// </summary>
    /// <param name="predicate">客户、司机、承运商、路线、状态和时间筛选条件。</param>
    /// <param name="pageNumber">从 1 开始的页码。</param>
    /// <param name="pageSize">每页记录数。</param>
    /// <param name="orderBy">排序字段。</param>
    /// <param name="isDescending">是否倒序。</param>
    /// <returns>配送任务数据和总记录数。</returns>
    new Task<(IEnumerable<DeliveryTask> Data, int Total)> GetPagedAsync(
        Expression<Func<DeliveryTask, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<DeliveryTask, object>>? orderBy = null,
        bool isDescending = false);

    /// <summary>
    /// 检查配送任务编号是否已被占用。
    /// </summary>
    /// <param name="taskNo">配送任务业务编号。</param>
    /// <returns>存在同号任务时返回 <c>true</c>。</returns>
    Task<bool> ExistsTaskNoAsync(string taskNo);
}
