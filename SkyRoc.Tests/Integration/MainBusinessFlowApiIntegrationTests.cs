using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.DTOs.Delivery;
using Application.DTOs.Orders;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Domain.Entities.Customers;
using Domain.Entities.Delivery;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Tests.Testing;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Integration;

public class MainBusinessFlowApiIntegrationTests
{
    [Fact]
    public async Task MainBusinessFlow_CompletesFromOrderToReturnedReceipt_ThroughHttpApi()
    {
        using var factory = new MainBusinessFlowApiFactory();
        var seed = await factory.SeedCatalogAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.All);

        var order = await ReadDataAsync<SaleOrderDto>(await client.PostAsJsonAsync(
            "/api/orders",
            new CreateSaleOrderDto
            {
                CustomerId = seed.CustomerId,
                WareId = seed.WareId,
                OrderDate = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
                ReceiveDate = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
                ContactName = "张老师",
                ContactPhone = "13900000000",
                DeliveryAddress = "联调学校食堂收货区",
                Remark = "第二阶段完整链路联调",
                Details =
                [
                    new CreateSaleOrderDetailDto
                    {
                        GoodsId = seed.GoodsId,
                        GoodsUnitId = seed.GoodsUnitId,
                        Quantity = 6m,
                        FixedPrice = 4.5m,
                        FixedGoodsUnitId = seed.GoodsUnitId
                    }
                ]
            }));
        Assert.Equal(SaleOrderStatus.PendingAudit, order.OrderStatus);
        var orderDetail = Assert.Single(order.Details);

