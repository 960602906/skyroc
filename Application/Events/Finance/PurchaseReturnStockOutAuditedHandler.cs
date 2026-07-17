using Application.Events;
using Application.Interfaces;

namespace Application.Events.Finance;

/// <summary>
///     采购退货出库审核后同步供应商账单。
/// </summary>
public sealed class PurchaseReturnStockOutAuditedHandler(ISupplierBillService supplierBillService)
    : IApplicationEventHandler<PurchaseReturnStockOutAudited>
{
    /// <inheritdoc />
    public Task HandleAsync(PurchaseReturnStockOutAudited @event, CancellationToken cancellationToken = default)
        => supplierBillService.SyncPurchaseReturnOutAsync(@event.Order);
}
