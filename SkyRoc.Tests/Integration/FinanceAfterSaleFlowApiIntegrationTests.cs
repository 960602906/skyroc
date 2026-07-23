using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.DTOs.AfterSales;
using Application.DTOs.Delivery;
using Application.DTOs.Finance;
using Application.DTOs.Orders;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Domain.Entities.AfterSales;
using Domain.Entities.Customers;
using Domain.Entities.Delivery;
using Domain.Entities.Finance;
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

/// <summary>
/// 第三阶段联调测试：使用真实控制器、应用服务和仓储贯通“订单签收 -> 客户账单 -> 售后冲减 -> 客户结款”
/// 以及“采购入库审核 -> 供应商待结单据 -> 供应商结算”两条财务主链路，验证账单生成、售后调整、
/// 部分/全部结款、作废回滚和最终余额一致性。
/// </summary>
public class FinanceAfterSaleFlowApiIntegrationTests
{
    [Fact]
    public async Task ThirdStageFlow_CompletesAfterSaleAndSettlements_ThroughHttpApi()
    {
        using var factory = new FinanceAfterSaleFlowApiFactory();
        var seed = await factory.SeedCatalogAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.All);

        // 订单 -> 采购计划 -> 采购单 -> 采购入库审核（生成供应商待结单据）。
        var order = await ReadDataAsync<SaleOrderDto>(await client.PostAsJsonAsync(
            "/api/orders",
            new CreateSaleOrderDto
            {
                CustomerId = seed.CustomerId,
                WareId = seed.WareId,
                OrderDate = new DateTime(2026, 7, 6, 8, 0, 0, DateTimeKind.Utc),
                ReceiveDate = new DateTime(2026, 7, 7, 8, 0, 0, DateTimeKind.Utc),
                ContactName = "李老师",
                ContactPhone = "13900000001",
                DeliveryAddress = "第三阶段联调学校食堂",
                Remark = "第三阶段财务联调",
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
        var orderDetail = Assert.Single(order.Details);
        await ReadDataAsync<SaleOrderDto>(await client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/approve",
            new SaleOrderAuditDto { Remark = "审核通过进入第三阶段联调" }));

        var plans = await ReadDataAsync<List<PurchasePlanDto>>(await client.PostAsJsonAsync(
            "/api/purchase-plans/generate",
            new GeneratePurchasePlanFromOrdersDto { OrderIds = [order.Id], Remark = "订单需求转采购计划" }));
        var plan = Assert.Single(plans);
        await ReadDataAsync<List<PurchasePlanDto>>(await client.PutAsJsonAsync(
            "/api/purchase-plans/supplier",
            new AssignPurchasePlanSupplierDto { PlanIds = [plan.Id], SupplierId = seed.SupplierId }));
        await ReadDataAsync<List<PurchasePlanDto>>(await client.PutAsJsonAsync(
            "/api/purchase-plans/purchaser",
            new AssignPurchasePlanPurchaserDto { PlanIds = [plan.Id], PurchaserId = seed.PurchaserId }));

        var purchaseOrders = await ReadDataAsync<List<PurchaseOrderDto>>(await client.PostAsJsonAsync(
            "/api/purchase-orders/generate-from-plans",
            new GeneratePurchaseOrdersFromPlansDto
            {
                PlanIds = [plan.Id],
                ReceiveTime = new DateTime(2026, 7, 7, 6, 0, 0, DateTimeKind.Utc),
                Remark = "采购计划生成采购单"
            }));
        var purchaseOrder = Assert.Single(purchaseOrders);
        var purchaseOrderDetail = Assert.Single(purchaseOrder.Details);
        await ReadDataAsync<PurchaseOrderDto>(await client.PostAsync(
            $"/api/purchase-orders/{purchaseOrder.Id}/complete", null));

        var stockIn = await ReadDataAsync<StockInOrderDto>(await client.PostAsJsonAsync(
            "/api/stock-in/purchase",
            new CreatePurchaseStockInDto
            {
                WareId = seed.WareId,
                PurchaseOrderId = purchaseOrder.Id,
                SupplierId = seed.SupplierId,
                PurchaserId = seed.PurchaserId,
                PurchasePattern = PurchasePattern.SupplierDirect,
                InTime = new DateTime(2026, 7, 7, 7, 0, 0, DateTimeKind.Utc),
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
                        ProductDate = new DateOnly(2026, 7, 6)
                    }
                ]
            }));
        var auditedStockIn = await ReadDataAsync<StockInOrderDto>(await client.PostAsJsonAsync(
            $"/api/stock-in/purchase/{stockIn.Id}/audit",
            new StockInAuditDto { Remark = "验收入库并生成供应商待结单据" }));
        var stockBatchId = Assert.IsType<Guid>(Assert.Single(auditedStockIn.Details).StockBatchId);

