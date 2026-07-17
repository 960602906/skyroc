using System.Linq.Expressions;
using Domain.Entities.AfterSales;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 售后单聚合仓储实现。
/// </summary>
public class AfterSaleRepository(ApplicationDbContext context)
    : Repository<AfterSale>(context), IAfterSaleRepository
{
    /// <inheritdoc />
    public override Task<AfterSale?> GetByIdAsync(Guid id)
    {
        return BuildDetailQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<AfterSale?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        var lockedAfterSales = DbSet.FromSqlInterpolated(
            $"SELECT * FROM after_sale WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(lockedAfterSales).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<AfterSale> Data, int Total)> GetPagedAsync(
        Expression<Func<AfterSale, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<AfterSale, object>>? orderBy = null,
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

    /// <inheritdoc />
    public Task<bool> ExistsAfterSaleNoAsync(string afterSaleNo)
    {
        var normalizedNo = afterSaleNo.Trim();
        return DbSet.AnyAsync(x => x.AfterSaleNo == normalizedNo);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetReservedBaseQuantitiesAsync(
        IEnumerable<Guid> saleOrderDetailIds,
        Guid? excludeAfterSaleId = null)
    {
        var idList = saleOrderDetailIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var totals = await Context.Set<AfterSaleGoods>()
            .Where(x => x.SaleOrderDetailId.HasValue
                        && idList.Contains(x.SaleOrderDetailId.Value)
                        && (!excludeAfterSaleId.HasValue || x.AfterSaleId != excludeAfterSaleId.Value))
            .GroupBy(x => x.SaleOrderDetailId!.Value)
            .Select(group => new { SaleOrderDetailId = group.Key, Quantity = group.Sum(x => x.BaseRefundQuantity) })
            .ToListAsync();
        return totals.ToDictionary(x => x.SaleOrderDetailId, x => x.Quantity);
    }

    /// <inheritdoc />
    public Task<List<AfterSale>> GetCompletedBySaleOrderIdAsync(Guid saleOrderId)
    {
        return BuildDetailQuery()
            .AsNoTracking()
            .Where(x => x.SaleOrderId == saleOrderId && x.AfterStatus == AfterSaleStatus.Completed)
            .ToListAsync();
    }

    private IQueryable<AfterSale> BuildDetailQuery(IQueryable<AfterSale>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.SaleOrder)
                .ThenInclude(x => x!.Details)
            .Include(x => x.Goods)
            .Include(x => x.AuditLogs)
            .Include(x => x.PickupTasks)
                .ThenInclude(x => x.StockInDetail)
                    .ThenInclude(x => x!.StockInOrder)
            .AsSplitQuery();
    }
}
