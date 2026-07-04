using Domain.Entities.Storage;

namespace Domain.Interfaces;

/// <summary>
/// 库存盘点仓储接口，负责读取盘点聚合并在审核时锁定主单以保证调整幂等。
/// </summary>
public interface IStocktakingOrderRepository : IRepository<StocktakingOrder>
{
    /// <summary>
    /// 在当前数据库事务内锁定盘点主单及批次明细，防止同一盘点被并发审核。
    /// </summary>
    /// <param name="id">待锁定的盘点单主键。</param>
    /// <returns>包含全部批次明细的盘点单；不存在时返回 <c>null</c>。</returns>
    Task<StocktakingOrder?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 检查盘点单编号是否已被占用。
    /// </summary>
    /// <param name="stocktakingNo">待校验的盘点单业务编号。</param>
    /// <returns>存在同号盘点单时返回 <c>true</c>。</returns>
    Task<bool> ExistsStocktakingNoAsync(string stocktakingNo);
}
