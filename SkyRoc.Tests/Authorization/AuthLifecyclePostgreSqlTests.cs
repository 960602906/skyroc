using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Application.DTOs.Auth;
using Domain.Entities;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Tests.Common;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>
///     T3 首个切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证登录、刷新、用户信息、路由、注销与登录审计闭环。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AuthLifecyclePostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     临时启用账号可完成登录→受保护读→刷新轮换→注销失效；错误凭据与禁用账号均失败并写审计；本轮临时数据必须精确清理。
    /// </summary>
    [Fact]
    public async Task AuthLifecycle_LoginRefreshUserInfoRoutesLogoutAndAudit_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);
        var roleId = Guid.NewGuid();
        var enabledUserId = Guid.NewGuid();
        var disabledUserId = Guid.NewGuid();
        var enabledUsername = batch.Id;
        var disabledUsername = $"{batch.Id}-D";
        var roleCode = batch.Id;
        var password = "SkyRocAuthTemp!2026";
        var userAgent = $"SkyRoc-T3-Auth/{batch.Id}";

        await using (var seedContext = fixture.CreateDbContext())
        {
            await seedContext.Roles.AddAsync(new Role
            {
                Id = roleId,
                Code = roleCode,
                Name = roleCode,
                Desc = "T3 认证生命周期临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = "T3-AuthLifecycle"
            });
            await seedContext.Users.AddAsync(new User
            {
                Id = enabledUserId,
                Username = enabledUsername,
                NickName = "T3认证启用用户",
                Gender = GenderType.Male,
                Phone = "13900001111",
                Email = $"{batch.Id}@skyroc-autotest.example",
                PasswordHash = PasswordHasher.Hash(password),
                Status = Status.Enable,
                CreateName = "T3-AuthLifecycle"
            });
            await seedContext.Users.AddAsync(new User
            {
                Id = disabledUserId,
                Username = disabledUsername,
                NickName = "T3认证禁用用户",
                Gender = GenderType.Female,
                Phone = "13900002222",
                Email = $"{batch.Id}-d@skyroc-autotest.example",
                PasswordHash = PasswordHasher.Hash(password),
                Status = Status.Disable,
                CreateName = "T3-AuthLifecycle"
            });
            await seedContext.UserRoles.AddRangeAsync(
                new UserRole { UserId = enabledUserId, RoleId = roleId },
                new UserRole { UserId = disabledUserId, RoleId = roleId });
            await seedContext.SaveChangesAsync();
        }

        registry.Register<Role>(roleId, nameof(Role.Code), roleCode);
        registry.Register<User>(enabledUserId, nameof(User.Username), enabledUsername);
        registry.Register<User>(disabledUserId, nameof(User.Username), disabledUsername);

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问受保护接口
            using (var anonymousResponse = await client.GetAsync("/api/auth/getUserInfo"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousResponse, ResponseCode.Unauthorized);
            }

            // 错误密码
            using (var wrongPasswordResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = enabledUsername,
                Password = "WrongPassword!999"
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(wrongPasswordResponse, ResponseCode.DatabaseError);
                var wrongBody = await wrongPasswordResponse.Content.ReadAsStringAsync();
                Assert.DoesNotContain(password, wrongBody, StringComparison.Ordinal);
                Assert.DoesNotContain("WrongPassword!999", wrongBody, StringComparison.Ordinal);
            }

            // 不存在用户
            using (var missingUserResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = $"{batch.Id}-missing",
                Password = password
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(missingUserResponse, ResponseCode.NotFound);
            }

            // 禁用用户即使密码正确也不得登录
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

            // 正常登录
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
                Assert.Equal(AuthConstants.BearerScheme, login.TokenType);
                Assert.True(login.ExpiresIn > 0);
            }

            using var authedClient = factory.CreateClient();
            authedClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, login.Token);
            authedClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // getUserInfo
            using (var infoResponse = await authedClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoResponse.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoResponse);
                Assert.Equal(enabledUserId, info.UserId);
                Assert.Equal(enabledUsername, info.UserName);
                Assert.Contains(roleCode, info.Roles);
                Assert.NotNull(info.Permissions);
                Assert.NotNull(info.Buttons);
            }

            // getRoutes（无菜单授权时返回空路由树，首页固定）
            using (var routesResponse = await authedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                Assert.Equal("/home", routes.Home);
                Assert.NotNull(routes.Routes);
            }

            // 刷新令牌轮换：旧 refresh 失效，新 access 可用
            LoginResDto refreshed;
            using (var refreshResponse = await client.PostAsJsonAsync(
                       "/api/auth/refresh-token",
                       new RefreshTokenReqDto { RefreshToken = login.RefreshToken }))
            {
                Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
                refreshed = await ReadApiDataAsync<LoginResDto>(refreshResponse);
                Assert.False(string.IsNullOrWhiteSpace(refreshed.Token));
                Assert.False(string.IsNullOrWhiteSpace(refreshed.RefreshToken));
                Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
                Assert.NotEqual(login.Token, refreshed.Token);
            }

            using (var reuseOldRefresh = await client.PostAsJsonAsync(
                       "/api/auth/refresh-token",
                       new RefreshTokenReqDto { RefreshToken = login.RefreshToken }))
            {
                Assert.Equal(HttpStatusCode.OK, reuseOldRefresh.StatusCode);
                await ApiHttpAssert.AssertBusinessCodeAsync(reuseOldRefresh, ResponseCode.Unauthorized);
            }

            using var refreshedClient = factory.CreateClient();
            refreshedClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, refreshed.Token);
            refreshedClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            using (var afterRefreshInfo = await refreshedClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, afterRefreshInfo.StatusCode);
            }

            // 注销后 access 与 refresh 均失效
            using (var logoutResponse = await refreshedClient.PostAsJsonAsync(
                       "/api/auth/logout",
                       new RefreshTokenReqDto { RefreshToken = refreshed.RefreshToken }))
            {
                Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(logoutResponse));
            }

            using (var afterLogoutInfo = await refreshedClient.GetAsync("/api/auth/getUserInfo"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(afterLogoutInfo, ResponseCode.Unauthorized);
            }

            using (var afterLogoutRefresh = await client.PostAsJsonAsync(
                       "/api/auth/refresh-token",
                       new RefreshTokenReqDto { RefreshToken = refreshed.RefreshToken }))
            {
                Assert.Equal(HttpStatusCode.OK, afterLogoutRefresh.StatusCode);
                await ApiHttpAssert.AssertBusinessCodeAsync(afterLogoutRefresh, ResponseCode.Unauthorized);
            }

            // 访问令牌已吊销后，受保护的重复注销应拒绝认证；刷新令牌侧的幂等失效已在上一断言覆盖
            using (var secondLogout = await refreshedClient.PostAsJsonAsync(
                       "/api/auth/logout",
                       new RefreshTokenReqDto { RefreshToken = refreshed.RefreshToken }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(secondLogout, ResponseCode.Unauthorized);
            }

            // 数据库审计与密码哈希副作用
            await using var verifyContext = fixture.CreateDbContext();
            var enabledUser = await verifyContext.Users.AsNoTracking()
                .SingleAsync(user => user.Id == enabledUserId);
            Assert.True(PasswordHasher.Verify(enabledUser.PasswordHash, password));
            Assert.DoesNotContain(password, enabledUser.PasswordHash, StringComparison.Ordinal);

            var loginLogs = await verifyContext.LoginLogs.AsNoTracking()
                .Where(log => log.Username == enabledUsername
                              || log.Username == disabledUsername
                              || log.Username == $"{batch.Id}-missing")
                .OrderBy(log => log.LoginTime)
                .ToListAsync();

            Assert.Contains(loginLogs, log =>
                log.Username == enabledUsername
                && !log.IsSuccess
                && log.UserId == enabledUserId
                && log.FailureReason == "用户不存在或密码错误");
            Assert.Contains(loginLogs, log =>
                log.Username == $"{batch.Id}-missing"
                && !log.IsSuccess
                && log.UserId == null
                && log.FailureReason == "用户不存在或密码错误");
            Assert.Contains(loginLogs, log =>
                log.Username == enabledUsername
                && log.IsSuccess
                && log.UserId == enabledUserId
                && log.FailureReason == null);
            Assert.Contains(loginLogs, log =>
                log.Username == disabledUsername
                && !log.IsSuccess
                && log.UserId == disabledUserId
                && log.FailureReason == "用户已禁用");

            Assert.All(loginLogs, log =>
            {
                Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
                Assert.DoesNotContain("Bearer", log.FailureReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(login.Token, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
                Assert.False(string.IsNullOrWhiteSpace(log.Username));
            });

            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, enabledUsername, disabledUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, batch.Id, enabledUsername, disabledUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, enabledUsername, disabledUsername);
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == enabledUsername || user.Username == disabledUsername));
            Assert.False(await residualContext.Roles.AnyAsync(role => role.Code == roleCode));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == enabledUsername
                || log.Username == disabledUsername
                || log.Username == $"{batch.Id}-missing"));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == enabledUserId || relation.UserId == disabledUserId));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == enabledUsername || log.CreateName == disabledUsername));
        }
    }

    /// <summary>
    ///     Access JWT 已过期时，受保护接口返回专用业务码 TokenExpired（4011），供客户端静默 refresh。
    /// </summary>
    [Fact]
    public async Task GetUserInfo_ReturnsTokenExpired_WhenAccessJwtIsExpired()
    {
        using var factory = fixture.CreateWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(AuthConstants.BearerScheme, CreateExpiredAccessToken());

        using var response = await client.GetAsync("/api/auth/getUserInfo");
        await ApiHttpAssert.AssertBusinessCodeAsync(response, ResponseCode.TokenExpired);
    }

    /// <summary>
    ///     使用与 PostgreSQL 测试宿主相同的签名密钥签发已过期的 access JWT。
    /// </summary>
    private static string CreateExpiredAccessToken()
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("postgresql-test-only-key-with-at-least-32-bytes"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            "skyrocket",
            "skyrocket",
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("N"))],
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
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
                // 已登记则跳过，保证 finally 与主路径均可幂等登记。
            }
        }
    }

    private static async Task RegisterResidualLoginLogsAsync(
        PostgreSqlTestFixture fixture,
        BatchCleanupRegistry registry,
        string batchId,
        string enabledUsername,
        string disabledUsername)
    {
        await using var context = fixture.CreateDbContext();
        var residualLogs = await context.LoginLogs.AsNoTracking()
            .Where(log => log.Username == enabledUsername
                          || log.Username == disabledUsername
                          || log.Username == $"{batchId}-missing")
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
