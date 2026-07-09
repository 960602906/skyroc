using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.DTOs.Orders;
using Application.DTOs.Purchases;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
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
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Purchases;

public class ProcurementFlowApiIntegrationTests
{
    [Fact]
    public async Task ApprovedOrder_CreatesPlanAndCompletedPurchaseOrder_ThroughHttpApi()
    {
        using var factory = new ProcurementFlowApiFactory();
        var seed = await factory.SeedCatalogAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.All);

        var createOrderResponse = await client.PostAsJsonAsync("/api/orders", new CreateSaleOrderDto
        {
            CustomerId = seed.CustomerId,
            OrderDate = new DateTime(2026, 7, 3, 8, 0, 0, DateTimeKind.Utc),
            ReceiveDate = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            Remark = "采购链路集成测试",
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
        });
        var order = await ReadDataAsync<SaleOrderDto>(createOrderResponse);

        var approveResponse = await client.PostAsJsonAsync(
            $"/api/orders/{order.Id}/approve",
            new SaleOrderAuditDto { Remark = "审核通过并进入采购" });
        var approvedOrder = await ReadDataAsync<SaleOrderDto>(approveResponse);
        Assert.Equal(SaleOrderStatus.SortingPending, approvedOrder.OrderStatus);

        var generatePlanResponse = await client.PostAsJsonAsync(
            "/api/purchase-plans/generate",
            new GeneratePurchasePlanFromOrdersDto
            {
                OrderIds = [order.Id],
                Remark = "从审核订单生成"
            });
        var plans = await ReadDataAsync<List<PurchasePlanDto>>(generatePlanResponse);
        var plan = Assert.Single(plans);
        var planDetail = Assert.Single(plan.Details);
        var orderRelation = Assert.Single(planDetail.OrderRelations);
        Assert.Equal(order.Id, orderRelation.SaleOrderId);
        Assert.Equal(order.Details[0].Id, orderRelation.SaleOrderDetailId);
        Assert.Equal(6m, planDetail.RequiredQuantity);

        var assignSupplierResponse = await client.PutAsJsonAsync(
            "/api/purchase-plans/supplier",
            new AssignPurchasePlanSupplierDto
            {
                PlanIds = [plan.Id],
                SupplierId = seed.SupplierId
            });
        await ReadDataAsync<List<PurchasePlanDto>>(assignSupplierResponse);

        var assignPurchaserResponse = await client.PutAsJsonAsync(
            "/api/purchase-plans/purchaser",
            new AssignPurchasePlanPurchaserDto
            {
                PlanIds = [plan.Id],
                PurchaserId = seed.PurchaserId
            });
        await ReadDataAsync<List<PurchasePlanDto>>(assignPurchaserResponse);

        var generatePurchaseOrderResponse = await client.PostAsJsonAsync(
            "/api/purchase-orders/generate-from-plans",
            new GeneratePurchaseOrdersFromPlansDto
            {
                PlanIds = [plan.Id],
                ReceiveTime = new DateTime(2026, 7, 6, 8, 0, 0, DateTimeKind.Utc),
                Remark = "采购计划转采购单"
            });
        var purchaseOrders = await ReadDataAsync<List<PurchaseOrderDto>>(generatePurchaseOrderResponse);
        var purchaseOrder = Assert.Single(purchaseOrders);
        var purchaseOrderDetail = Assert.Single(purchaseOrder.Details);
        var planRelation = Assert.Single(purchaseOrderDetail.PlanRelations);
        Assert.Equal(PurchaseOrderStatus.Draft, purchaseOrder.BusinessStatus);
        Assert.Equal(seed.SupplierId, purchaseOrder.SupplierId);
        Assert.Equal(seed.PurchaserId, purchaseOrder.PurchaserId);
        Assert.Equal(plan.Id, planRelation.PurchasePlanId);
        Assert.Equal(6m, planRelation.AllocatedQuantity);

        var orderAfterPlanResponse = await client.GetAsync($"/api/orders/{order.Id}");
        var orderAfterPlan = await ReadDataAsync<SaleOrderDto>(orderAfterPlanResponse);
        Assert.True(orderAfterPlan.HasPurchasePlan);
        Assert.All(orderAfterPlan.Details, detail => Assert.True(detail.HasPurchasePlan));

        var planAfterPurchaseResponse = await client.GetAsync($"/api/purchase-plans/{plan.Id}");
        var planAfterPurchase = await ReadDataAsync<PurchasePlanDto>(planAfterPurchaseResponse);
        Assert.Equal(PurchasePlanStatus.Generated, planAfterPurchase.PurchaseStatus);
        Assert.Equal(6m, Assert.Single(planAfterPurchase.Details).PurchasedQuantity);

        var completeResponse = await client.PostAsync(
            $"/api/purchase-orders/{purchaseOrder.Id}/complete",
            null);
        var completedPurchaseOrder = await ReadDataAsync<PurchaseOrderDto>(completeResponse);
        Assert.Equal(PurchaseOrderStatus.Completed, completedPurchaseOrder.BusinessStatus);
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(result);
        return Assert.IsType<T>(result.Data);
    }

    private sealed class ProcurementFlowApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"procurement-flow-{Guid.NewGuid():N}";

        public async Task<CatalogSeed> SeedCatalogAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = "采购链路学校客户",
                Code = "FLOW_CUSTOMER"
            };
            var goodsType = new GoodsType
            {
                Id = Guid.NewGuid(),
                Name = "蔬菜",
                Code = "FLOW_VEGETABLE"
            };
            var goods = new GoodsEntity
            {
                Id = Guid.NewGuid(),
                Name = "番茄",
                Code = "FLOW_TOMATO",
                GoodsTypeId = goodsType.Id,
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
                Name = "采购链路直供商",
                Code = "FLOW_SUPPLIER",
                ContactName = "王经理",
                ContactPhone = "13800000000"
            };
            var purchaser = new Purchaser
            {
                Id = Guid.NewGuid(),
                Name = "采购链路采购员",
                Code = "FLOW_PURCHASER"
            };

            await context.Customers.AddAsync(customer);
            await context.GoodsTypes.AddAsync(goodsType);
            await context.Goods.AddAsync(goods);
            await context.GoodsUnits.AddAsync(goodsUnit);
            await context.Suppliers.AddAsync(supplier);
            await context.Purchasers.AddAsync(purchaser);
            await context.SaveChangesAsync();
            return new CatalogSeed(
                customer.Id,
                goods.Id,
                goodsUnit.Id,
                supplier.Id,
                purchaser.Id);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=localhost;Database=skyroc_tests;Username=test;Password=test");
            builder.UseSetting("Redis:Enabled", "false");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
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
        public const string AuthenticationScheme = "ProcurementFlowIntegrationTest";
        public const string PermissionsHeader = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(PermissionsHeader, out var values))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "10000000-0000-0000-0000-000000000011"),
                new(ClaimTypes.Name, "procurement-flow-test")
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
        Guid SupplierId,
        Guid PurchaserId);
}
