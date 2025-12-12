using System.Linq.Expressions;

namespace Domain.Interfaces;

/// <summary>
///     泛型仓储接口 - Domain 层定义
///     实现在 Infrastructure 层
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IRepository<TEntity>
{
    /// <summary>
    ///     根据id验证是否存在
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    ///     根据ID获取实体
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id);

    /// <summary>
    ///     根据条件获取单个实体
    /// </summary>
    Task<TEntity?> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     获取所有实体 
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync();
    
    /// <summary>
    ///     条件查询
    /// </summary>
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     条件查询第一个实体
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<TEntity?> FirstFindAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     分页查询
    /// </summary>
    Task<(IEnumerable<TEntity> Data, int Total)> GetPagedAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false);

    /// <summary>
    ///     添加实体
    /// </summary>
    Task AddAsync(TEntity entity);

    /// <summary>
    ///     批量添加实体
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    ///     更新实体
    /// </summary>
    Task UpdateAsync(TEntity entity);

    /// <summary>
    ///     批量更新实体
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    ///     删除实体
    /// </summary>
    Task DeleteAsync(TEntity entity);

    /// <summary>
    ///     根据ID删除实体
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    ///     批量删除实体
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    ///     批量删除实体
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<Guid> guids);

    /// <summary>
    ///     检查实体是否存在
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    ///     获取满足条件的记录数
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
}