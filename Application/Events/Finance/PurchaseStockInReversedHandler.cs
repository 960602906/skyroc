using Application.Events;
using Application.Interfaces;

namespace Application.Events.Finance;

/// <summary>
///     采购入库反审核后移除来源账单。
/// </summary>
public sealed class PurchaseStockInReversedHandler(ISupplierBillService supplierBillService)
    : IApplicationEventHandler<PurchaseStockInReversed>
{
    /// <inheritdoc />
    public Task HandleAsync(PurchaseStockInReversed @event, CancellationToken cancellationToken = default)
        => supplierBillService.RemoveBySourceDocumentAsync(@event.StockInOrderId, null);
}
