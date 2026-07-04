using Domain.Entities.Storage;
using Domain.Interfaces;
using Domain.Queries.Storage;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 库存流水仓储，分页追溯完整台账，并按来源单据检索反审核所需的当前生效正向流水。
/// </summary>
public class StockLedgerRepository(ApplicationDbContext context)
    : Repository<StockLedger>(context), IStockLedgerRepository
{
    /// <inheritdoc />
    public async Task<(IReadOnlyList<StockLedger> Items, int Total)> GetQueryPagedAsync(
        StockLedgerCriteria criteria,
        int pageNumber,
        int pageSize)
    {
        var query = DbSet.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var keyword = criteria.Keyword;
            query = query.Where(ledger => ledger.WareNameSnapshot.Contains(keyword)
                                          || ledger.GoodsNameSnapshot.Contains(keyword)
                                          || ledger.GoodsCodeSnapshot.Contains(keyword)
                                          || ledger.BatchNoSnapshot.Contains(keyword));
        }

        if (criteria.WareId.HasValue)
        {
            query = query.Where(ledger => ledger.WareId == criteria.WareId.Value);
        }

        if (criteria.GoodsId.HasValue)
        {
            query = query.Where(ledger => ledger.GoodsId == criteria.GoodsId.Value);
        }

        if (criteria.StockBatchId.HasValue)
        {
            query = query.Where(ledger => ledger.StockBatchId == criteria.StockBatchId.Value);
        }

        if (criteria.Direction.HasValue)
        {
            query = query.Where(ledger => ledger.Direction == criteria.Direction.Value);
        }

        if (criteria.SourceType.HasValue)
        {
            query = query.Where(ledger => ledger.SourceType == criteria.SourceType.Value);
        }

        if (criteria.OccurredTimeStart.HasValue)
        {
            query = query.Where(ledger => ledger.OccurredTime >= criteria.OccurredTimeStart.Value);
        }

        if (criteria.OccurredTimeEnd.HasValue)
        {
            query = query.Where(ledger => ledger.OccurredTime <= criteria.OccurredTimeEnd.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(ledger => ledger.OccurredTime)
            .ThenByDescending(ledger => ledger.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StockLedger>> GetActiveBySourceOrderAsync(Guid sourceOrderId)
    {
        var reversedLedgerIds = await DbSet
            .Where(x => x.SourceOrderId == sourceOrderId && x.ReversedFromLedgerId != null)
            .Select(x => x.ReversedFromLedgerId!.Value)
            .ToListAsync();

        return await DbSet
            .Where(x => x.SourceOrderId == sourceOrderId
                        && x.ReversedFromLedgerId == null
                        && !reversedLedgerIds.Contains(x.Id))
            .OrderBy(x => x.OccurredTime)
            .ToListAsync();
    }
}
