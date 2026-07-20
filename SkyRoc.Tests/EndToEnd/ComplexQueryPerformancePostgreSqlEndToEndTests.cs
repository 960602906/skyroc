using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Domain.Entities;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Common;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.EndToEnd;

/// <summary>
///     T13 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证复杂只读查询的预热后 p95 性能门禁与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class ComplexQueryPerformancePostgreSqlEndToEndTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     预热后对销售/售后/库存/采购报表、驾驶舱、售后分页与库存总览采样；
    ///     各端点 p95 不超过门禁；401/403（财务相邻权限无报表）权限矩阵；临时批次精确清理。
    /// </summary>
    [Fact]
    public async Task ComplexQuery_WarmP95GateAndPermissionMatrix_OnPostgreSql()
    {
        const int warmRounds = 2;
        const int sampleRounds = 5;
        const double maxP95Milliseconds = 8000d;

        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedReadButtonId = Guid.NewGuid();

        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var password = "SkyRocT13QueryPerf!2026";
        var userAgent = $"SkyRoc-T13-QueryPerf/{batch.Id}";
        var createName = "T13-QueryPerf";

        var reportDateStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var reportDateEnd = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var reportsReadPermission = PermissionCodes.Business.Reports.Read;
        var financeReadPermission = PermissionCodes.Business.Finance.Read;

        Guid adminRoleId;
        Guid managedCustomerId;
        Guid managedGoodsId;
        Guid managedWareId;
        Guid managedSupplierId;
        Guid managedPurchaserId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var customerCode = DemoDataStableKeyCatalog.Create("CUSTOMER", 1);
            var goodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);
            var supplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);
            var purchaserCode = DemoDataStableKeyCatalog.Create("PURCHASER", 1);

            var customer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == customerCode);
            Assert.NotNull(customer);
            managedCustomerId = customer.Id;

            var goods = await seedContext.Goods.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsCode);
            Assert.NotNull(goods);
            managedGoodsId = goods.Id;

            var ware = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == wareCode);
            Assert.NotNull(ware);
            managedWareId = ware.Id;

            var supplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == supplierCode);
            Assert.NotNull(supplier);
            managedSupplierId = supplier.Id;

            var purchaser = await seedContext.Purchasers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == purchaserCode);
            Assert.NotNull(purchaser);
            managedPurchaserId = purchaser.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T13 复杂查询性能相邻权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T13复杂查询性能操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001351",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T13财务只读无报表权限",
                    Gender = GenderType.Female,
                    Phone = "13900001352",
                    Email = $"{batch.Id}-l@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.UserRoles.AddRangeAsync(
                new UserRole { UserId = adminUserId, RoleId = adminRoleId },
                new UserRole { UserId = limitedUserId, RoleId = limitedRoleId });

            await seedContext.Menus.AddAsync(new Menu
            {
                Id = seedMenuId,
                Name = seedMenuName,
                Path = $"/{batch.Id}s",
                Title = "T13财务只读菜单",
                Component = "page.t13.query.performance.seed",
                MenuType = MenuType.Menu,
                Order = 9132,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedReadButtonId,
                Code = financeReadPermission,
                Desc = "T13 财务读取权限按钮（故意不含报表）",
                MenuId = seedMenuId,
                Menu = null!,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.RoleMenus.AddAsync(new RoleMenu
            {
                RoleId = limitedRoleId,
                MenuId = seedMenuId
            });

            await seedContext.SaveChangesAsync();
        }

        registry.Register<Role>(limitedRoleId, nameof(Role.Code), limitedRoleCode);
        registry.Register<User>(adminUserId, nameof(User.Username), adminUsername);
        registry.Register<User>(limitedUserId, nameof(User.Username), limitedUsername);
        registry.Register<Menu>(seedMenuId, nameof(Menu.Name), seedMenuName);

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var salesGoodsUrl = BuildSalesGoodsUrl(
                reportDateStart,
                reportDateEnd,
                managedCustomerId);
            using (var anonymousSales = await anonymousClient.GetAsync(salesGoodsUrl))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousSales, ResponseCode.Unauthorized);
            }

            LoginResDto adminLogin;
            using (var loginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = adminUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
                adminLogin = await ReadApiDataAsync<LoginResDto>(loginResponse);
                Assert.False(string.IsNullOrWhiteSpace(adminLogin.Token));
            }

            using var adminClient = factory.CreateClient();
            adminClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            adminClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var endpoints = new (string Name, string Url)[]
            {
                ("sales-goods", salesGoodsUrl),
                ("after-sale-report", BuildAfterSaleReportUrl(
                    reportDateStart,
                    reportDateEnd,
                    managedCustomerId)),
                ("stock-daily", BuildStockDailyUrl(
                    reportDateStart,
                    reportDateEnd,
                    managedWareId,
                    managedGoodsId)),
                ("purchase-goods", BuildPurchaseGoodsUrl(
                    reportDateStart,
                    reportDateEnd,
                    managedWareId,
                    managedSupplierId,
                    managedPurchaserId,
                    managedGoodsId)),
                ("dashboard-brief", BuildDashboardBriefUrl(reportDateStart, reportDateEnd)),
                ("after-sale-list", "/api/after-sales?current=1&size=20"),
                ("stock-overview", $"/api/stock/overview?current=1&size=20&wareId={managedWareId}")
            };

            foreach (var (_, url) in endpoints)
            {
                for (var i = 0; i < warmRounds; i++)
                {
                    using var warm = await adminClient.GetAsync(url);
                    Assert.Equal(HttpStatusCode.OK, warm.StatusCode);
                    await ApiHttpAssert.AssertBusinessCodeAsync(warm, ResponseCode.Success);
                }
            }

            var p95ByEndpoint = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var (name, url) in endpoints)
            {
                var samples = new List<double>(sampleRounds);
                for (var i = 0; i < sampleRounds; i++)
                {
                    var sw = Stopwatch.StartNew();
                    using var response = await adminClient.GetAsync(url);
                    sw.Stop();
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    await ApiHttpAssert.AssertBusinessCodeAsync(response, ResponseCode.Success);
                    samples.Add(sw.Elapsed.TotalMilliseconds);
                }

                var p95 = Percentile(samples, 0.95);
                p95ByEndpoint[name] = p95;
                Assert.True(
                    p95 <= maxP95Milliseconds,
                    $"端点 {name} 预热后 p95={p95:F1}ms 超过门禁 {maxP95Milliseconds}ms；样本=[{string.Join(", ", samples.Select(v => v.ToString("F1")))}]");
            }

            Assert.Equal(endpoints.Length, p95ByEndpoint.Count);
            Assert.All(p95ByEndpoint.Values, value => Assert.True(value > 0d));

            LoginResDto limitedLogin;
            using (var limitedLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, limitedLoginResponse.StatusCode);
                limitedLogin = await ReadApiDataAsync<LoginResDto>(limitedLoginResponse);
            }

            using var limitedClient = factory.CreateClient();
            limitedClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedLogin.Token);
            limitedClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var limitedInfo = await limitedClient.GetAsync("/api/auth/getUserInfo"))
            {
                var info = await ReadApiDataAsync<UserInfoDto>(limitedInfo);
                Assert.Contains(financeReadPermission, info.Buttons);
                Assert.DoesNotContain(reportsReadPermission, info.Buttons);
            }

            using (var deniedSales = await limitedClient.GetAsync(salesGoodsUrl))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedSales, ResponseCode.Forbidden);
            }

            using (var deniedDashboard = await limitedClient.GetAsync(
                       BuildDashboardBriefUrl(reportDateStart, reportDateEnd)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDashboard, ResponseCode.Forbidden);
            }

            await using (var auditContext = fixture.CreateDbContext())
            {
                var loginLogs = await auditContext.LoginLogs.AsNoTracking()
                    .Where(log => log.Username == adminUsername || log.Username == limitedUsername)
                    .ToListAsync();
                Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
                Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
                Assert.All(loginLogs, log =>
                    Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal));
                RegisterLoginLogs(registry, loginLogs);
                await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

                Assert.True(await auditContext.Roles.AnyAsync(role =>
                    role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
                Assert.True(await auditContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
                Assert.True(await auditContext.Goods.AnyAsync(item => item.Id == managedGoodsId));
            }
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };
                var residualUsernames = new[] { adminUsername, limitedUsername };

                var residualUserRoles = await cleanupContext.UserRoles
                    .Where(relation => residualUserIds.Contains(relation.UserId))
                    .ToListAsync();
                if (residualUserRoles.Count > 0)
                {
                    cleanupContext.UserRoles.RemoveRange(residualUserRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualUsers = await cleanupContext.Users
                    .Where(user => residualUserIds.Contains(user.Id)
                                   || residualUsernames.Contains(user.Username)
                                   || (user.Username != null && user.Username.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualUsers.Count > 0)
                {
                    cleanupContext.Users.RemoveRange(residualUsers);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoleMenus = await cleanupContext.RoleMenus
                    .Where(relation => relation.RoleId == limitedRoleId || relation.MenuId == seedMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoles = await cleanupContext.Roles
                    .Where(role => role.Id == limitedRoleId
                                   || role.Code == limitedRoleCode
                                   || (role.Code != null && role.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualRoles.Count > 0)
                {
                    cleanupContext.Roles.RemoveRange(residualRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => button.Id == seedReadButtonId
                                     || button.MenuId == seedMenuId
                                     || button.CreateName == createName)
                    .ToListAsync();
                if (residualButtons.Count > 0)
                {
                    cleanupContext.MenuButtons.RemoveRange(residualButtons);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualMenus = await cleanupContext.Menus
                    .Where(menu => menu.Id == seedMenuId || menu.Name == seedMenuName)
                    .ToListAsync();
                if (residualMenus.Count > 0)
                {
                    cleanupContext.Menus.RemoveRange(residualMenus);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername
                || user.Username == limitedUsername
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId
                || role.Code == limitedRoleCode
                || (role.Code != null && role.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Id == seedMenuId || menu.Name == seedMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedReadButtonId || button.CreateName == createName));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
            Assert.True(await residualContext.Goods.AnyAsync(item => item.Id == managedGoodsId));
        }
    }

    private static double Percentile(IReadOnlyList<double> samples, double percentile)
    {
        Assert.NotEmpty(samples);
        var ordered = samples.OrderBy(value => value).ToArray();
        if (ordered.Length == 1)
            return ordered[0];

        var rank = percentile * (ordered.Length - 1);
        var lower = (int)Math.Floor(rank);
        var upper = (int)Math.Ceiling(rank);
        if (lower == upper)
            return ordered[lower];

        var weight = rank - lower;
        return ordered[lower] * (1d - weight) + ordered[upper] * weight;
    }

    private static string BuildSalesGoodsUrl(DateTime dateStart, DateTime dateEnd, Guid customerId)
    {
        return $"/api/reports/sales/goods?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&customerId={customerId}";
    }

    private static string BuildAfterSaleReportUrl(DateTime dateStart, DateTime dateEnd, Guid customerId)
    {
        return $"/api/reports/after-sales?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&customerId={customerId}";
    }

    private static string BuildStockDailyUrl(
        DateTime dateStart,
        DateTime dateEnd,
        Guid wareId,
        Guid goodsId)
    {
        return $"/api/reports/stock/daily?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&wareId={wareId}&goodsIds[0]={goodsId}";
    }

    private static string BuildPurchaseGoodsUrl(
        DateTime dateStart,
        DateTime dateEnd,
        Guid wareId,
        Guid supplierId,
        Guid purchaserId,
        Guid goodsId)
    {
        return $"/api/reports/purchase-in-out/goods?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&wareId={wareId}&supplierId={supplierId}&purchaserId={purchaserId}&goodsIds[0]={goodsId}";
    }

    private static string BuildDashboardBriefUrl(DateTime dateStart, DateTime dateEnd)
    {
        return $"/api/dashboard/brief?dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}";
    }

    private static void RegisterLoginLogs(BatchCleanupRegistry registry, IEnumerable<LoginLog> loginLogs)
    {
        foreach (var log in loginLogs)
        {
            try
            {
                registry.Register<LoginLog>(log.Id, nameof(LoginLog.Username), log.Username);
            }
            catch (InvalidOperationException)
            {
                // 已登记则跳过
            }
        }
    }

    private static async Task RegisterResidualLoginLogsAsync(
        PostgreSqlTestFixture fixture,
        BatchCleanupRegistry registry,
        params string[] usernames)
    {
        await using var context = fixture.CreateDbContext();
        var nameSet = usernames.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
        if (nameSet.Length == 0)
            return;

        var residualLogs = await context.LoginLogs.AsNoTracking()
            .Where(log => log.Username != null && nameSet.Contains(log.Username))
            .ToListAsync();
        RegisterLoginLogs(registry, residualLogs);
    }

    private static async Task RegisterBatchOperationLogsAsync(
        PostgreSqlTestFixture fixture,
        BatchCleanupRegistry registry,
        params string[] usernames)
    {
        await using var context = fixture.CreateDbContext();
        var nameSet = usernames.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
        if (nameSet.Length == 0)
            return;

        var operationLogs = await context.OperationLogs.AsNoTracking()
            .Where(log => log.CreateName != null && nameSet.Contains(log.CreateName))
            .ToListAsync();
        foreach (var log in operationLogs)
        {
            if (string.IsNullOrWhiteSpace(log.CreateName))
                continue;
            try
            {
                registry.Register<OperationLog>(log.Id, nameof(OperationLog.CreateName), log.CreateName);
            }
            catch (InvalidOperationException)
            {
                // 已登记则跳过
            }
        }
    }

    private static async Task<T> ReadApiDataAsync<T>(HttpResponseMessage response)
    {
        var payload = await ReadApiResponseAsync<T>(response);
        Assert.Equal(ResponseCode.Success, payload.Code);
        Assert.NotNull(payload.Data);
        return payload.Data!;
    }

    private static async Task<ApiResponse<T>> ReadApiResponseAsync<T>(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<ApiResponse<T>>(stream, JsonOptions);
        Assert.NotNull(payload);
        return payload!;
    }
}
