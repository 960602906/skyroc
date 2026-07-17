using Application.Events;
using Application.Interfaces;

namespace Application.Events.Finance;

/// <summary>
///     采购入库反审核前校验账单是否可冲销。
/// </summary>
public sealed class PurchaseStockInReversalRequestedHandler(ISupplierBillService supplierBillService)
    : IApplicationEventHandler<PurchaseStockInReversalRequested>
{
    /// <inheritdoc />
    public Task HandleAsync(PurchaseStockInReversalRequested @event, CancellationToken cancellationToken = default)
        => supplierBillService.EnsureCanReverseSourceDocumentAsync(@event.StockInOrderId, null);
}
