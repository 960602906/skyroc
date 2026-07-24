using System.Linq.Expressions;
using Domain.Entities.Orders;
using Domain.Interfaces;
using Domain.ReadModels.BaseData;
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

    /// <inheritdoc />
    public async Task<List<SelectionOption>> SearchSelectionOptionsAsync(string? keyword, int take)
    {
        var query = DbSet.AsNoTracking();
        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword)
            ? null
            : keyword.Trim().ToLowerInvariant();

        if (normalizedKeyword is not null)
        {
            query = query
                .Where(x => x.OrderNo.ToLower().Contains(normalizedKeyword))
                .OrderByDescending(x => x.OrderNo.ToLower() == normalizedKeyword)
                .ThenByDescending(x => x.OrderNo.ToLower().StartsWith(normalizedKeyword))
                .ThenByDescending(x => x.CreateTime)
                .ThenByDescending(x => x.Id);
        }
        else
        {
            query = query
                .OrderByDescending(x => x.CreateTime)
                .ThenByDescending(x => x.Id);
        }

        return await ProjectSelectionOptions(query).Take(take).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<SelectionOption>> ResolveSelectionOptionsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await ProjectSelectionOptions(DbSet
                .AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .OrderByDescending(x => x.CreateTime)
                .ThenByDescending(x => x.Id))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SaleOrder?> GetByOrderNoAsync(string orderNo)
    {
        var normalizedOrderNo = orderNo.Trim();
        return await BuildDetailQuery()
            .FirstOrDefaultAsync(x => x.OrderNo == normalizedOrderNo);
    }

    private static IQueryable<SelectionOption> ProjectSelectionOptions(IQueryable<SaleOrder> query)
    {
        return query.Select(x => new SelectionOption
        {
            Id = x.Id,
            Label = x.OrderNo,
            SecondaryText = x.CustomerNameSnapshot
        });
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
