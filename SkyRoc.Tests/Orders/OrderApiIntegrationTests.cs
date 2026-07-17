using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Application.DTOs.Orders;
using Application.Interfaces;
using Application.QueryParameters;
using Domain.Entities.Orders;
using Infrastructure.Data;
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
using SkyRoc.Tests.Common;
using SkyRoc.Tests.Testing;
using Xunit;

namespace SkyRoc.Tests.Orders;

public class OrderApiIntegrationTests
{
    private static readonly Guid CustomerId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid GoodsId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid GoodsUnitId = Guid.Parse("40000000-0000-0000-0000-000000000001");

    [Fact]
    public void OrderApiFactory_UsesInMemoryDatabaseForAuditScope()
    {
        using var factory = new OrderApiFactory();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", context.Database.ProviderName);
    }

    [Fact]
    public async Task OrderEndpoints_RequireAuthenticationAndExpectedPermission()
    {
        using var factory = new OrderApiFactory();
        using var anonymousClient = factory.CreateClient();

        var anonymousResponse = await anonymousClient.GetAsync("/api/orders/list?current=1&size=10");

        await ApiHttpAssert.AssertBusinessCodeAsync(anonymousResponse, ResponseCode.Unauthorized);

        using var readOnlyClient = factory.CreateClient();
        readOnlyClient.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.Business.Orders.Read);

