using Domain.Entities.Storage;
using Domain.Queries.Storage;

namespace Domain.Interfaces;

/// <summary>
/// 库存流水仓储接口，负责只追加写入流水并按来源单据检索历史记录。
/// </summary>
public interface IStockLedgerRepository : IRepository<StockLedger>
{
    /// <summary>
    /// 分页读取只追加库存台账，保留审核与反审核流水以支持完整追溯。
    /// </summary>
    /// <param name="criteria">仓库、商品、批次、方向、来源和发生时间筛选条件。</param>
    /// <param name="pageNumber">从 1 开始的页码。</param>
    /// <param name="pageSize">每页记录数。</param>
    /// <returns>按发生时间倒序排列的流水及筛选后的总记录数。</returns>
    Task<(IReadOnlyList<StockLedger> Items, int Total)> GetQueryPagedAsync(
        StockLedgerCriteria criteria,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// 读取指定来源单据在审核时写入且尚未被反向回滚的正向流水。
    /// </summary>
    /// <param name="sourceOrderId">来源入库、出库或盘点主单主键。</param>
    /// <returns>该来源单据当前生效的正向流水集合，用于反审核时逐条生成反向流水。</returns>
    Task<IReadOnlyList<StockLedger>> GetActiveBySourceOrderAsync(Guid sourceOrderId);
}
