namespace Domain.Entities.Finance;

/// <summary>
/// 供应商待结单据来源类型，区分采购入库正向应付与采购退货冲减。
/// </summary>
public enum SupplierBillSourceType
{
    /// <summary>
    /// 采购入库审核后形成的正向应付。
    /// </summary>
    PurchaseStockIn = 1,

    /// <summary>
    /// 采购退货出库审核后形成的应付冲减。
    /// </summary>
    PurchaseReturnOut = 2
}
