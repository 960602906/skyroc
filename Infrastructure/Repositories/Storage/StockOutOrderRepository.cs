using System.Linq.Expressions;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 出库单仓储，读取时聚合仓库、客户、供应商、部门、库存批次和商品明细。
/// </summary>
public class StockOutOrderRepository(ApplicationDbContext context)
    : Repository<StockOutOrder>(context), IStockOutOrderRepository
{
    /// <inheritdoc />
    public override async Task<StockOutOrder?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public virtual async Task<StockOutOrder?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByIdAsync(id);
        }

        var lockedOrders = DbSet.FromSqlInterpolated(
            $"SELECT * FROM stock_out_order WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(lockedOrders).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StockOutOrder>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
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
    public async Task<int> MarkPrintedAsync(IReadOnlyCollection<Guid> ids, Guid? updatedBy, string? updateName)
    {
        var distinctIds = ids.Distinct().ToArray();
        if (Context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var orders = await DbSet.Where(order => distinctIds.Contains(order.Id)).ToListAsync();
            foreach (var order in orders)
            {
                order.PrintStatus = StockPrintStatus.Printed;
                order.UpdateBy = updatedBy;
                order.UpdateName = updateName;
            }

            return orders.Count;
        }

        return await DbSet.Where(order => distinctIds.Contains(order.Id)).ExecuteUpdateAsync(setters => setters
            .SetProperty(order => order.PrintStatus, StockPrintStatus.Printed)
            .SetProperty(order => order.UpdateBy, updatedBy)
            .SetProperty(order => order.UpdateName, updateName)
            .SetProperty(order => order.UpdateTime, DateTime.UtcNow));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetOutboundBaseQuantitiesAsync(
        IReadOnlyCollection<Guid> saleOrderDetailIds,
        Guid? excludeOrderId = null)
    {
        if (saleOrderDetailIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var outboundRows = await DbSet
            .AsNoTracking()
            .Where(order => order.OrderType == StockOutOrderType.Sale
                            && order.BusinessStatus == StockDocumentStatus.Audited
                            && (!excludeOrderId.HasValue || order.Id != excludeOrderId.Value))
            .SelectMany(order => order.Details)
            .Where(detail => detail.SaleOrderDetailId.HasValue
                             && saleOrderDetailIds.Contains(detail.SaleOrderDetailId.Value))
            .Select(detail => new
            {
                SaleOrderDetailId = detail.SaleOrderDetailId!.Value,
                detail.BaseQuantity
            })
            .ToListAsync();

        return outboundRows
            .GroupBy(row => row.SaleOrderDetailId)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.BaseQuantity));
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetLatestOutboundTimeAsync(Guid saleOrderId, Guid? excludeOrderId = null)
    {
        return await DbSet
            .AsNoTracking()
            .Where(order => order.OrderType == StockOutOrderType.Sale
                            && order.SaleOrderId == saleOrderId
                            && order.BusinessStatus == StockDocumentStatus.Audited
                            && (!excludeOrderId.HasValue || order.Id != excludeOrderId.Value))
            .MaxAsync(order => (DateTime?)order.OutTime);
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<StockOutOrder> Data, int Total)> GetPagedAsync(
        Expression<Func<StockOutOrder, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<StockOutOrder, object>>? orderBy = null,
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
    public async Task<bool> ExistsOutNoAsync(string outNo, Guid? excludeId = null)
    {
        var normalizedOutNo = outNo.Trim();
        return await DbSet.AnyAsync(x =>
            x.OutNo == normalizedOutNo
            && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    /// <summary>
    /// 构建包含仓库、业务方、库存批次和商品明细聚合的出库单查询。
    /// </summary>
    /// <param name="source">可选的已锁定出库主单查询。</param>
    /// <returns>预加载出库单完整业务聚合的查询。</returns>
    private IQueryable<StockOutOrder> BuildDetailQuery(IQueryable<StockOutOrder>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.Ware)
            .Include(x => x.Customer)
            .Include(x => x.Supplier)
            .Include(x => x.Department)
            .Include(x => x.Details)
                .ThenInclude(x => x.Goods)
            .Include(x => x.Details)
                .ThenInclude(x => x.GoodsUnit)
            .Include(x => x.Details)
                .ThenInclude(x => x.StockBatch)
            .AsSplitQuery();
    }
}
