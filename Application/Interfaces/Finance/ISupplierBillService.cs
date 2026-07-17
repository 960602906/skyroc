using Domain.Entities.Finance;
using Domain.Entities.Storage;

namespace Application.Interfaces;

/// <summary>
/// 供应商待结单据同步服务，将采购入库和采购退货出库审核事实幂等转换为供应商应付单据。
/// </summary>
public interface ISupplierBillService
{
    /// <summary>
    /// 根据已审核的采购入库单同步正向应付明细；重复调用会重建入库商品行。
    /// </summary>
    /// <param name="stockInOrder">已锁定并包含商品明细的采购入库单。</param>
    /// <returns>同步后的供应商待结单据聚合。</returns>
    Task<SupplierBill> SyncPurchaseStockInAsync(StockInOrder stockInOrder);

    /// <summary>
    /// 根据已审核的采购退货出库单同步应付冲减明细；重复调用会重建退货商品行。
    /// </summary>
    /// <param name="stockOutOrder">已锁定并包含商品明细的采购退货出库单。</param>
    /// <returns>同步后的供应商待结单据聚合。</returns>
    Task<SupplierBill> SyncPurchaseReturnOutAsync(StockOutOrder stockOutOrder);

    /// <summary>
    /// 校验来源出入库单是否允许反审核；已有有效结算金额时拒绝回滚。
    /// </summary>
    /// <param name="stockInOrderId">采购入库单主键；与 <paramref name="stockOutOrderId"/> 二选一。</param>
    /// <param name="stockOutOrderId">采购退货出库单主键；与 <paramref name="stockInOrderId"/> 二选一。</param>
    Task EnsureCanReverseSourceDocumentAsync(Guid? stockInOrderId, Guid? stockOutOrderId);

    /// <summary>
    /// 删除来源出入库单对应的待结单据；仅在尚未发生有效结算时执行。
    /// </summary>
    /// <param name="stockInOrderId">采购入库单主键；与 <paramref name="stockOutOrderId"/> 二选一。</param>
    /// <param name="stockOutOrderId">采购退货出库单主键；与 <paramref name="stockInOrderId"/> 二选一。</param>
    Task RemoveBySourceDocumentAsync(Guid? stockInOrderId, Guid? stockOutOrderId);
}
