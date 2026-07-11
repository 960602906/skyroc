using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Application.DTOs.Storage;
using Domain.Entities.Goods;
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
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Storage;

public class StockInApiIntegrationTests
{
    [Fact]
    public async Task StockQueryEndpoints_ReturnOverviewBatchesAndLedger_ThroughHttpApi()
    {
        using var factory = new StockInApiFactory();
        var seed = await factory.SeedCatalogAsync();
        var querySeed = await factory.SeedStockQueryAsync(seed);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.Business.Storage.Read);

        var overviewResponse = await client.GetAsync(
            $"/api/stock/overview?current=1&size=10&wareId={seed.WareId}&goodsId={seed.GoodsId}");
        var overviewPage = await ReadDataAsync<PagedResult<StockOverviewDto>>(overviewResponse);
        var overview = Assert.Single(overviewPage.Records!);
        Assert.Equal(1, overviewPage.Total);
        Assert.Equal(15m, overview.CurrentQuantity);
        Assert.Equal(12m, overview.AvailableQuantity);
        Assert.Equal(3m, overview.OccupiedQuantity);
        Assert.Equal(5.3333m, overview.WeightedUnitCost);
        Assert.Equal(80m, overview.StockValue);

        var batchResponse = await client.GetAsync(
            $"/api/stock/batches?current=1&size=1&goodsId={seed.GoodsId}");
        var batchPage = await ReadDataAsync<PagedResult<StockBatchDto>>(batchResponse);
        Assert.Equal(2, batchPage.Total);
        var firstExpiringBatch = Assert.Single(batchPage.Records!);
        Assert.Equal(querySeed.FirstBatchId, firstExpiringBatch.Id);
        Assert.Equal(3m, firstExpiringBatch.OccupiedQuantity);
        Assert.Equal(40m, firstExpiringBatch.StockValue);

