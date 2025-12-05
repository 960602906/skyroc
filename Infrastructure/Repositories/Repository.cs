using System.Linq.Expressions;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class Repository<T>(ApplicationDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly DbSet<T> DbSet = context.Set<T>();

    /// <summary>
    ///     根据id验证是否存在
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await DbSet.AnyAsync(e => e.Id == id);
    }

    /// <summary>
    ///     根据ID获取实体
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    /// <summary>
    ///     根据条件获取单个实体
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public virtual async Task<T?> GetByConditionAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.Where(predicate).FirstOrDefaultAsync();
    }

    /// <summary>
    ///     获取所有实体
    /// </summary>
    /// <returns></returns>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    /// <summary>
    ///     条件查询
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    /// <summary>
    ///     条件查询第一个实体
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public virtual Task<T?> FirstFindAsync(Expression<Func<T, bool>> predicate)
    {
        return DbSet.Where(predicate).FirstOrDefaultAsync();
    }

    /// <summary>
    ///     分页查询
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="orderBy"></param>
    /// <param name="isDescending"></param>
    /// <returns></returns>
    public virtual async Task<(IEnumerable<T> Data, int Total)> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = false)
    {
        var query = DbSet.AsNoTracking();
        // 不需要手动过滤 IsDeleted，全局过滤器已处理 ✅
        if (predicate != null)
            query = query.Where(predicate);
        var total = await query.CountAsync();
        if (orderBy != null) query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (data, total);
    }

    /// <summary>
    ///     添加实体
    /// </summary>
    /// <param name="entity"></param>
    public virtual async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    /// <summary>
    ///     批量添加实体
    /// </summary>
    /// <param name="entities"></param>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await DbSet.AddRangeAsync(entities);
    }

    public virtual Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        DbSet.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity is not null)
        {
            DbSet.Remove(entity);
        }
    }

    public virtual Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<Guid> guids)
    {
        var entities = await DbSet.Where(e => guids.Contains(e.Id)).ToListAsync();
        await DeleteRangeAsync(entities);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        var result = await DbSet.Where(predicate).AnyAsync();
        return result;
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var count = await DbSet.CountAsync(predicate ?? (_ => true));
        return count;
    }
}