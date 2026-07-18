using System.Linq.Expressions;
using Domain.Entities.AfterSales;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 售后取货任务仓储，提供售后来源、商品、司机和退货入库明细的聚合读取及行锁。
/// </summary>
public class PickupTaskRepository(ApplicationDbContext context)
    : Repository<PickupTask>(context), IPickupTaskRepository
{
    /// <inheritdoc />
    public override Task UpdateAsync(PickupTask entity)
    {
        Context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<PickupTask?> GetByIdAsync(Guid id)
    {
        return BuildDetailQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<PickupTask?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        var lockedTasks = DbSet.FromSqlInterpolated($"SELECT * FROM pickup_task WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(lockedTasks).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PickupTask>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        bool forUpdate = false)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var orderedIds = ids.Distinct().OrderBy(x => x).ToArray();
        if (!forUpdate || !Context.Database.IsNpgsql())
        {
            var query = BuildDetailQuery().Where(x => orderedIds.Contains(x.Id));
            if (!forUpdate)
            {
                query = query.AsNoTracking();
            }

            return await query.OrderBy(x => x.Id).ToListAsync();
        }

        var lockedTasks = DbSet.FromSqlInterpolated(
            $"SELECT * FROM pickup_task WHERE id = ANY({orderedIds}) ORDER BY id FOR UPDATE");
        return await BuildDetailQuery(lockedTasks).OrderBy(x => x.Id).ToListAsync();
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<PickupTask> Data, int Total)> GetPagedAsync(
        Expression<Func<PickupTask, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<PickupTask, object>>? orderBy = null,
        bool isDescending = false)
    {
        return await PagedFromQueryAsync(
            BuildDetailQuery().AsNoTracking(),
            predicate,
            pageNumber,
            pageSize,
            orderBy,
            isDescending);
    }

    /// <summary>
    /// 构建包含售后单、售后商品、司机和来源退货入库明细的取货任务查询。
    /// </summary>
    /// <param name="source">可选的已锁定取货任务查询。</param>
    /// <returns>完整取货任务聚合查询。</returns>
    private IQueryable<PickupTask> BuildDetailQuery(IQueryable<PickupTask>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.AfterSale)
            .Include(x => x.AfterSaleGoods)
            .Include(x => x.Driver)
            .Include(x => x.StockInDetail)
            .AsSplitQuery();
    }
}
