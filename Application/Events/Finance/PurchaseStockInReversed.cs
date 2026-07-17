using Application.Events;

namespace Application.Events.Finance;

/// <summary>
///     采购入库已反审核，需移除来源账单明细。
/// </summary>
/// <param name="StockInOrderId">采购入库单 Id。</param>
public sealed record PurchaseStockInReversed(Guid StockInOrderId) : IApplicationEvent;