        var approvedOrder = await ReadDataAsync<SaleOrderDto>(await client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/approve",
            new SaleOrderAuditDto { Remark = "审核通过并进入主链路" }));
        Assert.Equal(SaleOrderStatus.SortingPending, approvedOrder.OrderStatus);

        var plans = await ReadDataAsync<List<PurchasePlanDto>>(await client.PostAsJsonAsync(
            "/api/purchase-plans/generate",
            new GeneratePurchasePlanFromOrdersDto
            {
                OrderIds = [order.Id],
                Remark = "订单需求转采购计划"
            }));
        var plan = Assert.Single(plans);
        var planDetail = Assert.Single(plan.Details);
        var planOrderRelation = Assert.Single(planDetail.OrderRelations);
        Assert.Equal(order.Id, planOrderRelation.SaleOrderId);
        Assert.Equal(orderDetail.Id, planOrderRelation.SaleOrderDetailId);
        await ReadDataAsync<List<PurchasePlanDto>>(await client.PutAsJsonAsync(
            "/api/purchase-plans/supplier",
            new AssignPurchasePlanSupplierDto
            {
                PlanIds = [plan.Id],
                SupplierId = seed.SupplierId
            }));
        await ReadDataAsync<List<PurchasePlanDto>>(await client.PutAsJsonAsync(
            "/api/purchase-plans/purchaser",
            new AssignPurchasePlanPurchaserDto
            {
                PlanIds = [plan.Id],
                PurchaserId = seed.PurchaserId
            }));

        var purchaseOrders = await ReadDataAsync<List<PurchaseOrderDto>>(await client.PostAsJsonAsync(
            "/api/purchase-orders/generate-from-plans",
            new GeneratePurchaseOrdersFromPlansDto
            {
                PlanIds = [plan.Id],
                ReceiveTime = new DateTime(2026, 7, 5, 6, 0, 0, DateTimeKind.Utc),
                Remark = "采购计划生成采购单"
            }));
        var purchaseOrder = Assert.Single(purchaseOrders);
        var purchaseOrderDetail = Assert.Single(purchaseOrder.Details);
        var purchasePlanRelation = Assert.Single(purchaseOrderDetail.PlanRelations);
        Assert.Equal(plan.Id, purchasePlanRelation.PurchasePlanId);
        Assert.Equal(planDetail.Id, purchasePlanRelation.PurchasePlanDetailId);
        var completedPurchaseOrder = await ReadDataAsync<PurchaseOrderDto>(await client.PostAsync(
            $"/api/purchase-orders/{purchaseOrder.Id}/complete",
            null));
        Assert.Equal(PurchaseOrderStatus.Completed, completedPurchaseOrder.BusinessStatus);

        var stockIn = await ReadDataAsync<StockInOrderDto>(await client.PostAsJsonAsync(
            "/api/stock-in/purchase",
            new CreatePurchaseStockInDto
            {
                WareId = seed.WareId,
                PurchaseOrderId = purchaseOrder.Id,
                SupplierId = seed.SupplierId,
                PurchaserId = seed.PurchaserId,
                PurchasePattern = PurchasePattern.SupplierDirect,
                InTime = new DateTime(2026, 7, 5, 7, 0, 0, DateTimeKind.Utc),
                Remark = "采购到货入库",
                Details =
                [
                    new CreateStockInDetailDto
                    {
                        PurchaseOrderDetailId = purchaseOrderDetail.Id,
                        GoodsId = seed.GoodsId,
                        GoodsUnitId = seed.GoodsUnitId,
                        Quantity = 6m,
                        UnitPrice = 3.2m,
                        ProductDate = new DateOnly(2026, 7, 4)
                    }
                ]
            }));
        var auditedStockIn = await ReadDataAsync<StockInOrderDto>(await client.PostAsJsonAsync(
            $"/api/stock-in/purchase/{stockIn.Id}/audit",
            new StockInAuditDto { Remark = "验收入库" }));
        Assert.Equal(StockDocumentStatus.Audited, auditedStockIn.BusinessStatus);
        Assert.Equal(purchaseOrder.Id, auditedStockIn.PurchaseOrderId);
        var auditedStockInDetail = Assert.Single(auditedStockIn.Details);
        Assert.Equal(purchaseOrderDetail.Id, auditedStockInDetail.PurchaseOrderDetailId);
        var stockBatchId = Assert.IsType<Guid>(auditedStockInDetail.StockBatchId);

        var stockOut = await ReadDataAsync<StockOutOrderDto>(await client.PostAsJsonAsync(
            "/api/stock-out/sale",
            new CreateSaleStockOutDto
            {
                WareId = seed.WareId,
                SaleOrderId = order.Id,
                CustomerId = seed.CustomerId,
                OutTime = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
                Remark = "销售订单全量出库",
                Details =
                [
                    new CreateStockOutDetailDto
                    {
                        SaleOrderDetailId = orderDetail.Id,
                        StockBatchId = stockBatchId,
                        GoodsUnitId = seed.GoodsUnitId,
                        Quantity = 6m,
                        UnitPrice = 4.5m
                    }
                ]
            }));
        var auditedStockOut = await ReadDataAsync<StockOutOrderDto>(await client.PostAsJsonAsync(
            $"/api/stock-out/sale/{stockOut.Id}/audit",
            new StockOutAuditDto { Remark = "审核出库并扣减库存" }));
        Assert.Equal(StockDocumentStatus.Audited, auditedStockOut.BusinessStatus);
        Assert.Equal(order.Id, auditedStockOut.SaleOrderId);
        var stockOutDetail = Assert.Single(auditedStockOut.Details);
        Assert.Equal(orderDetail.Id, stockOutDetail.SaleOrderDetailId);

        var orderAfterOutbound = await ReadDataAsync<SaleOrderDto>(await client.GetAsync($"/api/orders/{order.Id}"));
        Assert.True(orderAfterOutbound.HasOutSale);
        Assert.Equal(OrderOutStorageStatus.Generated, orderAfterOutbound.OutStorageStatus);

        var deliveryTask = await ReadDataAsync<DeliveryTaskDto>(await client.PostAsync(
            $"/api/delivery-tasks/generate/{stockOut.Id}",
            null));
        Assert.Equal(DeliveryTaskStatus.PendingAssign, deliveryTask.DeliveryStatus);
        Assert.Equal(stockOut.Id, deliveryTask.StockOutOrderId);
        Assert.Equal(order.Id, deliveryTask.SaleOrderId);
        Assert.Equal("联调学校食堂收货区", deliveryTask.DeliveryAddress);

        var assignedTasks = await ReadDataAsync<List<DeliveryTaskDto>>(await client.PutAsJsonAsync(
            "/api/delivery-tasks/driver",
            new AssignDeliveryDriverDto
            {
                TaskIds = [deliveryTask.Id],
                DriverId = seed.DriverId
            }));
        var assignedTask = Assert.Single(assignedTasks);
        Assert.Equal(DeliveryTaskStatus.Assigned, assignedTask.DeliveryStatus);
        Assert.Equal(seed.CarrierId, assignedTask.CarrierId);

        var startedTask = await ReadDataAsync<DeliveryTaskDto>(await client.PutAsync(
            $"/api/delivery-tasks/{deliveryTask.Id}/start",
            null));
        Assert.Equal(DeliveryTaskStatus.Delivering, startedTask.DeliveryStatus);
        var deliveringOrder = await ReadDataAsync<SaleOrderDto>(await client.GetAsync($"/api/orders/{order.Id}"));
        Assert.Equal(SaleOrderStatus.Delivering, deliveringOrder.OrderStatus);

        var signedReceipt = await ReadDataAsync<OrderReceiptDto>(await client.PutAsJsonAsync(
            $"/api/delivery-tasks/{deliveryTask.Id}/sign",
            new SignDeliveryTaskDto
            {
                SignerName = "张老师",
                Remark = "短收 0.5 千克",
                Details =
                [
                    new SignDeliveryCheckDetailDto
                    {
                        StockOutDetailId = stockOutDetail.Id,
                        AcceptedBaseQuantity = 5.5m,
                        CheckStatus = OrderCustomerCheckStatus.Rejected,
                        Remark = "运输损耗"
                    }
                ]
            }));
        Assert.Equal(deliveryTask.Id, signedReceipt.DeliveryTaskId);
        Assert.Equal(order.Id, signedReceipt.SaleOrderId);
        Assert.Equal(stockOut.Id, signedReceipt.StockOutOrderId);
        Assert.Equal(24.75m, Assert.Single(signedReceipt.CheckDetails).AcceptedAmount);

        var signedOrder = await ReadDataAsync<SaleOrderDto>(await client.GetAsync($"/api/orders/{order.Id}"));
        var checkedOrderDetail = Assert.Single(signedOrder.Details);
        Assert.Equal(SaleOrderStatus.Signed, signedOrder.OrderStatus);
        Assert.Equal(24.75m, signedOrder.SettlementPrice);
        Assert.Equal(5.5m, checkedOrderDetail.CustomerCheckBaseQuantity);
        Assert.Equal(OrderCustomerCheckStatus.Rejected, checkedOrderDetail.CustomerCheckStatus);

        var returnedReceipt = await ReadDataAsync<OrderReceiptDto>(await client.PutAsJsonAsync(
            $"/api/delivery-tasks/{deliveryTask.Id}/receipt",
            new ReturnOrderReceiptDto
            {
                ReceiptImageUrl = "https://example.test/receipts/p2-flow-001.jpg",
                Remark = "纸质回单已归档"
            }));
        Assert.NotNull(returnedReceipt.ReturnedTime);
        Assert.Equal("https://example.test/receipts/p2-flow-001.jpg", returnedReceipt.ReceiptImageUrl);
        var returnedOrder = await ReadDataAsync<SaleOrderDto>(await client.GetAsync($"/api/orders/{order.Id}"));
        Assert.Equal(OrderReturnStatus.Returned, returnedOrder.ReturnStatus);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var batch = await context.StockBatches.AsNoTracking().SingleAsync(x => x.Id == stockBatchId);
        Assert.Equal(0m, batch.CurrentQuantity);
        Assert.Equal(0m, batch.AvailableQuantity);
        var ledgers = await context.StockLedgers
            .AsNoTracking()
            .Where(x => x.StockBatchId == stockBatchId)
            .OrderBy(x => x.OccurredTime)
            .ToListAsync();
        Assert.Collection(
            ledgers,
            inbound =>
            {
                Assert.Equal(StockLedgerDirection.Increase, inbound.Direction);
                Assert.Equal(6m, inbound.ChangeQuantity);
                Assert.Equal(6m, inbound.BalanceQuantity);
            },
            outbound =>
            {
                Assert.Equal(StockLedgerDirection.Decrease, outbound.Direction);
                Assert.Equal(6m, outbound.ChangeQuantity);
                Assert.Equal(0m, outbound.BalanceQuantity);
            });
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(result);
        return Assert.IsType<T>(result.Data);
    }

    private sealed class MainBusinessFlowApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"main-business-flow-{Guid.NewGuid():N}";

        public async Task<CatalogSeed> SeedCatalogAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ware = new Ware
            {
                Id = Guid.NewGuid(),
                Name = "第二阶段联调仓",
                Code = "P2_FLOW_WARE"
            };
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = "第二阶段联调学校",
                Code = "P2_FLOW_CUSTOMER",
                DefaultWareId = ware.Id
            };
            var goodsType = new GoodsType
            {
                Id = Guid.NewGuid(),
                Name = "联调蔬菜",
                Code = "P2_FLOW_VEGETABLE"
            };
            var goods = new GoodsEntity
            {
                Id = Guid.NewGuid(),
                Name = "联调番茄",
                Code = "P2_FLOW_TOMATO",
                GoodsTypeId = goodsType.Id,
                DefaultWareId = ware.Id,
                Spec = "一级"
            };
            var goodsUnit = new GoodsUnit
            {
                Id = Guid.NewGuid(),
                GoodsId = goods.Id,
                Name = "千克",
                Code = "KG",
                ConversionRate = 1m,
                IsBaseUnit = true
            };
            goods.BaseUnitId = goodsUnit.Id;
            var supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = "第二阶段联调供应商",
                Code = "P2_FLOW_SUPPLIER",
                ContactName = "王经理",
                ContactPhone = "13800000000"
            };
            var purchaser = new Purchaser
            {
                Id = Guid.NewGuid(),
                Name = "第二阶段联调采购员",
                Code = "P2_FLOW_PURCHASER"
            };
            var carrier = new Carrier
            {
                Id = Guid.NewGuid(),
                Name = "第二阶段联调承运商",
                Code = "P2_FLOW_CARRIER"
            };
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                Name = "第二阶段联调司机",
                Code = "P2_FLOW_DRIVER",
                Phone = "13700000000",
                CarrierId = carrier.Id
            };

            await context.Wares.AddAsync(ware);
            await context.Customers.AddAsync(customer);
            await context.GoodsTypes.AddAsync(goodsType);
            await context.Goods.AddAsync(goods);
            await context.GoodsUnits.AddAsync(goodsUnit);
            await context.Suppliers.AddAsync(supplier);
            await context.Purchasers.AddAsync(purchaser);
            await context.Carriers.AddAsync(carrier);
            await context.Drivers.AddAsync(driver);
            await context.SaveChangesAsync();
            return new CatalogSeed(
                customer.Id,
                goods.Id,
                goodsUnit.Id,
                ware.Id,
                supplier.Id,
                purchaser.Id,
                carrier.Id,
                driver.Id);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=localhost;Database=skyroc_tests;Username=test;Password=test");
            builder.UseSetting("JwtSettings:SecretKey", "test-only-secret-key-with-at-least-32-bytes");
            builder.UseSetting("Redis:Enabled", "false");
            builder.ConfigureTestServices(services =>
            {
                services.UseIsolatedInMemoryPersistence(_databaseName);
                services.RemoveAll<IUnitOfWork>();
                services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                        options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                        options.DefaultForbidScheme = TestAuthHandler.AuthenticationScheme;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.AuthenticationScheme,
                        _ => { });
            });
        }
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "MainBusinessFlowIntegrationTest";
        public const string PermissionsHeader = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(PermissionsHeader, out var values))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "10000000-0000-0000-0000-000000000020"),
                new(ClaimTypes.Name, "main-business-flow-test")
            };
            claims.AddRange(values.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(permission => new Claim(AuthConstants.PermissionClaimType, permission)));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationScheme));
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, AuthenticationScheme)));
        }
    }

    private sealed class InMemoryUnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        public bool HasActiveTransaction { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return context.SaveChangesAsync(cancellationToken);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = true;
            return Task.CompletedTask;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            await context.SaveChangesAsync(cancellationToken);
            HasActiveTransaction = false;
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            context.ChangeTracker.Clear();
            HasActiveTransaction = false;
            return Task.CompletedTask;
        }

        public Task<int> ExecuteSqlAsync(string sql, params object[] parameters)
        {
            throw new NotSupportedException();
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                if (HasActiveTransaction)
                    await RollbackTransactionAsync(cancellationToken);

                throw;
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await CommitTransactionAsync(cancellationToken);
                return result;
            }
            catch
            {
                if (HasActiveTransaction)
                    await RollbackTransactionAsync(cancellationToken);

                throw;
            }
        }

        public void ClearChangeTracking()
        {
            context.ChangeTracker.Clear();
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed record CatalogSeed(
        Guid CustomerId,
        Guid GoodsId,
        Guid GoodsUnitId,
        Guid WareId,
        Guid SupplierId,
        Guid PurchaserId,
        Guid CarrierId,
        Guid DriverId);
}
