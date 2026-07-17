using Application.Events;
using Application.Events.Finance;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace SkyRoc.Tests.Testing;

/// <summary>
///     为直接 new 库存服务的单测组装同事务应用事件发布器（含供应商账单 handler）。
/// </summary>
internal static class TestApplicationEventPublisherFactory
{
    /// <summary>
    ///     创建已注册库存财务事件 handler 的 <see cref="IApplicationEventPublisher"/>。
    /// </summary>
    internal static IApplicationEventPublisher Create(
        ApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        var billService = new SupplierBillService(
            new SupplierBillRepository(context),
            new SupplierSettlementRepository(context),
            currentUser,
            DocumentNoGeneratorTestDouble.Instance);

        var services = new ServiceCollection();
        services.AddSingleton<ISupplierBillService>(billService);
        services.AddSingleton<IApplicationEventHandler<PurchaseStockInAudited>>(
            new PurchaseStockInAuditedHandler(billService));
        services.AddSingleton<IApplicationEventHandler<PurchaseStockInReversalRequested>>(
            new PurchaseStockInReversalRequestedHandler(billService));
        services.AddSingleton<IApplicationEventHandler<PurchaseStockInReversed>>(
            new PurchaseStockInReversedHandler(billService));
        services.AddSingleton<IApplicationEventHandler<PurchaseReturnStockOutAudited>>(
            new PurchaseReturnStockOutAuditedHandler(billService));
        services.AddSingleton<IApplicationEventHandler<PurchaseReturnStockOutReversalRequested>>(
            new PurchaseReturnStockOutReversalRequestedHandler(billService));
        services.AddSingleton<IApplicationEventHandler<PurchaseReturnStockOutReversed>>(
            new PurchaseReturnStockOutReversedHandler(billService));
        services.AddSingleton<IApplicationEventPublisher, ApplicationEventPublisher>();
        return services.BuildServiceProvider().GetRequiredService<IApplicationEventPublisher>();
    }
}
