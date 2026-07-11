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
    /// 在当前数据库事务内按主键稳定顺序批量锁定入库单聚合，避免售后完成逐单查询。
    /// </summary>
    /// <param name="ids">待锁定的入库单主键集合。</param>
    /// <returns>存在的入库单聚合，按主键升序排列。</returns>
    Task<IReadOnlyList<StockInOrder>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids);

    /// <summary>批量只读入库单主单及商品明细快照，用于打印场景。</summary>
    /// <param name="ids">待读取的入库单主键集合。</param>
    /// <returns>存在的入库单完整聚合集合。</returns>
    Task<IReadOnlyList<StockInOrder>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    /// <summary>原子标记指定入库单已完成正式打印。</summary>
    /// <param name="ids">待标记的入库单主键集合。</param>
    /// <param name="updatedBy">确认打印的操作人主键。</param>
    /// <param name="updateName">确认打印的操作人名称快照。</param>
    /// <returns>实际标记成功的入库单数量。</returns>
    Task<int> MarkPrintedAsync(IReadOnlyCollection<Guid> ids, Guid? updatedBy, string? updateName);

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

    /// <summary>
    /// 查询已引用指定取货任务的销售退货入库单，用于重试返回既有结果和阻止跨单重复入库。
    /// </summary>
    /// <param name="pickupTaskIds">来源取货任务主键集合。</param>
    /// <returns>包含任一指定任务来源的入库单聚合。</returns>
    Task<IReadOnlyList<StockInOrder>> GetByPickupTaskIdsAsync(IReadOnlyCollection<Guid> pickupTaskIds);
}
