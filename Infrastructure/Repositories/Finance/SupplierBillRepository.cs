using Domain.Entities.Finance;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 供应商待结单据仓储实现，加载出入库维度单据聚合并支持 PostgreSQL 行级锁。
/// </summary>
public class SupplierBillRepository(ApplicationDbContext context)
    : Repository<SupplierBill>(context), ISupplierBillRepository
{
    /// <inheritdoc />
    public override Task<SupplierBill?> GetByIdAsync(Guid id)
    {
        return BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public Task<SupplierBill?> GetByStockInOrderIdAsync(Guid stockInOrderId)
    {
        return BuildDetailQuery().FirstOrDefaultAsync(x => x.StockInOrderId == stockInOrderId);
    }

    /// <inheritdoc />
    public Task<SupplierBill?> GetByStockOutOrderIdAsync(Guid stockOutOrderId)
    {
        return BuildDetailQuery().FirstOrDefaultAsync(x => x.StockOutOrderId == stockOutOrderId);
    }

    /// <inheritdoc />
    public async Task<SupplierBill?> GetByStockInOrderIdForUpdateAsync(Guid stockInOrderId)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByStockInOrderIdAsync(stockInOrderId);
        }

        var lockedBills = DbSet.FromSqlInterpolated(
            $"SELECT * FROM supplier_bill WHERE stock_in_order_id = {stockInOrderId} FOR UPDATE");
        return await BuildDetailQuery(lockedBills).FirstOrDefaultAsync(x => x.StockInOrderId == stockInOrderId);
    }

    /// <inheritdoc />
    public async Task<SupplierBill?> GetByStockOutOrderIdForUpdateAsync(Guid stockOutOrderId)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByStockOutOrderIdAsync(stockOutOrderId);
        }

        var lockedBills = DbSet.FromSqlInterpolated(
            $"SELECT * FROM supplier_bill WHERE stock_out_order_id = {stockOutOrderId} FOR UPDATE");
        return await BuildDetailQuery(lockedBills).FirstOrDefaultAsync(x => x.StockOutOrderId == stockOutOrderId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SupplierBill>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids)
    {
        var normalizedIds = ids.Distinct().OrderBy(id => id).ToArray();
        if (normalizedIds.Length == 0)
        {
            return [];
        }

        if (!Context.Database.IsNpgsql())
        {
            return await BuildDetailQuery()
                .Where(x => normalizedIds.Contains(x.Id))
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        var lockedBills = DbSet.FromSqlRaw(
            "SELECT * FROM supplier_bill WHERE id = ANY({0}) ORDER BY id FOR UPDATE",
            normalizedIds);
        return await BuildDetailQuery(lockedBills)
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    /// <inheritdoc />
    public Task<bool> ExistsBillNoAsync(string billNo)
    {
        var normalizedBillNo = billNo.Trim();
        return DbSet.AnyAsync(x => x.BillNo == normalizedBillNo);
    }

    /// <inheritdoc />
    public Task AddDetailAsync(SupplierBillDetail detail)
    {
        Context.Entry(detail).State = EntityState.Added;
        return Task.CompletedTask;
    }

    private IQueryable<SupplierBill> BuildDetailQuery(IQueryable<SupplierBill>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.Details)
            .Include(x => x.Supplier)
            .Include(x => x.StockInOrder)
            .Include(x => x.StockOutOrder)
            .AsSplitQuery();
    }
}
