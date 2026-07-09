using Domain.Entities.Storage;
using Domain.Queries.Storage;
using Domain.ReadModels.Storage;

namespace Domain.Interfaces;

/// <summary>
/// 库存批次仓储接口，负责按仓库、商品和批次号定位或创建批次并追踪余额。
/// </summary>
public interface IStockBatchRepository : IRepository<StockBatch>
{
    /// <summary>
    /// 按仓库和商品聚合库存批次余额、可用量及货值并分页返回。
    /// </summary>
    /// <param name="criteria">仓库、分类、商品、关键字和零库存筛选条件。</param>
    /// <param name="pageNumber">从 1 开始的页码。</param>
    /// <param name="pageSize">每页记录数。</param>
    /// <returns>仓库商品库存聚合行及筛选后的总分组数。</returns>
    Task<(IReadOnlyList<StockOverviewReadModel> Items, int Total)> GetOverviewPagedAsync(
        StockOverviewCriteria criteria,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// 分页读取库存批次投影，仅返回展示所需的余额、成本、效期及关联资料名称。
    /// </summary>
    /// <param name="criteria">批次筛选条件。</param>
    /// <param name="pageNumber">从 1 开始的页码。</param>
    /// <param name="pageSize">每页记录数。</param>
    /// <returns>匹配的库存批次投影及筛选后的总记录数。</returns>
    Task<(IReadOnlyList<StockBatchReadModel> Items, int Total)> GetQueryPagedAsync(
        StockBatchCriteria criteria,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// 批量读取指定库存批次，用于盘点创建时一次性固化账面数量和成本快照。
    /// </summary>
    /// <param name="ids">待读取的库存批次主键集合。</param>
    /// <returns>实际存在的库存批次集合；不存在的主键不会出现在结果中。</returns>
    Task<IReadOnlyList<StockBatch>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

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
