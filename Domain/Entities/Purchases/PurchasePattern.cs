namespace Domain.Entities.Purchases;

/// <summary>
/// 采购模式。
/// </summary>
public enum PurchasePattern
{
    /// <summary>
    /// 供应商直接供货。
    /// </summary>
    SupplierDirect = 1,
    /// <summary>
    /// 市场自采。
    /// </summary>
    MarketSelfPurchase = 2
}
