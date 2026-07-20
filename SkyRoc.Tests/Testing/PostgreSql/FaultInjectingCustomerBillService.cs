using Application.Interfaces;
using Domain.Entities.AfterSales;
using Domain.Entities.Finance;
using Domain.Entities.Orders;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     装饰 <see cref="ICustomerBillService" />：在签收账单同步前按门闩注入失败。
/// </summary>
public sealed class FaultInjectingCustomerBillService(
    ICustomerBillService inner,
    FaultInjectionGate gate) : ICustomerBillService
{
    /// <inheritdoc />
    public Task<CustomerBill> SyncOrderAcceptanceAsync(SaleOrder saleOrder)
    {
        gate.ThrowIfCustomerBillAcceptanceArmed();
        return inner.SyncOrderAcceptanceAsync(saleOrder);
    }

    /// <inheritdoc />
    public Task<CustomerBill?> ApplyAfterSaleAdjustmentAsync(AfterSale afterSale)
        => inner.ApplyAfterSaleAdjustmentAsync(afterSale);
}
