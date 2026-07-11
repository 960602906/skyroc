using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Application.DTOs.Purchases;
using Application.interfaces;
using Application.QueryParameters;
using Domain.Entities.Purchases;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Purchases;

public class PurchasePlanApiIntegrationTests
{
    private static readonly Guid GoodsId = Guid.Parse("30000000-0000-0000-0000-000000000009");
    private static readonly Guid PurchaseUnitId = Guid.Parse("40000000-0000-0000-0000-000000000009");

    [Fact]
    public async Task PurchasePlanEndpoints_RequireAuthenticationAndExpectedPermission()
    {
        using var factory = new PurchasePlanApiFactory();
        using var anonymousClient = factory.CreateClient();

        var anonymousResponse = await anonymousClient.GetAsync("/api/purchase-plans/list?current=1&size=10");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var readOnlyClient = factory.CreateClient();
        readOnlyClient.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.Business.Purchases.Read);

        var readResponse = await readOnlyClient.GetAsync("/api/purchase-plans/list?current=1&size=10");
        var createResponse = await readOnlyClient.PostAsJsonAsync("/api/purchase-plans", CreateRequest());
        var updateResponse = await readOnlyClient.PutAsJsonAsync(
            "/api/purchase-plans/supplier",
            new AssignPurchasePlanSupplierDto { PlanIds = [Guid.NewGuid()] });

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task PurchasePlanCreateAndGenerate_SucceedWithCreatePermission()
    {
        using var factory = new PurchasePlanApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.All);

        var createResponse = await client.PostAsJsonAsync("/api/purchase-plans", CreateRequest());
        var created = await ReadDataAsync<PurchasePlanDto>(createResponse);
        Assert.StartsWith("PP", created.PlanNo);
        Assert.Single(created.Details);

        var listResponse = await client.GetAsync("/api/purchase-plans/list?current=1&size=10");
        var page = await ReadDataAsync<PagedResult<PurchasePlanDto>>(listResponse);
        Assert.Equal(created.Id, Assert.Single(page.Records!).Id);

        var detailResponse = await client.GetAsync($"/api/purchase-plans/{created.Id}");
        var detail = await ReadDataAsync<PurchasePlanDto>(detailResponse);
        Assert.Equal(created.PlanNo, detail.PlanNo);

        var generateResponse = await client.PostAsJsonAsync(
            "/api/purchase-plans/generate",
            new GeneratePurchasePlanFromOrdersDto { OrderIds = [Guid.NewGuid()] });
        var generated = await ReadDataAsync<List<PurchasePlanDto>>(generateResponse);
        Assert.Single(generated);

