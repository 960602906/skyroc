using Domain.Entities.Storage;

namespace Domain.Interfaces;

/// <summary>
/// 库存流水仓储接口，负责只追加写入流水并按来源单据检索历史记录。
/// </summary>
public interface IStockLedgerRepository : IRepository<StockLedger>
{
    /// <summary>
    /// 读取指定来源单据在审核时写入且尚未被反向回滚的正向流水。
    /// </summary>
    /// <param name="sourceOrderId">来源入库、出库或盘点主单主键。</param>
    /// <returns>该来源单据当前生效的正向流水集合，用于反审核时逐条生成反向流水。</returns>
    Task<IReadOnlyList<StockLedger>> GetActiveBySourceOrderAsync(Guid sourceOrderId);
}
