using Application.Events;
using Application.Interfaces;

namespace Application.Events.Finance;

/// <summary>
///     采购入库审核后同步供应商账单。
/// </summary>
public sealed class PurchaseStockInAuditedHandler(ISupplierBillService supplierBillService)
    : IApplicationEventHandler<PurchaseStockInAudited>
{
    /// <inheritdoc />
    public Task HandleAsync(PurchaseStockInAudited @event, CancellationToken cancellationToken = default)
        => supplierBillService.SyncPurchaseStockInAsync(@event.Order);
}
