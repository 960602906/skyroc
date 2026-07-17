using Application.Events;

namespace Application.Events.Finance;

/// <summary>
///     采购退货出库反审核前校验。
/// </summary>
/// <param name="StockOutOrderId">采购退货出库单 Id。</param>
public sealed record PurchaseReturnStockOutReversalRequested(Guid StockOutOrderId) : IApplicationEvent;
