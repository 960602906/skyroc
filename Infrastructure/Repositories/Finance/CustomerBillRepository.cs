using Domain.Entities.Finance;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 客户账单仓储实现，加载订单维度账单聚合并支持 PostgreSQL 行级锁。
/// </summary>
public class CustomerBillRepository(ApplicationDbContext context)
    : Repository<CustomerBill>(context), ICustomerBillRepository
{
    /// <inheritdoc />
    public override Task<CustomerBill?> GetByIdAsync(Guid id)
    {
        return BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public Task<CustomerBill?> GetBySaleOrderIdAsync(Guid saleOrderId)
    {
        return BuildDetailQuery().FirstOrDefaultAsync(x => x.SaleOrderId == saleOrderId);
    }

    /// <inheritdoc />
    public async Task<CustomerBill?> GetBySaleOrderIdForUpdateAsync(Guid saleOrderId)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetBySaleOrderIdAsync(saleOrderId);
        }

        var lockedBills = DbSet.FromSqlInterpolated(
            $"SELECT * FROM customer_bill WHERE sale_order_id = {saleOrderId} FOR UPDATE");
        return await BuildDetailQuery(lockedBills).FirstOrDefaultAsync(x => x.SaleOrderId == saleOrderId);
    }

    /// <inheritdoc />
    public Task<bool> ExistsBillNoAsync(string billNo)
    {
        var normalizedBillNo = billNo.Trim();
        return DbSet.AnyAsync(x => x.BillNo == normalizedBillNo);
    }

    /// <inheritdoc />
    public Task AddDetailAsync(CustomerBillDetail detail)
    {
        Context.Entry(detail).State = EntityState.Added;
        return Task.CompletedTask;
    }

    private IQueryable<CustomerBill> BuildDetailQuery(IQueryable<CustomerBill>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.Details)
            .Include(x => x.Customer)
            .Include(x => x.SaleOrder)
            .AsSplitQuery();
    }
}
