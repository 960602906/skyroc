using Domain.Entities.Finance;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 供应商结算单仓储实现，加载结算明细并支持 PostgreSQL 行级锁。
/// </summary>
public class SupplierSettlementRepository(ApplicationDbContext context)
    : Repository<SupplierSettlement>(context), ISupplierSettlementRepository
{
    /// <inheritdoc />
    public override Task<SupplierSettlement?> GetByIdAsync(Guid id)
    {
        return BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<SupplierSettlement?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByIdAsync(id);
        }

        var lockedSettlements = DbSet.FromSqlInterpolated(
            $"SELECT * FROM supplier_settlement WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(lockedSettlements).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public Task<bool> ExistsSettlementNoAsync(string settlementNo)
    {
        var normalizedSettlementNo = settlementNo.Trim();
        return DbSet.AnyAsync(x => x.SettlementNo == normalizedSettlementNo);
    }

    /// <inheritdoc />
    public Task<bool> ExistsDetailByBillIdAsync(Guid supplierBillId)
    {
        return Context.Set<SupplierSettlementDetail>()
            .AnyAsync(x => x.SupplierBillId == supplierBillId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SupplierSettlement>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
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
    public async Task<SupplierSettlement?> GetBySettlementNoAsync(string settlementNo)
    {
        var normalizedSettlementNo = settlementNo.Trim();
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.SettlementNo == normalizedSettlementNo);
    }

    private IQueryable<SupplierSettlement> BuildDetailQuery(IQueryable<SupplierSettlement>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.Details)
            .Include(x => x.Supplier)
            .AsSplitQuery();
    }
}
