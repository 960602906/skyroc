using Domain.Entities.Storage;
using Domain.Interfaces;
using Domain.Queries.Storage;
using Domain.ReadModels.Storage;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 库存批次仓储，支持库存总览聚合、批次分页，以及入出库审核时按唯一标识定位余额记录。
/// </summary>
public class StockBatchRepository(ApplicationDbContext context)
    : Repository<StockBatch>(context), IStockBatchRepository
{
    /// <inheritdoc />
    public async Task<(IReadOnlyList<StockOverviewReadModel> Items, int Total)> GetOverviewPagedAsync(
        StockOverviewCriteria criteria,
        int pageNumber,
        int pageSize)
    {
        var query = DbSet.AsNoTracking().AsQueryable();
        if (!criteria.IncludeZeroStock)
        {
            query = query.Where(batch => batch.CurrentQuantity > 0m);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var keyword = criteria.Keyword;
            query = query.Where(batch => batch.Goods.Name.Contains(keyword)
                                         || batch.Goods.Code.Contains(keyword));
        }

        if (criteria.WareId.HasValue)
        {
            query = query.Where(batch => batch.WareId == criteria.WareId.Value);
        }

        if (criteria.GoodsTypeId.HasValue)
        {
            query = query.Where(batch => batch.Goods.GoodsTypeId == criteria.GoodsTypeId.Value);
        }

        if (criteria.GoodsId.HasValue)
        {
            query = query.Where(batch => batch.GoodsId == criteria.GoodsId.Value);
        }

        var groupedQuery = query
            .GroupBy(batch => new
            {
                batch.WareId,
                WareName = batch.Ware.Name,
                batch.Goods.GoodsTypeId,
                GoodsTypeName = batch.Goods.GoodsType.Name,
                batch.GoodsId,
                GoodsName = batch.Goods.Name,
                GoodsCode = batch.Goods.Code,
                batch.BaseUnitId,
                BaseUnitName = batch.BaseUnit.Name
            })
            .Select(group => new StockOverviewReadModel
            {
                WareId = group.Key.WareId,
                WareName = group.Key.WareName,
                GoodsTypeId = group.Key.GoodsTypeId,
                GoodsTypeName = group.Key.GoodsTypeName,
                GoodsId = group.Key.GoodsId,
                GoodsName = group.Key.GoodsName,
                GoodsCode = group.Key.GoodsCode,
                BaseUnitId = group.Key.BaseUnitId,
                BaseUnitName = group.Key.BaseUnitName,
                CurrentQuantity = group.Sum(batch => batch.CurrentQuantity),
                AvailableQuantity = group.Sum(batch => batch.AvailableQuantity),
                StockValue = group.Sum(batch => batch.CurrentQuantity * batch.UnitCost),
                LastMovementTime = group.Max(batch => batch.LastMovementTime)
            });

        var total = await groupedQuery.CountAsync();
        var items = await groupedQuery
            .OrderBy(item => item.GoodsCode)
            .ThenBy(item => item.WareName)
            .ThenBy(item => item.GoodsId)
            .ThenBy(item => item.WareId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, total);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<StockBatchReadModel> Items, int Total)> GetQueryPagedAsync(
        StockBatchCriteria criteria,
        int pageNumber,
        int pageSize)
    {
        var query = DbSet.AsNoTracking().AsQueryable();
        if (!criteria.IncludeZeroStock)
        {
            query = query.Where(batch => batch.CurrentQuantity > 0m);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var keyword = criteria.Keyword;
            query = query.Where(batch => batch.Goods.Name.Contains(keyword)
                                         || batch.Goods.Code.Contains(keyword)
                                         || batch.BatchNo.Contains(keyword));
        }

        if (criteria.WareId.HasValue)
        {
            query = query.Where(batch => batch.WareId == criteria.WareId.Value);
        }

        if (criteria.GoodsTypeId.HasValue)
        {
            query = query.Where(batch => batch.Goods.GoodsTypeId == criteria.GoodsTypeId.Value);
        }

        if (criteria.GoodsId.HasValue)
        {
            query = query.Where(batch => batch.GoodsId == criteria.GoodsId.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.BatchNo))
        {
            query = query.Where(batch => batch.BatchNo == criteria.BatchNo);
        }

        if (criteria.ExpireDateStart.HasValue)
        {
            query = query.Where(batch => batch.ExpireDate >= criteria.ExpireDateStart.Value);
        }

        if (criteria.ExpireDateEnd.HasValue)
        {
            query = query.Where(batch => batch.ExpireDate <= criteria.ExpireDateEnd.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(batch => batch.ExpireDate == null)
            .ThenBy(batch => batch.ExpireDate)
            .ThenBy(batch => batch.Goods.Code)
            .ThenBy(batch => batch.BatchNo)
            .ThenBy(batch => batch.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(batch => new StockBatchReadModel
            {
                Id = batch.Id,
                WareId = batch.WareId,
                WareName = batch.Ware.Name,
                GoodsTypeId = batch.Goods.GoodsTypeId,
                GoodsTypeName = batch.Goods.GoodsType.Name,
                GoodsId = batch.GoodsId,
                GoodsName = batch.Goods.Name,
                GoodsCode = batch.Goods.Code,
                BatchNo = batch.BatchNo,
                BaseUnitId = batch.BaseUnitId,
                BaseUnitName = batch.BaseUnit.Name,
                CurrentQuantity = batch.CurrentQuantity,
                AvailableQuantity = batch.AvailableQuantity,
                UnitCost = batch.UnitCost,
                ProductDate = batch.ProductDate,
                ExpireDate = batch.ExpireDate,
                LastMovementTime = batch.LastMovementTime
            })
            .ToListAsync();
        return (items, total);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<StockBatch>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await DbSet
            .AsNoTracking()
            .Where(batch => ids.Contains(batch.Id))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<StockBatch?> GetByIdentityAsync(Guid wareId, Guid goodsId, string batchNo)
    {
        var normalizedBatchNo = batchNo.Trim();
        return await DbSet.FirstOrDefaultAsync(x =>
            x.WareId == wareId
            && x.GoodsId == goodsId
            && x.BatchNo == normalizedBatchNo);
    }

    /// <inheritdoc />
    public virtual async Task<StockBatch?> GetByIdentityForUpdateAsync(Guid wareId, Guid goodsId, string batchNo)
    {
        var normalizedBatchNo = batchNo.Trim();
        if (!Context.Database.IsNpgsql())
        {
            return await GetByIdentityAsync(wareId, goodsId, normalizedBatchNo);
        }

        return await DbSet.FromSqlInterpolated(
                $"SELECT * FROM stock_batch WHERE ware_id = {wareId} AND goods_id = {goodsId} AND batch_no = {normalizedBatchNo} FOR UPDATE")
            .FirstOrDefaultAsync();
    }
}
