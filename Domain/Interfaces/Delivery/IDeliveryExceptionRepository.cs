using System.Linq.Expressions;
using Domain.Entities.Delivery;

namespace Domain.Interfaces;

/// <summary>
///     配送异常仓储接口。
/// </summary>
public interface IDeliveryExceptionRepository : IRepository<DeliveryException>
{
    /// <summary>
    /// 在当前事务内锁定配送异常，防止重复处理。
    /// </summary>
    /// <param name="id">配送异常主键。</param>
    /// <returns>配送异常聚合；不存在时返回 <c>null</c>。</returns>
    Task<DeliveryException?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 判断配送任务是否仍有其他待处理异常。
    /// </summary>
    /// <param name="deliveryTaskId">配送任务主键。</param>
    /// <param name="excludeExceptionId">本次正在处理、需从查询中排除的异常主键。</param>
    /// <returns>存在其他待处理异常时返回 <c>true</c>。</returns>
    Task<bool> HasPendingExceptionsAsync(Guid deliveryTaskId, Guid excludeExceptionId);

    /// <summary>
    /// 按条件分页查询配送异常，加载所属任务、司机和客户信息。
    /// </summary>
    /// <param name="predicate">任务、司机、客户、处理状态和时间筛选条件。</param>
    /// <param name="pageNumber">从 1 开始的页码。</param>
    /// <param name="pageSize">每页记录数。</param>
    /// <param name="orderBy">排序字段。</param>
    /// <param name="isDescending">是否倒序。</param>
    /// <returns>配送异常数据和总记录数。</returns>
    new Task<(IEnumerable<DeliveryException> Data, int Total)> GetPagedAsync(
        Expression<Func<DeliveryException, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<DeliveryException, object>>? orderBy = null,
        bool isDescending = false);

    /// <summary>
    ///     按异常编号判断是否存在，可排除指定记录。
    /// </summary>
    /// <param name="exceptionNo">配送异常业务编号。</param>
    /// <param name="excludeId">需要排除的记录主键，通常为当前更新记录。</param>
    /// <returns>存在返回 true，否则返回 false。</returns>
    Task<bool> ExistsByExceptionNoAsync(string exceptionNo, Guid? excludeId = null);

    /// <summary>
    /// 根据配送异常号查询配送异常详情（含所属任务、司机和客户信息）。
    /// </summary>
    /// <param name="exceptionNo">配送异常号。</param>
    /// <returns>配送异常聚合；不存在时返回 <c>null</c>。</returns>
    Task<DeliveryException?> GetByExceptionNoAsync(string exceptionNo);
}
