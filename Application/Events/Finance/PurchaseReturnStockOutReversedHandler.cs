using Application.Events;
using Application.Interfaces;

namespace Application.Events.Finance;

/// <summary>
///     采购退货出库反审核后移除来源账单。
/// </summary>
public sealed class PurchaseReturnStockOutReversedHandler(ISupplierBillService supplierBillService)
    : IApplicationEventHandler<PurchaseReturnStockOutReversed>
{
    /// <inheritdoc />
    public Task HandleAsync(PurchaseReturnStockOutReversed @event, CancellationToken cancellationToken = default)
        => supplierBillService.RemoveBySourceDocumentAsync(null, @event.StockOutOrderId);
}
