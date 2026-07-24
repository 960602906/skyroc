using Domain.Entities.Finance;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 客户结款凭证仓储实现，加载凭证明细并支持 PostgreSQL 行级锁。
/// </summary>
public class CustomerSettlementRepository(ApplicationDbContext context)
    : Repository<CustomerSettlement>(context), ICustomerSettlementRepository
{
    /// <inheritdoc />
    public override Task<CustomerSettlement?> GetByIdAsync(Guid id)
    {
        return BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<CustomerSettlement?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByIdAsync(id);
        }

        var lockedSettlements = DbSet.FromSqlInterpolated(
            $"SELECT * FROM customer_settlement WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(lockedSettlements).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public Task<bool> ExistsSettlementNoAsync(string settlementNo)
    {
        var normalizedSettlementNo = settlementNo.Trim();
        return DbSet.AnyAsync(x => x.SettlementNo == normalizedSettlementNo);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CustomerSettlement>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
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

    /// <inheritdoc />
    public async Task<CustomerSettlement?> GetBySettlementNoAsync(string settlementNo)
    {
        var normalizedSettlementNo = settlementNo.Trim();
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.SettlementNo == normalizedSettlementNo);
    }

    private IQueryable<CustomerSettlement> BuildDetailQuery(IQueryable<CustomerSettlement>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.Details)
            .Include(x => x.Customer)
            .AsSplitQuery();
    }
}
