using System.Linq.Expressions;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 入库单仓储，读取时聚合仓库、供应商、客户、部门、采购员和商品明细。
/// </summary>
public class StockInOrderRepository(ApplicationDbContext context)
    : Repository<StockInOrder>(context), IStockInOrderRepository
{
    /// <inheritdoc />
    public override async Task<StockInOrder?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public virtual async Task<StockInOrder?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByIdAsync(id);
        }

        var lockedOrders = DbSet.FromSqlInterpolated(
            $"SELECT * FROM stock_in_order WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(lockedOrders).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetReceivedBaseQuantitiesAsync(
        IReadOnlyCollection<Guid> purchaseOrderDetailIds,
        Guid? excludeOrderId = null)
    {
        if (purchaseOrderDetailIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var receivedRows = await DbSet
            .AsNoTracking()
            .Where(order => order.OrderType == StockInOrderType.Purchase
                            && order.BusinessStatus == StockDocumentStatus.Audited
                            && (!excludeOrderId.HasValue || order.Id != excludeOrderId.Value))
            .SelectMany(order => order.Details)
            .Where(detail => detail.PurchaseOrderDetailId.HasValue
                             && purchaseOrderDetailIds.Contains(detail.PurchaseOrderDetailId.Value))
            .Select(detail => new
            {
                PurchaseOrderDetailId = detail.PurchaseOrderDetailId!.Value,
                detail.BaseQuantity
            })
            .ToListAsync();

        return receivedRows
            .GroupBy(row => row.PurchaseOrderDetailId)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.BaseQuantity));
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<StockInOrder> Data, int Total)> GetPagedAsync(
        Expression<Func<StockInOrder, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<StockInOrder, object>>? orderBy = null,
        bool isDescending = false)
    {
        var filteredQuery = DbSet.AsNoTracking().Where(predicate ?? (_ => true));
        var total = await filteredQuery.CountAsync();
        var query = BuildDetailQuery().AsNoTracking();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (orderBy != null)
        {
            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        }

        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (data, total);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsInNoAsync(string inNo, Guid? excludeId = null)
    {
        var normalizedInNo = inNo.Trim();
        return await DbSet.AnyAsync(x =>
            x.InNo == normalizedInNo
            && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    /// <summary>
    /// 构建包含仓库、业务方和商品明细聚合的入库单查询。
    /// </summary>
    /// <returns>预加载入库单完整业务聚合的查询。</returns>
    private IQueryable<StockInOrder> BuildDetailQuery(IQueryable<StockInOrder>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.Ware)
            .Include(x => x.Supplier)
            .Include(x => x.Customer)
            .Include(x => x.Department)
            .Include(x => x.Purchaser)
            .Include(x => x.Details)
                .ThenInclude(x => x.Goods)
            .Include(x => x.Details)
                .ThenInclude(x => x.GoodsUnit)
            .AsSplitQuery();
    }
}
