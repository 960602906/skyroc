using System.Linq.Expressions;
using Domain.Entities.Orders;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 销售订单仓储。
/// </summary>
public class SaleOrderRepository(ApplicationDbContext context)
    : Repository<SaleOrder>(context), ISaleOrderRepository
{
    public override async Task<SaleOrder?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    public override async Task<(IEnumerable<SaleOrder> Data, int Total)> GetPagedAsync(
        Expression<Func<SaleOrder, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<SaleOrder, object>>? orderBy = null,
        bool isDescending = false)
    {
        var filteredQuery = DbSet.AsNoTracking().Where(predicate ?? (_ => true));
        var total = await filteredQuery.CountAsync();
        var query = BuildDetailQuery().AsNoTracking();
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

    public async Task<bool> ExistsOrderNoAsync(string orderNo, Guid? excludeId = null)
    {
        var normalizedOrderNo = orderNo.Trim();
        return await DbSet.AnyAsync(x =>
            x.OrderNo == normalizedOrderNo
            && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    public async Task<List<SaleOrder>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Where(x => x != Guid.Empty).Distinct().ToList();
        return idList.Count == 0
            ? []
            : await DbSet.Where(x => idList.Contains(x.Id)).ToListAsync();
    }

    private IQueryable<SaleOrder> BuildDetailQuery()
    {
        return DbSet
            .Include(x => x.Customer)
            .Include(x => x.Quotation)
            .Include(x => x.Ware)
            .Include(x => x.Details)
                .ThenInclude(x => x.Goods)
            .Include(x => x.Details)
                .ThenInclude(x => x.GoodsUnit)
            .Include(x => x.Details)
                .ThenInclude(x => x.BaseUnit)
            .Include(x => x.Details)
                .ThenInclude(x => x.FixedGoodsUnit)
            .Include(x => x.AuditLogs)
                .ThenInclude(x => x.AuditUser)
            .AsSplitQuery();
    }
}