        var assignResponse = await client.PutAsJsonAsync(
            "/api/purchase-plans/supplier",
            new AssignPurchasePlanSupplierDto { PlanIds = [created.Id], SupplierId = Guid.NewGuid() });
        var assigned = await ReadDataAsync<List<PurchasePlanDto>>(assignResponse);
        Assert.Equal(created.Id, Assert.Single(assigned).Id);
    }

    [Fact]
    public async Task Swagger_DocumentsPurchaseRoutesAndPermissions()
    {
        using var factory = new PurchasePlanApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var paths = document.RootElement.GetProperty("paths");
        var listOperation = paths.GetProperty("/api/purchase-plans/list").GetProperty("get");
        var generateOperation = paths.GetProperty("/api/purchase-plans/generate").GetProperty("post");
        var mergeOperation = paths.GetProperty("/api/purchase-plans/merge").GetProperty("post");
        var splitOrdersOperation = paths.GetProperty("/api/purchase-plans/split/orders").GetProperty("post");
        var purchaseOrderListOperation = paths.GetProperty("/api/purchase-orders/list").GetProperty("get");
        var purchaseOrderGenerateOperation = paths.GetProperty("/api/purchase-orders/generate-from-plans").GetProperty("post");
        var purchaseOrderCompleteOperation = paths.GetProperty("/api/purchase-orders/{id}/complete").GetProperty("post");
        var purchaseOrderCancelOperation = paths.GetProperty("/api/purchase-orders/{id}/cancel").GetProperty("post");
        var purchaseOrderDeleteOperation = paths.GetProperty("/api/purchase-orders/{id}").GetProperty("delete");

        Assert.Contains(PermissionCodes.Business.Purchases.Read, listOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Purchases.Create, generateOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Purchases.Update, mergeOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Purchases.Update, splitOrdersOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Purchases.Read, purchaseOrderListOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Purchases.Create, purchaseOrderGenerateOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Purchases.Update, purchaseOrderCompleteOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Purchases.Update, purchaseOrderCancelOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Purchases.Delete, purchaseOrderDeleteOperation.GetProperty("description").GetString());
        Assert.Equal("Bearer", listOperation.GetProperty("security")[0].EnumerateObject().Single().Name);
        Assert.Equal("Bearer", purchaseOrderListOperation.GetProperty("security")[0].EnumerateObject().Single().Name);
        Assert.True(listOperation.GetProperty("responses").TryGetProperty("401", out _));
        Assert.True(listOperation.GetProperty("responses").TryGetProperty("403", out _));
        Assert.True(purchaseOrderListOperation.GetProperty("responses").TryGetProperty("401", out _));
        Assert.True(purchaseOrderListOperation.GetProperty("responses").TryGetProperty("403", out _));
    }

    private static CreatePurchasePlanDto CreateRequest()
    {
        return new CreatePurchasePlanDto
        {
            PlanDate = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            PurchasePattern = PurchasePattern.SupplierDirect,
            Remark = "联调采购计划",
            Details =
            [
                new CreatePurchasePlanDetailDto
                {
                    GoodsId = GoodsId,
                    PurchaseUnitId = PurchaseUnitId,
                    PlannedQuantity = 6m
                }
            ]
        };
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(result);
        return Assert.IsType<T>(result.Data);
    }

    private sealed class PurchasePlanApiFactory : WebApplicationFactory<Program>
    {
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
                services.RemoveAll<IPurchasePlanService>();
                services.AddSingleton<IPurchasePlanService, InMemoryPurchasePlanService>();
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
        public const string AuthenticationScheme = "PurchasePlanIntegrationTest";
        public const string PermissionsHeader = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(PermissionsHeader, out var values))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "10000000-0000-0000-0000-000000000009"),
                new(ClaimTypes.Name, "purchase-api-test")
            };
            claims.AddRange(values.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(permission => new Claim(AuthConstants.PermissionClaimType, permission)));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationScheme));
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, AuthenticationScheme)));
        }
    }

    private sealed class InMemoryPurchasePlanService : IPurchasePlanService
    {
        private readonly Dictionary<Guid, PurchasePlanDto> _plans = [];

        public Task<PagedResult<PurchasePlanDto>> GetPagedAsync(PurchasePlanQueryParameters parameters)
        {
            var records = _plans.Values.ToList();
            return Task.FromResult(new PagedResult<PurchasePlanDto>
            {
                Records = records,
                Total = records.Count,
                Current = parameters.Current,
                Size = parameters.Size
            });
        }

        public Task<PurchasePlanDto> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_plans[id]);
        }

        public Task<PurchasePlanDto> CreateAsync(CreatePurchasePlanDto dto)
        {
            var planId = Guid.NewGuid();
            var details = dto.Details.Select(detail => new PurchasePlanDetailDto
            {
                Id = Guid.NewGuid(),
                PurchasePlanId = planId,
                GoodsId = detail.GoodsId,
                GoodsName = "联调商品",
                GoodsCode = "API_GOODS_009",
                PurchaseUnitId = detail.PurchaseUnitId,
                PurchaseUnitName = "千克",
                PlannedQuantity = detail.PlannedQuantity,
                RequiredQuantity = detail.RequiredQuantity ?? detail.PlannedQuantity
            }).ToList();
            var plan = new PurchasePlanDto
            {
                Id = planId,
                PlanNo = $"PP{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                PlanDate = dto.PlanDate,
                PurchasePattern = dto.PurchasePattern,
                PurchaseStatus = PurchasePlanStatus.Unpublished,
                Remark = dto.Remark,
                Details = details
            };
            _plans.Add(plan.Id, plan);
            return Task.FromResult(plan);
        }

        public Task<List<PurchasePlanDto>> GenerateFromOrdersAsync(GeneratePurchasePlanFromOrdersDto dto)
        {
            var plans = dto.OrderIds.Select(orderId =>
            {
                var plan = new PurchasePlanDto
                {
                    Id = Guid.NewGuid(),
                    PlanNo = $"PP{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    PlanDate = DateTime.UtcNow,
                    PurchasePattern = PurchasePattern.SupplierDirect,
                    PurchaseStatus = PurchasePlanStatus.Unpublished,
                    Remark = dto.Remark
                };
                _plans.Add(plan.Id, plan);
                return plan;
            }).ToList();
            return Task.FromResult(plans);
        }

        public Task<List<PurchasePlanDto>> AssignSupplierAsync(AssignPurchasePlanSupplierDto dto)
        {
            return Task.FromResult(dto.PlanIds.Select(id => _plans[id]).ToList());
        }

        public Task<List<PurchasePlanDto>> AssignPurchaserAsync(AssignPurchasePlanPurchaserDto dto)
        {
            return Task.FromResult(dto.PlanIds.Select(id => _plans[id]).ToList());
        }

        public Task<PurchasePlanDto> MergeAsync(MergePurchasePlansDto dto)
        {
            return Task.FromResult(_plans[dto.PlanIds[0]]);
        }

        public Task<List<SplittablePurchasePlanOrderDto>> GetSplittableOrdersAsync(Guid planId)
        {
            return Task.FromResult(new List<SplittablePurchasePlanOrderDto>());
        }

        public Task<PurchasePlanDto> SplitByOrdersAsync(SplitPurchasePlanByOrdersDto dto)
        {
            return Task.FromResult(_plans[dto.PlanId]);
        }

        public Task<PurchasePlanDto> SplitByQuantityAsync(SplitPurchasePlanByQuantityDto dto)
        {
            return Task.FromResult(_plans[dto.PlanId]);
        }
    }
}
