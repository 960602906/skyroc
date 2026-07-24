using Domain.Entities.Purchases;

namespace Domain.Interfaces;

/// <summary>
/// 采购单仓储接口，负责聚合读取采购单、商品明细和采购计划占用关系。
/// </summary>
public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    /// <summary>
    /// 在当前数据库事务内锁定并读取采购单聚合，用于完成/取消/编辑/删除状态流转
    /// 以及串行校验采购入库数量，避免并发双重终结或计划占用不一致。
    /// </summary>
    /// <param name="id">待锁定的采购单主键。</param>
    /// <returns>包含商品明细的采购单；不存在时返回 <c>null</c>。</returns>
    Task<PurchaseOrder?> GetByIdForUpdateAsync(Guid id);

    /// <summary>
    /// 检查采购单编号是否已被其他采购单占用。
    /// </summary>
    /// <param name="purchaseNo">待校验的采购单业务编号。</param>
    /// <param name="excludeId">编辑场景需要排除的采购单主键。</param>
    /// <returns>存在同号采购单时返回 <c>true</c>。</returns>
    Task<bool> ExistsPurchaseNoAsync(string purchaseNo, Guid? excludeId = null);

    /// <summary>批量只读采购单主单及商品明细快照,用于打印等多单据场景。</summary>
    /// <param name="ids">待读取的采购单主键集合。</param>
    /// <returns>存在的采购单完整聚合集合。</returns>
    Task<IReadOnlyList<PurchaseOrder>> GetByIdsAsync(IReadOnlyCollection<Guid> ids);

    /// <summary>
    /// 根据采购单号查询采购单详情（含商品明细和计划关联）。
    /// </summary>
    /// <param name="purchaseNo">采购单号。</param>
    /// <returns>采购单聚合；不存在时返回 <c>null</c>。</returns>
    Task<PurchaseOrder?> GetByPurchaseNoAsync(string purchaseNo);
}
