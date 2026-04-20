using System.Linq.Expressions;

namespace Domain.Interfaces;

public interface IProjectableRepository<T> where T : class
{
    /// <summary>
    ///     根据ID获取实体
    /// </summary>
    Task<TEntity?> GetByIdAsync<TEntity>(Guid id, Expression<Func<T, TEntity>> selector);
    
    /// <summary>
    ///     查询所有数据并投影 
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(Expression<Func<T, TEntity>> selector);
    
    /// <summary>
    ///     分页查询并且投影
    /// </summary>
    Task<(IEnumerable<TEntity> Data, int Total)> GetPagedAsync<TEntity>(
        Expression<Func<T, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = false,
        Expression<Func<T, TEntity>>? selector = null
        );
    
}