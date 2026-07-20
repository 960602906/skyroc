using System.Linq.Expressions;
using Domain.Entities.AfterSales;
using Domain.Interfaces;
using Domain.ReadModels.AfterSales;
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
    public async Task<(IReadOnlyList<AfterSaleListItemReadModel> Data, int Total)> GetListPageAsync(
        Expression<Func<AfterSale, bool>>? predicate,
        int pageNumber,
        int pageSize)
    {
        var query = DbSet.AsNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        var total = await query.CountAsync();
        var data = await query
            .AsSingleQuery()
            .OrderByDescending(x => x.CreateTime)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AfterSaleListItemReadModel
            {
                Id = x.Id,
                CreateTime = x.CreateTime,
                AfterSaleNo = x.AfterSaleNo,
                SaleOrderId = x.SaleOrderId,
                SaleOrderNo = x.SaleOrderNoSnapshot,
                CustomerName = x.CustomerNameSnapshot,
                OrderPrice = x.OrderPrice,
                SettlementPrice = x.SettlementPrice,
                AfterStatus = x.AfterStatus,
                ContactName = x.ContactNameSnapshot,
                ContactPhone = x.ContactPhoneSnapshot,
                Goods = x.Goods
                    .OrderBy(item => item.Id)
                    .Select(item => new AfterSaleListGoodsReadModel
                    {
                        AfterSaleType = item.AfterSaleType,
                        HandleType = item.HandleType,
                        RefundAmount = item.RefundAmount
                    })
                    .ToList(),
                LatestAuditAction = x.AuditLogs
                    .OrderByDescending(log => log.AuditTime)
                    .ThenByDescending(log => log.CreateTime)
                    .ThenByDescending(log => log.Id)
                    .Select(log => (AfterSaleAuditAction?)log.Action)
                    .FirstOrDefault(),
                HasPickupTasks = x.PickupTasks.Any()
            })
            .ToListAsync();

        return (data, total);
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
