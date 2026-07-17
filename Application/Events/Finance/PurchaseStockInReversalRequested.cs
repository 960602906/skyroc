using Application.Events;

namespace Application.Events.Finance;

/// <summary>
///     采购入库反审核前校验：账单是否允许冲销。
/// </summary>
/// <param name="StockInOrderId">采购入库单 Id。</param>
public sealed record PurchaseStockInReversalRequested(Guid StockInOrderId) : IApplicationEvent;
