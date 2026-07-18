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
    /// <inheritdoc />
    public override async Task<SaleOrder?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public virtual async Task<SaleOrder?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByIdAsync(id);
        }

        var lockedOrders = DbSet.FromSqlInterpolated(
            $"SELECT * FROM sale_order WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(lockedOrders).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<SaleOrder> Data, int Total)> GetPagedAsync(
        Expression<Func<SaleOrder, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<SaleOrder, object>>? orderBy = null,
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
    public async Task<bool> ExistsOrderNoAsync(string orderNo, Guid? excludeId = null)
    {
        var normalizedOrderNo = orderNo.Trim();
        return await DbSet.AnyAsync(x =>
            x.OrderNo == normalizedOrderNo
            && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    /// <inheritdoc />
    public async Task<List<SaleOrder>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Where(x => x != Guid.Empty).Distinct().ToList();
        return idList.Count == 0
            ? []
            : await DbSet
                .AsNoTracking()
                .Where(x => idList.Contains(x.Id))
                .Include(x => x.Details)
                .AsSplitQuery()
                .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> MarkPrintedAsync(IReadOnlyCollection<Guid> ids, Guid? updatedBy, string? updateName)
    {
        var distinctIds = ids.Distinct().ToArray();
        if (Context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var orders = await DbSet.Where(order => distinctIds.Contains(order.Id)).ToListAsync();
            foreach (var order in orders)
            {
                order.PrintStatus = OrderPrintStatus.Printed;
                order.UpdateBy = updatedBy;
                order.UpdateName = updateName;
            }

            return orders.Count;
        }

        return await DbSet.Where(order => distinctIds.Contains(order.Id)).ExecuteUpdateAsync(setters => setters
            .SetProperty(order => order.PrintStatus, OrderPrintStatus.Printed)
            .SetProperty(order => order.UpdateBy, updatedBy)
            .SetProperty(order => order.UpdateName, updateName)
            .SetProperty(order => order.UpdateTime, DateTime.UtcNow));
    }

    private IQueryable<SaleOrder> BuildDetailQuery(IQueryable<SaleOrder>? source = null)
    {
        return (source ?? DbSet)
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
