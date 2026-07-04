using Domain.Entities.Storage;

namespace Domain.Interfaces;

/// <summary>
/// 出库单仓储接口，负责聚合读取出库主单、商品批次明细及销售订单来源关系。
/// </summary>
public interface IStockOutOrderRepository : IRepository<StockOutOrder>
{
    /// <summary>
    /// 在当前数据库事务内锁定并读取出库单聚合，防止审核状态被并发修改。
    /// </summary>
    /// <param name="id">待锁定的出库单主键。</param>
    /// <returns>包含商品明细的出库单；不存在时返回 <c>null</c>。</returns>
    Task<StockOutOrder?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 汇总指定销售订单明细已经正式审核出库且尚未反审核的基础单位数量。
    /// </summary>
    /// <param name="saleOrderDetailIds">来源销售订单商品明细主键集合。</param>
    /// <param name="excludeOrderId">需要从汇总中排除的当前出库单主键。</param>
    /// <returns>以销售订单明细主键为键、已出库基础单位数量为值的只读字典。</returns>
    Task<IReadOnlyDictionary<Guid, decimal>> GetOutboundBaseQuantitiesAsync(
        IReadOnlyCollection<Guid> saleOrderDetailIds,
        Guid? excludeOrderId = null);

    /// <summary>
    /// 查询指定销售订单当前已审核且尚未反审核的最近一次实际出库时间。
    /// </summary>
    /// <param name="saleOrderId">来源销售订单主键。</param>
    /// <param name="excludeOrderId">需要从查询中排除的当前出库单主键。</param>
    /// <returns>最近一次有效出库时间（UTC）；没有有效出库时返回 <c>null</c>。</returns>
    Task<DateTime?> GetLatestOutboundTimeAsync(Guid saleOrderId, Guid? excludeOrderId = null);

    /// <summary>
    /// 检查出库单编号是否已被其他出库单占用。
    /// </summary>
    /// <param name="outNo">待校验的出库单业务编号。</param>
    /// <param name="excludeId">编辑场景需要排除的出库单主键。</param>
    /// <returns>存在同号出库单时返回 <c>true</c>。</returns>
    Task<bool> ExistsOutNoAsync(string outNo, Guid? excludeId = null);
}
