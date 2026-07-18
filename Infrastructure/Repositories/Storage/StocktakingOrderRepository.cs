using System.Linq.Expressions;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 库存盘点仓储，聚合读取仓库和批次盘点明细，并支持审核事务内的主单行锁。
/// </summary>
public class StocktakingOrderRepository(ApplicationDbContext context)
    : Repository<StocktakingOrder>(context), IStocktakingOrderRepository
{
    /// <inheritdoc />
    public override async Task<StocktakingOrder?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public virtual async Task<StocktakingOrder?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByIdAsync(id);
        }

        var lockedOrders = DbSet.FromSqlInterpolated(
            $"SELECT * FROM stocktaking_order WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(lockedOrders).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<StocktakingOrder> Data, int Total)> GetPagedAsync(
        Expression<Func<StocktakingOrder, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<StocktakingOrder, object>>? orderBy = null,
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
    public async Task<bool> ExistsStocktakingNoAsync(string stocktakingNo)
    {
        var normalizedNo = stocktakingNo.Trim();
        return await DbSet.AnyAsync(x => x.StocktakingNo == normalizedNo);
    }

    /// <summary>
    /// 构建包含仓库和全部批次明细的盘点聚合查询。
    /// </summary>
    /// <param name="source">可选的已锁定盘点主单查询。</param>
    /// <returns>预加载盘点完整业务聚合的查询。</returns>
    private IQueryable<StocktakingOrder> BuildDetailQuery(IQueryable<StocktakingOrder>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.Ware)
            .Include(x => x.Details)
            .AsSplitQuery();
    }
}