        var ledgerResponse = await client.GetAsync(
            $"/api/stock/ledgers?current=1&size=10&stockBatchId={querySeed.FirstBatchId}&direction=2");
        var ledgerPage = await ReadDataAsync<PagedResult<StockLedgerDto>>(ledgerResponse);
        var ledger = Assert.Single(ledgerPage.Records!);
        Assert.Equal(StockLedgerDirection.Decrease, ledger.Direction);
        Assert.Equal(StockLedgerSourceType.OtherOutbound, ledger.SourceType);
        Assert.Equal(-2m, ledger.SignedChangeQuantity);
        Assert.Equal(10m, ledger.BalanceQuantity);
    }

    [Fact]
    public async Task StockQueryEndpoints_RequireAuthenticationAndStorageReadPermission()
    {
        using var factory = new StockInApiFactory();
        using var anonymousClient = factory.CreateClient();

        var anonymousResponse = await anonymousClient.GetAsync("/api/stock/overview?current=1&size=10");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var forbiddenClient = factory.CreateClient();
        forbiddenClient.DefaultRequestHeaders.Add(
            TestAuthHandler.PermissionsHeader,
            PermissionCodes.Business.Goods.Read);
        var forbiddenResponse = await forbiddenClient.GetAsync("/api/stock/batches?current=1&size=10");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using var allowedClient = factory.CreateClient();
        allowedClient.DefaultRequestHeaders.Add(
            TestAuthHandler.PermissionsHeader,
            PermissionCodes.Business.Storage.Read);
        var allowedResponse = await allowedClient.GetAsync("/api/stock/ledgers?current=1&size=10");
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
    }

    [Fact]
    public async Task Swagger_DocumentsStockQueryRoutesAndReadPermission()
    {
        using var factory = new StockInApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var paths = document.RootElement.GetProperty("paths");
        foreach (var path in new[] { "/api/stock/overview", "/api/stock/batches", "/api/stock/ledgers" })
        {
            var operation = paths.GetProperty(path).GetProperty("get");
            Assert.Contains(
                PermissionCodes.Business.Storage.Read,
                operation.GetProperty("description").GetString());
            Assert.Equal("Bearer", operation.GetProperty("security")[0].EnumerateObject().Single().Name);
            Assert.True(operation.GetProperty("responses").TryGetProperty("401", out _));
            Assert.True(operation.GetProperty("responses").TryGetProperty("403", out _));
        }
    }

    [Fact]
    public async Task PurchaseInbound_AuditIncreasesStock_ReverseRollsBack_ThroughHttpApi()
    {
        using var factory = new StockInApiFactory();
        var seed = await factory.SeedCatalogAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.All);

        var createResponse = await client.PostAsJsonAsync("/api/stock-in/purchase", new CreatePurchaseStockInDto
        {
            WareId = seed.WareId,
            SupplierId = seed.SupplierId,
            PurchaserId = seed.PurchaserId,
            PurchasePattern = PurchasePattern.SupplierDirect,
            InTime = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            Remark = "入库集成测试",
            Details =
            [
                new CreateStockInDetailDto
                {
                    GoodsId = seed.GoodsId,
                    GoodsUnitId = seed.GoodsUnitId,
                    Quantity = 20m,
                    UnitPrice = 4m,
                    BatchNo = "BATCH-IT-01",
                    ProductDate = new DateOnly(2026, 7, 1)
                }
            ]
        });
        var created = await ReadDataAsync<StockInOrderDto>(createResponse);
        Assert.Equal(StockDocumentStatus.Draft, created.BusinessStatus);

        var auditResponse = await client.PostAsJsonAsync(
            $"/api/stock-in/purchase/{created.Id}/audit",
            new StockInAuditDto { Remark = "审核入库" });
        var audited = await ReadDataAsync<StockInOrderDto>(auditResponse);
        Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
        Assert.NotNull(Assert.Single(audited.Details).StockBatchId);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var batch = await context.StockBatches.SingleAsync();
            Assert.Equal(20m, batch.CurrentQuantity);
            Assert.Equal(20m, batch.AvailableQuantity);
            Assert.Equal(4m, batch.UnitCost);
            Assert.Equal(1, await context.StockLedgers.CountAsync());
        }

        var reverseResponse = await client.PostAsJsonAsync(
            $"/api/stock-in/purchase/{created.Id}/reverse-audit",
            new StockInAuditDto { Remark = "反审核回滚" });
        var reversed = await ReadDataAsync<StockInOrderDto>(reverseResponse);
        Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var batch = await context.StockBatches.SingleAsync();
            Assert.Equal(0m, batch.CurrentQuantity);
            Assert.Equal(0m, batch.AvailableQuantity);
            Assert.Equal(2, await context.StockLedgers.CountAsync());
        }
    }

    [Fact]
    public async Task StockInEndpoints_Return403_WhenPermissionMissing()
    {
        using var factory = new StockInApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.Business.Storage.Read);

        var response = await client.PostAsJsonAsync("/api/stock-in/purchase", new CreatePurchaseStockInDto
        {
            WareId = Guid.NewGuid(),
            InTime = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            Details = []
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOutbound_AuditDecreasesStock_ReverseRestores_ThroughHttpApi()
    {
        using var factory = new StockInApiFactory();
        var seed = await factory.SeedCatalogAsync();
        var stockBatchId = await factory.SeedStockBatchAsync(seed);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.All);

        var createResponse = await client.PostAsJsonAsync("/api/stock-out/other", new CreateOtherStockOutDto
        {
            WareId = seed.WareId,
            OutTime = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            Remark = "其他出库集成测试",
            Details =
            [
                new CreateStockOutDetailDto
                {
                    StockBatchId = stockBatchId,
                    GoodsUnitId = seed.GoodsUnitId,
                    Quantity = 6m,
                    UnitPrice = 5m
                }
            ]
        });
        var created = await ReadDataAsync<StockOutOrderDto>(createResponse);
        Assert.Equal(StockDocumentStatus.Draft, created.BusinessStatus);

        var auditResponse = await client.PostAsJsonAsync(
            $"/api/stock-out/other/{created.Id}/audit",
            new StockOutAuditDto { Remark = "审核出库" });
        var audited = await ReadDataAsync<StockOutOrderDto>(auditResponse);
        Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var batch = await context.StockBatches.SingleAsync();
            Assert.Equal(14m, batch.CurrentQuantity);
            Assert.Equal(14m, batch.AvailableQuantity);
            var ledger = await context.StockLedgers.SingleAsync();
            Assert.Equal(StockLedgerDirection.Decrease, ledger.Direction);
            Assert.Equal(StockLedgerSourceType.OtherOutbound, ledger.SourceType);
        }

        var reverseResponse = await client.PostAsJsonAsync(
            $"/api/stock-out/other/{created.Id}/reverse-audit",
            new StockOutAuditDto { Remark = "反审核恢复" });
        var reversed = await ReadDataAsync<StockOutOrderDto>(reverseResponse);
        Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);

        using var finalScope = factory.Services.CreateScope();
        var finalContext = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var restoredBatch = await finalContext.StockBatches.SingleAsync();
        Assert.Equal(20m, restoredBatch.CurrentQuantity);
        Assert.Equal(20m, restoredBatch.AvailableQuantity);
        Assert.Equal(2, await finalContext.StockLedgers.CountAsync());
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(result);
        return Assert.IsType<T>(result.Data);
    }

    private sealed class StockInApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"stock-in-{Guid.NewGuid():N}";

        public async Task<CatalogSeed> SeedCatalogAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var goodsType = new GoodsType { Id = Guid.NewGuid(), Name = "蔬菜", Code = "IT_VEGETABLE" };
            var goods = new GoodsEntity
            {
                Id = Guid.NewGuid(),
                Name = "番茄",
                Code = "IT_TOMATO",
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
            var ware = new Ware { Id = Guid.NewGuid(), Name = "入库集成仓", Code = "IT_WARE" };
            var supplier = new Supplier { Id = Guid.NewGuid(), Name = "入库集成供应商", Code = "IT_SUPPLIER" };
            var purchaser = new Purchaser { Id = Guid.NewGuid(), Name = "入库集成采购员", Code = "IT_PURCHASER" };

            await context.GoodsTypes.AddAsync(goodsType);
            await context.Goods.AddAsync(goods);
            await context.GoodsUnits.AddAsync(goodsUnit);
            await context.Wares.AddAsync(ware);
            await context.Suppliers.AddAsync(supplier);
            await context.Purchasers.AddAsync(purchaser);
            await context.SaveChangesAsync();
            return new CatalogSeed(goods.Id, goodsUnit.Id, ware.Id, supplier.Id, purchaser.Id);
        }

        public async Task<Guid> SeedStockBatchAsync(CatalogSeed seed)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stockBatch = new StockBatch
            {
                Id = Guid.NewGuid(),
                WareId = seed.WareId,
                GoodsId = seed.GoodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "IT_TOMATO",
                BatchNo = "IT-BATCH-OUT-001",
                BaseUnitId = seed.GoodsUnitId,
                BaseUnitNameSnapshot = "千克",
                CurrentQuantity = 20m,
                AvailableQuantity = 20m,
                UnitCost = 5m
            };
            await context.StockBatches.AddAsync(stockBatch);
            await context.SaveChangesAsync();
            return stockBatch.Id;
        }

        public async Task<StockQuerySeed> SeedStockQueryAsync(CatalogSeed seed)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var firstBatch = new StockBatch
            {
                Id = Guid.NewGuid(),
                WareId = seed.WareId,
                GoodsId = seed.GoodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "IT_TOMATO",
                BatchNo = "IT-QUERY-001",
                BaseUnitId = seed.GoodsUnitId,
                BaseUnitNameSnapshot = "千克",
                CurrentQuantity = 10m,
                AvailableQuantity = 7m,
                UnitCost = 4m,
                ExpireDate = new DateOnly(2026, 7, 10),
                LastMovementTime = new DateTime(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc)
            };
            var secondBatch = new StockBatch
            {
                Id = Guid.NewGuid(),
                WareId = seed.WareId,
                GoodsId = seed.GoodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "IT_TOMATO",
                BatchNo = "IT-QUERY-002",
                BaseUnitId = seed.GoodsUnitId,
                BaseUnitNameSnapshot = "千克",
                CurrentQuantity = 5m,
                AvailableQuantity = 5m,
                UnitCost = 8m,
                ExpireDate = new DateOnly(2026, 7, 20),
                LastMovementTime = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc)
            };
            var sourceOrderId = Guid.NewGuid();
            var inboundLedger = new StockLedger
            {
                Id = Guid.NewGuid(),
                StockBatchId = firstBatch.Id,
                WareId = seed.WareId,
                WareNameSnapshot = "入库集成仓",
                GoodsId = seed.GoodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "IT_TOMATO",
                BatchNoSnapshot = firstBatch.BatchNo,
                BaseUnitNameSnapshot = "千克",
                Direction = StockLedgerDirection.Increase,
                SourceType = StockLedgerSourceType.PurchaseInbound,
                SourceOrderId = sourceOrderId,
                SourceDetailId = Guid.NewGuid(),
                ChangeQuantity = 12m,
                BalanceQuantity = 12m,
                UnitCost = 4m,
                TotalCost = 48m,
                OccurredTime = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc)
            };
            var outboundLedger = new StockLedger
            {
                Id = Guid.NewGuid(),
                StockBatchId = firstBatch.Id,
                WareId = seed.WareId,
                WareNameSnapshot = "入库集成仓",
                GoodsId = seed.GoodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "IT_TOMATO",
                BatchNoSnapshot = firstBatch.BatchNo,
                BaseUnitNameSnapshot = "千克",
                Direction = StockLedgerDirection.Decrease,
                SourceType = StockLedgerSourceType.OtherOutbound,
                SourceOrderId = Guid.NewGuid(),
                SourceDetailId = Guid.NewGuid(),
                ChangeQuantity = 2m,
                BalanceQuantity = 10m,
                UnitCost = 4m,
                TotalCost = 8m,
                OccurredTime = new DateTime(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc)
            };

            await context.StockBatches.AddRangeAsync(firstBatch, secondBatch);
            await context.StockLedgers.AddRangeAsync(inboundLedger, outboundLedger);
            await context.SaveChangesAsync();
            return new StockQuerySeed(firstBatch.Id);
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
        public const string AuthenticationScheme = "StockInIntegrationTest";
        public const string PermissionsHeader = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(PermissionsHeader, out var values))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "10000000-0000-0000-0000-000000000022"),
                new(ClaimTypes.Name, "stock-in-test")
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
        Guid GoodsId,
        Guid GoodsUnitId,
        Guid WareId,
        Guid SupplierId,
        Guid PurchaserId);

    private sealed record StockQuerySeed(Guid FirstBatchId);
}