        // 采购入库审核后应自动生成金额为 19.2 的供应商待结单据。
        var supplierBills = await ReadDataAsync<PagedResult<SupplierBillDto>>(await client.GetAsync(
            $"/api/supplier-settlements/bills?current=1&size=10&pendingOnly=true&supplierId={seed.SupplierId}"));
        var supplierBill = Assert.Single(supplierBills.Records!);
        Assert.Equal(SupplierBillSourceType.PurchaseStockIn, supplierBill.SourceType);
        Assert.Equal(stockIn.Id, supplierBill.StockInOrderId);
        Assert.Equal(19.2m, supplierBill.PayableAmount);
        Assert.Equal(19.2m, supplierBill.PendingAmount);
        Assert.Equal(SupplierBillStatus.Pending, supplierBill.BillStatus);

        // 销售出库 -> 配送签收（整单验收）生成客户正向应收账单。
        var stockOut = await ReadDataAsync<StockOutOrderDto>(await client.PostAsJsonAsync(
            "/api/stock-out/sale",
            new CreateSaleStockOutDto
            {
                WareId = seed.WareId,
                SaleOrderId = order.Id,
                CustomerId = seed.CustomerId,
                OutTime = new DateTime(2026, 7, 7, 8, 0, 0, DateTimeKind.Utc),
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
        var stockOutDetail = Assert.Single(auditedStockOut.Details);

        var deliveryTask = await ReadDataAsync<DeliveryTaskDto>(await client.PostAsync(
            $"/api/delivery-tasks/generate/{stockOut.Id}", null));
        await ReadDataAsync<List<DeliveryTaskDto>>(await client.PutAsJsonAsync(
            "/api/delivery-tasks/driver",
            new AssignDeliveryDriverDto { TaskIds = [deliveryTask.Id], DriverId = seed.DriverId }));
        await ReadDataAsync<DeliveryTaskDto>(await client.PutAsync(
            $"/api/delivery-tasks/{deliveryTask.Id}/start", null));
        var signedReceipt = await ReadDataAsync<OrderReceiptDto>(await client.PutAsJsonAsync(
            $"/api/delivery-tasks/{deliveryTask.Id}/sign",
            new SignDeliveryTaskDto
            {
                SignerName = "李老师",
                Remark = "整单验收",
                Details =
                [
                    new SignDeliveryCheckDetailDto
                    {
                        StockOutDetailId = stockOutDetail.Id,
                        AcceptedBaseQuantity = 6m,
                        CheckStatus = OrderCustomerCheckStatus.Accepted
                    }
                ]
            }));
        Assert.Equal(order.Id, signedReceipt.SaleOrderId);

        // 整单验收后应生成金额为 27 的客户账单。
        var customerBills = await ReadDataAsync<PagedResult<CustomerBillDto>>(await client.GetAsync(
            $"/api/customer-settlements/bills?current=1&size=10&customerId={seed.CustomerId}"));
        var customerBill = Assert.Single(customerBills.Records!);
        Assert.Equal(order.Id, customerBill.SaleOrderId);
        Assert.Equal(27m, customerBill.OrderAmount);
        Assert.Equal(0m, customerBill.AfterSaleAdjustmentAmount);
        Assert.Equal(27m, customerBill.ReceivableAmount);
        Assert.Equal(CustomerBillStatus.Pending, customerBill.BillStatus);

        // 售后主链路：建单 -> 提交 -> 审核 -> 取货 -> 销售退货入库审核 -> 完成，冲减客户账单应收。
        var afterSale = await ReadDataAsync<AfterSaleDto>(await client.PostAsJsonAsync(
            "/api/after-sales",
            new CreateAfterSaleDto
            {
                SaleOrderId = order.Id,
                CustomerId = seed.CustomerId,
                Source = "第三阶段联调退货",
                ContactName = "李老师",
                ContactPhone = "13900000001",
                PickupAddress = "第三阶段联调学校食堂",
                Goods =
                [
                    new CreateAfterSaleGoodsDto
                    {
                        SaleOrderDetailId = orderDetail.Id,
                        ActualRefundQuantity = 2m,
                        AfterSaleType = AfterSaleType.ReturnAndRefund,
                        ReasonType = AfterSaleReasonType.QualityIssue,
                        HandleType = AfterSaleHandleType.GoodsDiscount
                    }
                ]
            }));
        await ReadDataAsync<AfterSaleDto>(await client.PostAsJsonAsync(
            $"/api/after-sales/{afterSale.Id}/submit",
            new AfterSaleActionDto { Remark = "提交售后审核" }));
        var approvedAfterSale = await ReadDataAsync<AfterSaleDto>(await client.PostAsJsonAsync(
            $"/api/after-sales/{afterSale.Id}/approve",
            new AfterSaleActionDto { Remark = "同意退货退款" }));
        var pickupTask = Assert.Single(approvedAfterSale.PickupTasks);

        var pickupTaskDetail = await ReadDataAsync<PickupTaskDto>(await client.GetAsync(
            $"/api/after-sales/pickup-tasks/{pickupTask.Id}"));
        Assert.Equal(pickupTask.Id, pickupTaskDetail.Id);
        Assert.Equal(afterSale.Id, pickupTaskDetail.AfterSaleId);
        Assert.Equal("第三阶段联调学校食堂", pickupTaskDetail.PickupAddress);

        await ReadDataAsync<PickupTaskDto>(await client.PutAsJsonAsync(
            $"/api/after-sales/pickup-tasks/{pickupTask.Id}/assign",
            new AssignPickupTaskDto
            {
                DriverId = seed.DriverId,
                PlannedPickupTime = new DateTime(2026, 7, 8, 9, 0, 0, DateTimeKind.Utc)
            }));
        await ReadDataAsync<PickupTaskDto>(await client.PostAsync(
            $"/api/after-sales/pickup-tasks/{pickupTask.Id}/start", null));
        await ReadDataAsync<PickupTaskDto>(await client.PostAsync(
            $"/api/after-sales/pickup-tasks/{pickupTask.Id}/complete", null));

        var salesReturn = await ReadDataAsync<StockInOrderDto>(await client.PostAsJsonAsync(
            "/api/stock-in/sales-return",
            new CreateSalesReturnStockInDto
            {
                AfterSaleId = afterSale.Id,
                WareId = seed.WareId,
                CustomerId = seed.CustomerId,
                InTime = new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc),
                Remark = "售后取货入库",
                Details =
                [
                    new CreateStockInDetailDto
                    {
                        PickupTaskId = pickupTask.Id,
                        GoodsId = seed.GoodsId,
                        GoodsUnitId = seed.GoodsUnitId,
                        Quantity = 2m,
                        UnitPrice = 4.5m
                    }
                ]
            }));
        await ReadDataAsync<StockInOrderDto>(await client.PostAsJsonAsync(
            $"/api/stock-in/sales-return/{salesReturn.Id}/audit",
            new StockInAuditDto { Remark = "退货质检通过并入库" }));
        var completedAfterSale = await ReadDataAsync<AfterSaleDto>(await client.PostAsync(
            $"/api/after-sales/{afterSale.Id}/complete", null));
        Assert.Equal(AfterSaleStatus.Completed, completedAfterSale.AfterStatus);

        // 售后完成冲减客户账单，应收由 27 降为 18。
        var adjustedBills = await ReadDataAsync<PagedResult<CustomerBillDto>>(await client.GetAsync(
            $"/api/customer-settlements/bills?current=1&size=10&pendingOnly=true&customerId={seed.CustomerId}"));
        var adjustedBill = Assert.Single(adjustedBills.Records!);
        Assert.Equal(-9m, adjustedBill.AfterSaleAdjustmentAmount);
        Assert.Equal(18m, adjustedBill.ReceivableAmount);
        Assert.Equal(18m, adjustedBill.PendingAmount);

        // 客户结款：部分结款 + 优惠 -> 作废回滚 -> 全部结款。
        var partialCustomerSettlement = await ReadDataAsync<CustomerSettlementDto>(await client.PostAsJsonAsync(
            "/api/customer-settlements",
            new CreateCustomerSettlementDto
            {
                SettlementDate = new DateTime(2026, 7, 8, 11, 0, 0, DateTimeKind.Utc),
                SerialNo = "BANK-P3-CUST-001",
                Remark = "客户部分转账并给予尾差优惠",
                Details =
                [
                    new CreateCustomerSettlementDetailDto
                    {
                        CustomerBillId = adjustedBill.Id,
                        PaymentAmount = 10m,
                        DiscountAmount = 3m
                    }
                ]
            }));
        Assert.Equal(CustomerSettlementStatus.PartiallySettled, partialCustomerSettlement.SettlementStatus);
        Assert.Equal(18m, partialCustomerSettlement.ShouldAmount);
        Assert.Equal(13m, partialCustomerSettlement.AppliedAmount);
        Assert.Equal(5m, partialCustomerSettlement.RemainingAmount);

        var voidedCustomerSettlement = await ReadDataAsync<CustomerSettlementDto>(await SendJsonAsync(
            client,
            HttpMethod.Delete,
            $"/api/customer-settlements/{partialCustomerSettlement.Id}/void",
            new VoidCustomerSettlementDto { Remark = "流水录错，作废后重新结款" }));
        Assert.Equal(CustomerSettlementStatus.Voided, voidedCustomerSettlement.SettlementStatus);

        var billAfterVoid = await ReadDataAsync<PagedResult<CustomerBillDto>>(await client.GetAsync(
            $"/api/customer-settlements/bills?current=1&size=10&pendingOnly=true&customerId={seed.CustomerId}"));
        Assert.Equal(CustomerBillStatus.Pending, Assert.Single(billAfterVoid.Records!).BillStatus);

        var fullCustomerSettlement = await ReadDataAsync<CustomerSettlementDto>(await client.PostAsJsonAsync(
            "/api/customer-settlements",
            new CreateCustomerSettlementDto
            {
                SerialNo = "BANK-P3-CUST-002",
                Remark = "客户全额结清",
                Details =
                [
                    new CreateCustomerSettlementDetailDto
                    {
                        CustomerBillId = adjustedBill.Id,
                        PaymentAmount = 18m
                    }
                ]
            }));
        Assert.Equal(CustomerSettlementStatus.Settled, fullCustomerSettlement.SettlementStatus);
        Assert.Equal(0m, fullCustomerSettlement.RemainingAmount);

        // 供应商结算：部分结款 + 优惠 -> 作废回滚 -> 全部结款。
        var partialSupplierSettlement = await ReadDataAsync<SupplierSettlementDto>(await client.PostAsJsonAsync(
            "/api/supplier-settlements",
            new CreateSupplierSettlementDto
            {
                SettlementDate = new DateTime(2026, 7, 8, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "BANK-P3-SUP-001",
                Remark = "向供应商部分付款并给予尾差优惠",
                Details =
                [
                    new CreateSupplierSettlementDetailDto
                    {
                        SupplierBillId = supplierBill.Id,
                        PaymentAmount = 10m,
                        DiscountAmount = 2m
                    }
                ]
            }));
        Assert.Equal(SupplierSettlementStatus.PartiallySettled, partialSupplierSettlement.SettlementStatus);
        Assert.Equal(19.2m, partialSupplierSettlement.ShouldAmount);
        Assert.Equal(12m, partialSupplierSettlement.AppliedAmount);
        Assert.Equal(7.2m, partialSupplierSettlement.RemainingAmount);

        var voidedSupplierSettlement = await ReadDataAsync<SupplierSettlementDto>(await SendJsonAsync(
            client,
            HttpMethod.Delete,
            $"/api/supplier-settlements/{partialSupplierSettlement.Id}/void",
            new VoidSupplierSettlementDto { Remark = "付款流水录错，作废后重新结算" }));
        Assert.Equal(SupplierSettlementStatus.Voided, voidedSupplierSettlement.SettlementStatus);

        var supplierBillAfterVoid = await ReadDataAsync<PagedResult<SupplierBillDto>>(await client.GetAsync(
            $"/api/supplier-settlements/bills?current=1&size=10&pendingOnly=true&supplierId={seed.SupplierId}"));
        Assert.Equal(SupplierBillStatus.Pending, Assert.Single(supplierBillAfterVoid.Records!).BillStatus);

        var fullSupplierSettlement = await ReadDataAsync<SupplierSettlementDto>(await client.PostAsJsonAsync(
            "/api/supplier-settlements",
            new CreateSupplierSettlementDto
            {
                SerialNo = "BANK-P3-SUP-002",
                Remark = "向供应商全额付款",
                Details =
                [
                    new CreateSupplierSettlementDetailDto
                    {
                        SupplierBillId = supplierBill.Id,
                        PaymentAmount = 19.2m
                    }
                ]
            }));
        Assert.Equal(SupplierSettlementStatus.Settled, fullSupplierSettlement.SettlementStatus);
        Assert.Equal(0m, fullSupplierSettlement.RemainingAmount);

        // 数据库侧核对账单和待结单据的最终已结金额与状态。
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedCustomerBill = await context.CustomerBills.AsNoTracking()
            .SingleAsync(x => x.Id == adjustedBill.Id);
        Assert.Equal(18m, persistedCustomerBill.SettledAmount);
        Assert.Equal(CustomerBillStatus.Settled, persistedCustomerBill.BillStatus);
        var persistedSupplierBill = await context.SupplierBills.AsNoTracking()
            .SingleAsync(x => x.Id == supplierBill.Id);
        Assert.Equal(19.2m, persistedSupplierBill.SettledAmount);
        Assert.Equal(SupplierBillStatus.Settled, persistedSupplierBill.BillStatus);
        Assert.Equal(2, await context.CustomerSettlements.CountAsync());
        Assert.Equal(2, await context.SupplierSettlements.CountAsync());
    }

    private static Task<HttpResponseMessage> SendJsonAsync<TBody>(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        TBody body)
    {
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(body)
        };
        return client.SendAsync(request);
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(result);
        return Assert.IsType<T>(result.Data);
    }

    private sealed class FinanceAfterSaleFlowApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"finance-after-sale-flow-{Guid.NewGuid():N}";

        public async Task<CatalogSeed> SeedCatalogAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ware = new Ware { Id = Guid.NewGuid(), Name = "第三阶段联调仓", Code = "P3_FLOW_WARE" };
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = "第三阶段联调学校",
                Code = "P3_FLOW_CUSTOMER",
                DefaultWareId = ware.Id
            };
            var goodsType = new GoodsType { Id = Guid.NewGuid(), Name = "联调蔬菜", Code = "P3_FLOW_VEGETABLE" };
            var goods = new GoodsEntity
            {
                Id = Guid.NewGuid(),
                Name = "联调白菜",
                Code = "P3_FLOW_CABBAGE",
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
                Name = "第三阶段联调供应商",
                Code = "P3_FLOW_SUPPLIER",
                ContactName = "赵经理",
                ContactPhone = "13800000001"
            };
            var purchaser = new Purchaser
            {
                Id = Guid.NewGuid(),
                Name = "第三阶段联调采购员",
                Code = "P3_FLOW_PURCHASER"
            };
            var carrier = new Carrier { Id = Guid.NewGuid(), Name = "第三阶段联调承运商", Code = "P3_FLOW_CARRIER" };
            var driver = new Driver
            {
                Id = Guid.NewGuid(),
                Name = "第三阶段联调司机",
                Code = "P3_FLOW_DRIVER",
                Phone = "13700000001",
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
        public const string AuthenticationScheme = "FinanceAfterSaleFlowIntegrationTest";
        public const string PermissionsHeader = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(PermissionsHeader, out var values))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "10000000-0000-0000-0000-000000000030"),
                new(ClaimTypes.Name, "finance-after-sale-flow-test")
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
        Guid DriverId);
}
