using Domain.Entities.Storage;

namespace Domain.Interfaces;

/// <summary>
/// 入库单仓储接口，负责聚合读取入库主单、商品明细及来源采购单快照。
/// </summary>
public interface IStockInOrderRepository : IRepository<StockInOrder>
{
    /// <summary>
    /// 在当前数据库事务内锁定并读取入库单聚合，防止审核状态被并发修改。
    /// </summary>
    /// <param name="id">待锁定的入库单主键。</param>
    /// <returns>包含商品明细的入库单；不存在时返回 <c>null</c>。</returns>
    Task<StockInOrder?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 汇总指定采购明细已经正式审核入库且尚未反审核的基础单位数量。
    /// </summary>
    /// <param name="purchaseOrderDetailIds">来源采购单明细主键集合。</param>
    /// <param name="excludeOrderId">需要从汇总中排除的当前入库单主键。</param>
    /// <returns>以采购明细主键为键、已入库基础单位数量为值的只读字典。</returns>
    Task<IReadOnlyDictionary<Guid, decimal>> GetReceivedBaseQuantitiesAsync(
        IReadOnlyCollection<Guid> purchaseOrderDetailIds,
        Guid? excludeOrderId = null);

    /// <summary>
    /// 检查入库单编号是否已被其他入库单占用。
    /// </summary>
    /// <param name="inNo">待校验的入库单业务编号。</param>
    /// <param name="excludeId">编辑场景需要排除的入库单主键。</param>
    /// <returns>存在同号入库单时返回 <c>true</c>。</returns>
    Task<bool> ExistsInNoAsync(string inNo, Guid? excludeId = null);
}
