using System.Linq.Expressions;
using Domain.Entities.Purchases;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 采购单仓储，读取时聚合供应商、采购员、商品明细及采购计划来源。
/// </summary>
public class PurchaseOrderRepository(ApplicationDbContext context)
    : Repository<PurchaseOrder>(context), IPurchaseOrderRepository
{
    /// <inheritdoc />
    public override async Task<PurchaseOrder?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery(includePlanDetails: true).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrder?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByIdAsync(id);
        }

        var lockedOrders = DbSet.FromSqlInterpolated(
            $"SELECT * FROM purchase_order WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(includePlanDetails: true, lockedOrders)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<PurchaseOrder> Data, int Total)> GetPagedAsync(
        Expression<Func<PurchaseOrder, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<PurchaseOrder, object>>? orderBy = null,
        bool isDescending = false)
    {
        var filteredQuery = DbSet.AsNoTracking().Where(predicate ?? (_ => true));
        var total = await filteredQuery.CountAsync();
        var query = BuildDetailQuery(includePlanDetails: false).AsNoTracking();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (orderBy != null)
        {
            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        }

        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (data, total);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsPurchaseNoAsync(string purchaseNo, Guid? excludeId = null)
    {
        var normalizedPurchaseNo = purchaseNo.Trim();
        return await DbSet.AnyAsync(x =>
            x.PurchaseNo == normalizedPurchaseNo
            && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PurchaseOrder>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        var distinctIds = ids.Where(x => x != Guid.Empty).Distinct().ToArray();
        return distinctIds.Length == 0
            ? []
            : await DbSet
                .AsNoTracking()
                .Where(x => distinctIds.Contains(x.Id))
                .Include(x => x.Details)
                .AsSplitQuery()
                .ToListAsync();
    }

    /// <summary>
    /// 构建包含商品、单位和计划来源聚合的采购单查询。
    /// </summary>
    /// <returns>预加载采购单完整业务聚合的查询。</returns>
    private IQueryable<PurchaseOrder> BuildDetailQuery(
        bool includePlanDetails,
        IQueryable<PurchaseOrder>? source = null)
    {
        var query = (source ?? DbSet)
            .Include(x => x.Supplier)
            .Include(x => x.Purchaser)
            .Include(x => x.Details)
                .ThenInclude(x => x.Goods)
            .Include(x => x.Details)
                .ThenInclude(x => x.PurchaseUnit)
            .Include(x => x.Details)
                .ThenInclude(x => x.PlanRelations)
                    .ThenInclude(x => x.PurchasePlanDetail)
                        .ThenInclude(x => x.PurchasePlan)
            .AsSplitQuery();

        if (includePlanDetails)
        {
            query = query
                .Include(x => x.Details)
                    .ThenInclude(x => x.PlanRelations)
                        .ThenInclude(x => x.PurchasePlanDetail)
                            .ThenInclude(x => x.PurchasePlan)
                                .ThenInclude(x => x.Details);
        }

        return query;
    }
}
