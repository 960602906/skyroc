using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 库存批次仓储，按仓库、商品和批次号定位批次以支持入出库审核时的余额更新。
/// </summary>
public class StockBatchRepository(ApplicationDbContext context)
    : Repository<StockBatch>(context), IStockBatchRepository
{
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
