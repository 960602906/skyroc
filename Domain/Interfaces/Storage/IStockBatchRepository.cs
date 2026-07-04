using Domain.Entities.Storage;

namespace Domain.Interfaces;

/// <summary>
/// 库存批次仓储接口，负责按仓库、商品和批次号定位或创建批次并追踪余额。
/// </summary>
public interface IStockBatchRepository : IRepository<StockBatch>
{
    /// <summary>
    /// 按仓库、商品和批次号读取库存批次，用于入库审核时定位既有批次。
    /// </summary>
    /// <param name="wareId">批次所在仓库主键。</param>
    /// <param name="goodsId">批次所属商品主键。</param>
    /// <param name="batchNo">仓库内商品批次号。</param>
    /// <returns>命中的库存批次；不存在时返回 <c>null</c>。</returns>
    Task<StockBatch?> GetByIdentityAsync(Guid wareId, Guid goodsId, string batchNo);

    /// <summary>
    /// 在当前数据库事务内按仓库、商品和批次号锁定库存批次，防止并发审核丢失余额更新。
    /// </summary>
    /// <param name="wareId">批次所在仓库主键。</param>
    /// <param name="goodsId">批次所属商品主键。</param>
    /// <param name="batchNo">仓库内商品批次号。</param>
    /// <returns>已锁定的库存批次；不存在时返回 <c>null</c>。</returns>
    Task<StockBatch?> GetByIdentityForUpdateAsync(Guid wareId, Guid goodsId, string batchNo);
}
