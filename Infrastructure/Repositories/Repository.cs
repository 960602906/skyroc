using System.Linq.Expressions;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class Repository<T>(ApplicationDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public  async Task<bool> ExistsAsync(Guid id)
    {
        return await DbSet.AnyAsync(e => e.Id == id);
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    public async Task<T?> GetByConditionAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.Where(predicate).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public Task<T?> FirstFindAsync(Expression<Func<T, bool>> predicate)
    {
        return DbSet.Where(predicate).FirstOrDefaultAsync();
    }

    public async Task<(IEnumerable<T> Data, int Total)> GetPagedAsync(
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

    public async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await DbSet.AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities)
    {
        DbSet.UpdateRange(entities);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity is not null)
        {
            DbSet.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
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