        var readResponse = await readOnlyClient.GetAsync("/api/orders/list?current=1&size=10");
        var createResponse = await readOnlyClient.PostAsJsonAsync("/api/orders", CreateRequest());

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        await ApiHttpAssert.AssertBusinessCodeAsync(createResponse, ResponseCode.Forbidden);
    }

    [Fact]
    public async Task OrderMainFlow_CompletesThroughHttpApi()
    {
        using var factory = new OrderApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, PermissionCodes.All);

        var createResponse = await client.PostAsJsonAsync("/api/orders", CreateRequest());
        var created = await ReadDataAsync<SaleOrderDto>(createResponse);
        Assert.Equal(SaleOrderStatus.PendingAudit, created.OrderStatus);
        Assert.Single(created.Details);

        var listResponse = await client.GetAsync("/api/orders/list?current=1&size=10&keyword=SO");
        var page = await ReadDataAsync<PagedResult<SaleOrderDto>>(listResponse);
        Assert.Equal(created.Id, Assert.Single(page.Records!).Id);

        var detailResponse = await client.GetAsync($"/api/orders/{created.Id}");
        var detail = await ReadDataAsync<SaleOrderDto>(detailResponse);
        Assert.Equal(created.OrderNo, detail.OrderNo);

        var updateResponse = await client.PutAsJsonAsync("/api/orders", new UpdateSaleOrderDto
        {
            Id = created.Id,
            CustomerId = CustomerId,
            OrderDate = created.OrderDate,
            Remark = "联调已更新",
            Details =
            [
                new UpdateSaleOrderDetailDto
                {
                    Id = created.Details[0].Id,
                    GoodsId = GoodsId,
                    GoodsUnitId = GoodsUnitId,
                    Quantity = 3m,
                    FixedPrice = 4m,
                    FixedGoodsUnitId = GoodsUnitId
                }
            ]
        });
        var updated = await ReadDataAsync<SaleOrderDto>(updateResponse);
        Assert.Equal("联调已更新", updated.Remark);
        Assert.Equal(12m, updated.OrderPrice);

        var rejected = await PostAuditAsync(client, created.Id, "reject", "价格需确认");
        Assert.Equal(SaleOrderStatus.Rejected, rejected.OrderStatus);

        var resubmitted = await PostAuditAsync(client, created.Id, "resubmit", "价格已确认");
        Assert.Equal(SaleOrderStatus.PendingAudit, resubmitted.OrderStatus);

        var approved = await PostAuditAsync(client, created.Id, "approve", "审核通过");
        Assert.Equal(SaleOrderStatus.SortingPending, approved.OrderStatus);
        Assert.Equal(4, approved.AuditLogs.Count);

        var deleteResponse = await client.DeleteAsync($"/api/orders/{created.Id}");
        Assert.True(await ReadDataAsync<bool>(deleteResponse));
    }

    [Fact]
    public async Task Swagger_DocumentsOrderRoutesAndPermissions()
    {
        using var factory = new OrderApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var paths = document.RootElement.GetProperty("paths");
        var listOperation = paths.GetProperty("/api/orders/list").GetProperty("get");
        var approveOperation = paths.GetProperty("/api/orders/{id}/approve").GetProperty("post");

        Assert.Contains(PermissionCodes.Business.Orders.Read, listOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Orders.Audit, approveOperation.GetProperty("description").GetString());
        Assert.Equal("Bearer", listOperation.GetProperty("security")[0].EnumerateObject().Single().Name);
        Assert.Contains("body.code=401", listOperation.GetProperty("description").GetString());
        Assert.Contains("body.code=403", listOperation.GetProperty("description").GetString());
    }

    private static CreateSaleOrderDto CreateRequest()
    {
        return new CreateSaleOrderDto
        {
            CustomerId = CustomerId,
            OrderDate = new DateTime(2026, 7, 3, 8, 0, 0, DateTimeKind.Utc),
            ReceiveDate = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            ContactName = "张老师",
            Details =
            [
                new CreateSaleOrderDetailDto
                {
                    GoodsId = GoodsId,
                    GoodsUnitId = GoodsUnitId,
                    Quantity = 2m,
                    FixedPrice = 4m,
                    FixedGoodsUnitId = GoodsUnitId
                }
            ]
        };
    }

    private static async Task<SaleOrderDto> PostAuditAsync(
        HttpClient client,
        Guid orderId,
        string action,
        string remark)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/{action}",
            new SaleOrderAuditDto { Remark = remark });
        return await ReadDataAsync<SaleOrderDto>(response);
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(result);
        return Assert.IsType<T>(result.Data);
    }

    private sealed class OrderApiFactory : WebApplicationFactory<Program>
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
                services.UseIsolatedInMemoryPersistence($"order-api-{Guid.NewGuid():N}");
                services.RemoveAll<ISaleOrderService>();
                services.AddSingleton<ISaleOrderService, InMemorySaleOrderService>();
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
        public const string AuthenticationScheme = "OrderIntegrationTest";
        public const string PermissionsHeader = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(PermissionsHeader, out var values))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "10000000-0000-0000-0000-000000000001"),
                new(ClaimTypes.Name, "order-api-test")
            };
            claims.AddRange(values.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(permission => new Claim(AuthConstants.PermissionClaimType, permission)));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationScheme));
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, AuthenticationScheme)));
        }
    }

    private sealed class InMemorySaleOrderService : ISaleOrderService
    {
        private readonly Dictionary<Guid, SaleOrderDto> _orders = [];

        public Task<PagedResult<SaleOrderDto>> GetPagedAsync(SaleOrderQueryParameters parameters)
        {
            var records = _orders.Values
                .Where(order => string.IsNullOrWhiteSpace(parameters.Keyword)
                                || order.OrderNo.Contains(parameters.Keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(new PagedResult<SaleOrderDto>
            {
                Records = records,
                Total = records.Count,
                Current = parameters.Current,
                Size = parameters.Size
            });
        }

        public Task<SaleOrderDto> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_orders[id]);
        }

        public Task<SaleOrderDto> CreateAsync(CreateSaleOrderDto dto)
        {
            var orderId = Guid.NewGuid();
            var details = dto.Details.Select(detail => new SaleOrderDetailDto
            {
                Id = Guid.NewGuid(),
                SaleOrderId = orderId,
                GoodsId = detail.GoodsId,
                GoodsName = "联调商品",
                GoodsCode = "API_GOODS_001",
                GoodsUnitId = detail.GoodsUnitId,
                GoodsUnitName = "千克",
                Quantity = detail.Quantity,
                BaseQuantity = detail.Quantity,
                FixedPrice = detail.FixedPrice,
                FixedGoodsUnitId = detail.FixedGoodsUnitId,
                FixedGoodsUnitName = "千克",
                UnitConversion = 1m,
                TotalPrice = detail.Quantity * detail.FixedPrice
            }).ToList();
            var order = new SaleOrderDto
            {
                Id = orderId,
                OrderNo = $"SO{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                CustomerId = dto.CustomerId,
                CustomerName = "联调客户",
                CustomerCode = "API_CUSTOMER_001",
                OrderDate = dto.OrderDate,
                ReceiveDate = dto.ReceiveDate,
                ContactName = dto.ContactName,
                ContactPhone = dto.ContactPhone,
                DeliveryAddress = dto.DeliveryAddress,
                Remark = dto.Remark,
                InnerRemark = dto.InnerRemark,
                OrderStatus = SaleOrderStatus.PendingAudit,
                Details = details,
                OrderPrice = details.Sum(detail => detail.TotalPrice),
                SettlementPrice = details.Sum(detail => detail.TotalPrice),
                AuditLogs = [CreateAuditLog(orderId, OrderAuditAction.Submit, SaleOrderStatus.PendingAudit)]
            };
            _orders.Add(order.Id, order);
            return Task.FromResult(order);
        }

        public Task<SaleOrderDto> UpdateAsync(UpdateSaleOrderDto dto)
        {
            var order = _orders[dto.Id];
            order.CustomerId = dto.CustomerId;
            order.OrderDate = dto.OrderDate;
            order.ReceiveDate = dto.ReceiveDate;
            order.Remark = dto.Remark;
            order.UpdateStatus = true;
            order.Details = dto.Details.Select(detail => new SaleOrderDetailDto
            {
                Id = detail.Id ?? Guid.NewGuid(),
                SaleOrderId = order.Id,
                GoodsId = detail.GoodsId,
                GoodsName = "联调商品",
                GoodsCode = "API_GOODS_001",
                GoodsUnitId = detail.GoodsUnitId,
                GoodsUnitName = "千克",
                Quantity = detail.Quantity,
                BaseQuantity = detail.Quantity,
                FixedPrice = detail.FixedPrice,
                FixedGoodsUnitId = detail.FixedGoodsUnitId,
                FixedGoodsUnitName = "千克",
                UnitConversion = 1m,
                TotalPrice = detail.Quantity * detail.FixedPrice
            }).ToList();
            order.OrderPrice = order.Details.Sum(detail => detail.TotalPrice);
            order.SettlementPrice = order.OrderPrice;
            return Task.FromResult(order);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            return Task.FromResult(_orders.Remove(id));
        }

        public Task<SaleOrderDto> ApproveAsync(Guid id, string? remark)
        {
            return TransitionAsync(id, OrderAuditAction.Approve, SaleOrderStatus.SortingPending, remark);
        }

        public Task<SaleOrderDto> RejectAsync(Guid id, string? remark)
        {
            return TransitionAsync(id, OrderAuditAction.Reject, SaleOrderStatus.Rejected, remark);
        }

        public Task<SaleOrderDto> ResubmitAsync(Guid id, string? remark)
        {
            return TransitionAsync(id, OrderAuditAction.Resubmit, SaleOrderStatus.PendingAudit, remark);
        }

        private Task<SaleOrderDto> TransitionAsync(
            Guid id,
            OrderAuditAction action,
            SaleOrderStatus targetStatus,
            string? remark)
        {
            var order = _orders[id];
            var previousStatus = order.OrderStatus;
            order.OrderStatus = targetStatus;
            order.AuditLogs.Add(CreateAuditLog(id, action, targetStatus, previousStatus, remark));
            return Task.FromResult(order);
        }

        private static OrderAuditLogDto CreateAuditLog(
            Guid orderId,
            OrderAuditAction action,
            SaleOrderStatus currentStatus,
            SaleOrderStatus? previousStatus = null,
            string? remark = null)
        {
            return new OrderAuditLogDto
            {
                Id = Guid.NewGuid(),
                SaleOrderId = orderId,
                Action = action,
                PreviousStatus = previousStatus ?? currentStatus,
                CurrentStatus = currentStatus,
                AuditUserName = "order-api-test",
                AuditTime = DateTime.UtcNow,
                Remark = remark
            };
        }
    }
}
