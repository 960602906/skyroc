using Domain.Entities.Delivery;

namespace Domain.Interfaces;

/// <summary>
///     司机仓储接口，可按主键预加载所属承运商。
/// </summary>
public interface IDriverRepository : INamedCodeRepository<Driver>
{
    /// <summary>
    /// 判断司机是否已被任一配送任务引用；被引用司机只能停用，不允许删除。
    /// </summary>
    /// <param name="id">司机主键。</param>
    /// <returns>存在配送任务引用时返回 <c>true</c>。</returns>
    Task<bool> HasDeliveryTasksAsync(Guid id);

    /// <summary>
    /// 在当前事务内锁定司机并加载所属承运商，保证任务分配期间启用状态和承运关系稳定。
    /// </summary>
    /// <param name="id">待锁定司机主键。</param>
    /// <returns>司机及所属承运商；不存在时返回 <c>null</c>。</returns>
    Task<Driver?> GetByIdForUpdateAsync(Guid id);
}
