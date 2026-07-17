using Application.Events;
using Application.Interfaces;

namespace Application.Events.Finance;

/// <summary>
///     采购退货出库反审核前校验账单是否可冲销。
/// </summary>
public sealed class PurchaseReturnStockOutReversalRequestedHandler(ISupplierBillService supplierBillService)
    : IApplicationEventHandler<PurchaseReturnStockOutReversalRequested>
{
    /// <inheritdoc />
    public Task HandleAsync(PurchaseReturnStockOutReversalRequested @event, CancellationToken cancellationToken = default)
        => supplierBillService.EnsureCanReverseSourceDocumentAsync(null, @event.StockOutOrderId);
}
