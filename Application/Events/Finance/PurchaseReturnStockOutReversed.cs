using Application.Events;

namespace Application.Events.Finance;

/// <summary>
///     采购退货出库已反审核。
/// </summary>
/// <param name="StockOutOrderId">采购退货出库单 Id。</param>
public sealed record PurchaseReturnStockOutReversed(Guid StockOutOrderId) : IApplicationEvent;
