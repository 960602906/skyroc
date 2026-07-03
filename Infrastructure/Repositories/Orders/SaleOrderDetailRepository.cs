using System.Linq.Expressions;
using Domain.Entities.Orders;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 销售订单明细仓储。
/// </summary>
public class SaleOrderDetailRepository(ApplicationDbContext context)
    : Repository<SaleOrderDetail>(context), ISaleOrderDetailRepository
{
    /// <inheritdoc />
    public override async Task<SaleOrderDetail?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<SaleOrderDetail> Data, int Total)> GetPagedAsync(
        Expression<Func<SaleOrderDetail, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<SaleOrderDetail, object>>? orderBy = null,
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

    /// <inheritdoc />
    public async Task<List<SaleOrderDetail>> GetBySaleOrderIdAsync(Guid saleOrderId)
    {
        return await BuildDetailQuery()
            .Where(x => x.SaleOrderId == saleOrderId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<SaleOrderDetail>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Where(x => x != Guid.Empty).Distinct().ToList();
        return idList.Count == 0
            ? []
            : await BuildDetailQuery().Where(x => idList.Contains(x.Id)).ToListAsync();
    }

    private IQueryable<SaleOrderDetail> BuildDetailQuery()
    {
        return DbSet
            .Include(x => x.SaleOrder)
            .Include(x => x.Goods)
                .ThenInclude(x => x.GoodsType)
            .Include(x => x.GoodsUnit)
            .Include(x => x.BaseUnit)
            .Include(x => x.FixedGoodsUnit);
    }
}
