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
using SkyRoc.Tests.Common;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>
///     T3 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证禁用用户无法登录，且禁用后刷新令牌失效。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AuthDisabledUserLoginPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     禁用账号登录被拒绝并写失败审计；启用账号可登录，禁用后刷新返回空且旧 refresh 失效；本轮临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task AuthDisabledUser_LoginRejectedAndRefreshBlockedAfterDisable_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);
        var roleId = Guid.NewGuid();
        var disabledUserId = Guid.NewGuid();
        var enabledThenDisabledUserId = Guid.NewGuid();
        var disabledUsername = $"{batch.Id}-D";
        var enabledUsername = batch.Id;
        var roleCode = batch.Id;
        var password = "SkyRocDisabledTemp!2026";
        var userAgent = $"SkyRoc-T3-Disabled/{batch.Id}";

        await using (var seedContext = fixture.CreateDbContext())
        {
            await seedContext.Roles.AddAsync(new Role
            {
                Id = roleId,
                Code = roleCode,
                Name = roleCode,
                Desc = "T3 禁用用户登录切片临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = "T3-AuthDisabledUser"
            });
            await seedContext.Users.AddAsync(new User
            {
                Id = disabledUserId,
                Username = disabledUsername,
                NickName = "T3禁用登录用户",
                Gender = GenderType.Female,
                Phone = "13900006666",
                Email = $"{batch.Id}-d@skyroc-autotest.example",
                PasswordHash = PasswordHasher.Hash(password),
                Status = Status.Disable,
                CreateName = "T3-AuthDisabledUser"
            });
            await seedContext.Users.AddAsync(new User
            {
                Id = enabledThenDisabledUserId,
                Username = enabledUsername,
                NickName = "T3先启后禁用户",
                Gender = GenderType.Male,
                Phone = "13900007777",
                Email = $"{batch.Id}@skyroc-autotest.example",
                PasswordHash = PasswordHasher.Hash(password),
                Status = Status.Enable,
                CreateName = "T3-AuthDisabledUser"
            });
            await seedContext.UserRoles.AddRangeAsync(
                new UserRole { UserId = disabledUserId, RoleId = roleId },
                new UserRole { UserId = enabledThenDisabledUserId, RoleId = roleId });
            await seedContext.SaveChangesAsync();
        }

        registry.Register<Role>(roleId, nameof(Role.Code), roleCode);
        registry.Register<User>(disabledUserId, nameof(User.Username), disabledUsername);
        registry.Register<User>(enabledThenDisabledUserId, nameof(User.Username), enabledUsername);

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 已禁用用户：密码正确也不得登录
            using (var disabledLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = disabledUsername,
                Password = password
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(disabledLoginResponse, ResponseCode.DatabaseError);
                var disabledBody = await disabledLoginResponse.Content.ReadAsStringAsync();
                var disabledPayload = JsonSerializer.Deserialize<ApiResponse<object?>>(disabledBody, JsonOptions);
                Assert.NotNull(disabledPayload);
                Assert.Equal("用户已禁用", disabledPayload.Msg);
                Assert.DoesNotContain(password, disabledBody, StringComparison.Ordinal);
            }

            // 启用用户正常登录后拿到 refresh
            LoginResDto login;
            using (var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = enabledUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
                login = await ReadApiDataAsync<LoginResDto>(loginResponse);
                Assert.False(string.IsNullOrWhiteSpace(login.Token));
                Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
            }

            // 中途禁用该用户
            await using (var disableContext = fixture.CreateDbContext())
            {
                var user = await disableContext.Users.SingleAsync(u => u.Id == enabledThenDisabledUserId);
                user.Status = Status.Disable;
                await disableContext.SaveChangesAsync();
            }

            // 禁用后刷新应返回空 data，且旧 refresh 被吊销
            using (var refreshResponse = await client.PostAsJsonAsync(
                       "/api/auth/refresh-token",
                       new RefreshTokenReqDto { RefreshToken = login.RefreshToken }))
            {
                Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
                var refreshPayload = await ReadApiResponseAsync<LoginResDto?>(refreshResponse);
                Assert.Equal(ResponseCode.Success, refreshPayload.Code);
                Assert.Null(refreshPayload.Data);
            }

            using (var reuseRefresh = await client.PostAsJsonAsync(
                       "/api/auth/refresh-token",
                       new RefreshTokenReqDto { RefreshToken = login.RefreshToken }))
            {
                Assert.Equal(HttpStatusCode.OK, reuseRefresh.StatusCode);
                var reusePayload = await ReadApiResponseAsync<LoginResDto?>(reuseRefresh);
                Assert.Null(reusePayload.Data);
            }

            // 禁用后再次登录仍被拒绝
            using (var reloginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = enabledUsername,
                Password = password
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reloginResponse, ResponseCode.DatabaseError);
            }

            await using var verifyContext = fixture.CreateDbContext();
            var loginLogs = await verifyContext.LoginLogs.AsNoTracking()
                .Where(log => log.Username == disabledUsername || log.Username == enabledUsername)
                .OrderBy(log => log.LoginTime)
                .ToListAsync();

            Assert.Contains(loginLogs, log =>
                log.Username == disabledUsername
                && !log.IsSuccess
                && log.UserId == disabledUserId
                && log.FailureReason == "用户已禁用");
            Assert.Contains(loginLogs, log =>
                log.Username == enabledUsername
                && log.IsSuccess
                && log.UserId == enabledThenDisabledUserId
                && log.FailureReason == null);
            Assert.Contains(loginLogs, log =>
                log.Username == enabledUsername
                && !log.IsSuccess
                && log.UserId == enabledThenDisabledUserId
                && log.FailureReason == "用户已禁用");

            Assert.All(loginLogs, log =>
            {
                Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
                Assert.DoesNotContain("Bearer", log.FailureReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(login.Token, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
            });

            // 旧 access 在缓存未主动吊销前可能仍可用；本切片只约束登录与刷新路径，不伪造全局踢下线。
            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, enabledUsername, disabledUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, enabledUsername, disabledUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, enabledUsername, disabledUsername);
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == enabledUsername || user.Username == disabledUsername));
            Assert.False(await residualContext.Roles.AnyAsync(role => role.Code == roleCode));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == enabledUsername || log.Username == disabledUsername));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == disabledUserId || relation.UserId == enabledThenDisabledUserId));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == enabledUsername || log.CreateName == disabledUsername));
        }
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
                // 已登记则跳过。
            }
        }
    }

    private static async Task RegisterResidualLoginLogsAsync(
        PostgreSqlTestFixture fixture,
        BatchCleanupRegistry registry,
        string enabledUsername,
        string disabledUsername)
    {
        await using var context = fixture.CreateDbContext();
        var residualLogs = await context.LoginLogs.AsNoTracking()
            .Where(log => log.Username == enabledUsername || log.Username == disabledUsername)
            .ToListAsync();
        RegisterLoginLogs(registry, residualLogs);
    }

    private static async Task RegisterBatchOperationLogsAsync(
        PostgreSqlTestFixture fixture,
        BatchCleanupRegistry registry,
        string enabledUsername,
        string disabledUsername)
    {
        await using var context = fixture.CreateDbContext();
        var operationLogs = await context.OperationLogs.AsNoTracking()
            .Where(log => log.CreateName == enabledUsername || log.CreateName == disabledUsername)
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
                // 已登记则跳过。
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
