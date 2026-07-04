using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 库存流水仓储，只追加写入流水并按来源单据检索当前生效的正向流水。
/// </summary>
public class StockLedgerRepository(ApplicationDbContext context)
    : Repository<StockLedger>(context), IStockLedgerRepository
{
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
