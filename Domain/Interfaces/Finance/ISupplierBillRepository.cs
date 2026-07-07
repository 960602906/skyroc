using Domain.Entities.Finance;

namespace Domain.Interfaces;

/// <summary>
/// 供应商待结单据仓储接口，提供按来源出入库单读取、锁定和编号唯一性校验能力。
/// </summary>
public interface ISupplierBillRepository : IRepository<SupplierBill>
{
    /// <summary>
    /// 根据来源采购入库单读取供应商待结单据及明细。
    /// </summary>
    /// <param name="stockInOrderId">来源采购入库单主键。</param>
    /// <returns>包含明细的供应商待结单据；尚未生成时返回 <c>null</c>。</returns>
    Task<SupplierBill?> GetByStockInOrderIdAsync(Guid stockInOrderId);

    /// <summary>
    /// 根据来源采购退货出库单读取供应商待结单据及明细。
    /// </summary>
    /// <param name="stockOutOrderId">来源采购退货出库单主键。</param>
    /// <returns>包含明细的供应商待结单据；尚未生成时返回 <c>null</c>。</returns>
    Task<SupplierBill?> GetByStockOutOrderIdAsync(Guid stockOutOrderId);

    /// <summary>
    /// 在当前数据库事务内锁定来源采购入库单对应待结单据，供审核并发同步应付时串行更新。
    /// </summary>
    /// <param name="stockInOrderId">来源采购入库单主键。</param>
    /// <returns>被锁定的供应商待结单据；尚未生成时返回 <c>null</c>。</returns>
    Task<SupplierBill?> GetByStockInOrderIdForUpdateAsync(Guid stockInOrderId);

    /// <summary>
    /// 在当前数据库事务内锁定来源采购退货出库单对应待结单据，供审核并发同步应付时串行更新。
    /// </summary>
    /// <param name="stockOutOrderId">来源采购退货出库单主键。</param>
    /// <returns>被锁定的供应商待结单据；尚未生成时返回 <c>null</c>。</returns>
    Task<SupplierBill?> GetByStockOutOrderIdForUpdateAsync(Guid stockOutOrderId);

    /// <summary>
    /// 按主键集合读取供应商待结单据并在当前事务中锁定，供结算创建和作废串行校验余额。
    /// </summary>
    /// <param name="ids">供应商待结单据主键集合。</param>
    /// <returns>按主键稳定排序的供应商待结单据集合。</returns>
    Task<IReadOnlyList<SupplierBill>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids);

    /// <summary>
    /// 检查供应商待结单据编号是否已存在，避免自动编号冲突。
    /// </summary>
    /// <param name="billNo">待检查的单据编号。</param>
    /// <returns>编号已被占用时返回 <c>true</c>。</returns>
    Task<bool> ExistsBillNoAsync(string billNo);

    /// <summary>
    /// 显式追加供应商待结单据明细，供已存在单据增补商品行时标记为新增。
    /// </summary>
    /// <param name="detail">待追加的供应商待结单据明细。</param>
    Task AddDetailAsync(SupplierBillDetail detail);
}
