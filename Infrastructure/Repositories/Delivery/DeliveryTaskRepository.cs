using System.Linq.Expressions;
using Domain.Entities.Delivery;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 配送任务仓储，负责完整聚合读取、来源幂等查询、整单履约判断和事务内行锁定。
/// </summary>
public class DeliveryTaskRepository(ApplicationDbContext context)
    : Repository<DeliveryTask>(context), IDeliveryTaskRepository
{
    /// <inheritdoc />
    public override Task UpdateAsync(DeliveryTask entity)
    {
        Context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeliveryTask>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await BuildDetailQuery()
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
    }

    /// <inheritdoc />
    public override async Task<DeliveryTask?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<DeliveryTask?> GetByStockOutOrderIdAsync(Guid stockOutOrderId)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.StockOutOrderId == stockOutOrderId);
    }

    /// <inheritdoc />
    public async Task<DeliveryTask?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        var lockedTasks = DbSet.FromSqlInterpolated($"SELECT * FROM delivery_task WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(lockedTasks).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<bool> HasIncompleteDeliveriesAsync(Guid saleOrderId, Guid excludeTaskId)
    {
        return await Context.Set<StockOutOrder>().AnyAsync(outbound =>
            outbound.OrderType == StockOutOrderType.Sale
            && outbound.BusinessStatus == StockDocumentStatus.Audited
            && outbound.SaleOrderId == saleOrderId
            && !DbSet.Any(task => task.StockOutOrderId == outbound.Id
                                  && (task.Id == excludeTaskId
                                      || task.DeliveryStatus == DeliveryTaskStatus.Signed)));
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<DeliveryTask> Data, int Total)> GetPagedAsync(
        Expression<Func<DeliveryTask, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<DeliveryTask, object>>? orderBy = null,
        bool isDescending = false)
    {
        var filtered = DbSet.AsNoTracking().Where(predicate ?? (_ => true));
        var total = await filtered.CountAsync();
        var query = BuildDetailQuery().AsNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        if (orderBy is not null)
        {
            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        }

        var data = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (data, total);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsTaskNoAsync(string taskNo)
    {
        var normalized = taskNo.Trim();
        return await DbSet.AnyAsync(x => x.TaskNo == normalized);
    }

    /// <summary>
    /// 构建包含来源单据、客户、仓库、司机、承运商、路线和异常的配送任务查询。
    /// </summary>
    /// <param name="source">可选的已锁定配送任务查询。</param>
    /// <returns>完整配送任务聚合查询。</returns>
    private IQueryable<DeliveryTask> BuildDetailQuery(IQueryable<DeliveryTask>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.StockOutOrder)
                .ThenInclude(x => x.Details)
            .Include(x => x.SaleOrder)
            .Include(x => x.Customer)
            .Include(x => x.Ware)
            .Include(x => x.Driver)
            .Include(x => x.Carrier)
            .Include(x => x.Route)
            .Include(x => x.Exceptions)
            .Include(x => x.Receipt)
                .ThenInclude(x => x!.CheckDetails)
            .AsSplitQuery();
    }
}
