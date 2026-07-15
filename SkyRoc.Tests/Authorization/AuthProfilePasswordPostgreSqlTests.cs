using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.User;
using Domain.Entities;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>
///     T3 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证个人中心资料更新与改密闭环。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AuthProfilePasswordPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     当前用户可查询/更新本人资料并改密后用新密码登录；错误旧密码、非法资料与未认证均被拒绝；仅影响本人且本轮临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task AuthProfilePassword_UpdateProfileChangePasswordAndRelogin_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);
        var roleId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var ownerUsername = batch.Id;
        var otherUsername = $"{batch.Id}-O";
        var roleCode = batch.Id;
        var originalPassword = "SkyRocProfileTemp!2026";
        var newPassword = "SkyRocProfileNew!2026";
        var userAgent = $"SkyRoc-T3-Profile/{batch.Id}";
        var ownerOriginalNickName = "T3资料本人用户";
        var otherOriginalNickName = "T3资料他人用户";
        var ownerOriginalPhone = "13900003333";
        var otherOriginalPhone = "13900004444";
        var ownerOriginalEmail = $"{batch.Id}@skyroc-autotest.example";
        var otherOriginalEmail = $"{batch.Id}-o@skyroc-autotest.example";
        var updatedNickName = "T3资料已更新";
        var updatedPhone = "13900005555";
        var updatedEmail = $"{batch.Id}-updated@skyroc-autotest.example";

        await using (var seedContext = fixture.CreateDbContext())
        {
            await seedContext.Roles.AddAsync(new Role
            {
                Id = roleId,
                Code = roleCode,
                Name = roleCode,
                Desc = "T3 个人中心改密切片临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = "T3-AuthProfilePassword"
            });
            await seedContext.Users.AddAsync(new User
            {
                Id = ownerUserId,
                Username = ownerUsername,
                NickName = ownerOriginalNickName,
                Gender = GenderType.Male,
                Phone = ownerOriginalPhone,
                Email = ownerOriginalEmail,
                PasswordHash = PasswordHasher.Hash(originalPassword),
                Status = Status.Enable,
                CreateName = "T3-AuthProfilePassword"
            });
            await seedContext.Users.AddAsync(new User
            {
                Id = otherUserId,
                Username = otherUsername,
                NickName = otherOriginalNickName,
                Gender = GenderType.Female,
                Phone = otherOriginalPhone,
                Email = otherOriginalEmail,
                PasswordHash = PasswordHasher.Hash(originalPassword),
                Status = Status.Enable,
                CreateName = "T3-AuthProfilePassword"
            });
            await seedContext.UserRoles.AddRangeAsync(
                new UserRole { UserId = ownerUserId, RoleId = roleId },
                new UserRole { UserId = otherUserId, RoleId = roleId });
            await seedContext.SaveChangesAsync();
        }

        registry.Register<Role>(roleId, nameof(Role.Code), roleCode);
        registry.Register<User>(ownerUserId, nameof(User.Username), ownerUsername);
        registry.Register<User>(otherUserId, nameof(User.Username), otherUsername);

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问个人中心
            using (var anonymousGet = await anonymousClient.GetAsync("/api/system/user/profile"))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousGet.StatusCode);
            }

            using (var anonymousUpdate = await anonymousClient.PutAsJsonAsync(
                       "/api/system/user/profile",
                       new UpdateProfileDto
                       {
                           NickName = updatedNickName,
                           Gender = GenderType.Female,
                           Phone = updatedPhone,
                           Email = updatedEmail
                       }))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousUpdate.StatusCode);
            }

            using (var anonymousPassword = await anonymousClient.PutAsJsonAsync(
                       "/api/system/user/profile/updatePwd",
                       new ChangePasswordDto
                       {
                           OldPassword = originalPassword,
                           NewPassword = newPassword
                       }))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousPassword.StatusCode);
            }

            // 本人登录
            LoginResDto login;
            using (var loginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = ownerUsername,
                Password = originalPassword
            }))
            {
                Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
                login = await ReadApiDataAsync<LoginResDto>(loginResponse);
                Assert.False(string.IsNullOrWhiteSpace(login.Token));
            }

            using var authedClient = factory.CreateClient();
            authedClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, login.Token);
            authedClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 查询本人资料
            using (var profileResponse = await authedClient.GetAsync("/api/system/user/profile"))
            {
                Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
                var profile = await ReadApiDataAsync<ProfileDto>(profileResponse);
                Assert.Equal(ownerUserId, profile.Id);
                Assert.Equal(ownerUsername, profile.Username);
                Assert.Equal(ownerOriginalNickName, profile.NickName);
                Assert.Equal(GenderType.Male, profile.Gender);
                Assert.Equal(ownerOriginalPhone, profile.Phone);
                Assert.Equal(ownerOriginalEmail, profile.Email);
            }

            // 非法手机号被校验拒绝，不改写资料
            using (var invalidProfileResponse = await authedClient.PutAsJsonAsync(
                       "/api/system/user/profile",
                       new UpdateProfileDto
                       {
                           NickName = "X",
                           Gender = GenderType.Female,
                           Phone = "12345",
                           Email = "not-an-email"
                       }))
            {
                Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidProfileResponse.StatusCode);
                var invalidBody = await invalidProfileResponse.Content.ReadAsStringAsync();
                Assert.DoesNotContain(originalPassword, invalidBody, StringComparison.Ordinal);
            }

            await using (var afterInvalidContext = fixture.CreateDbContext())
            {
                var owner = await afterInvalidContext.Users.AsNoTracking()
                    .SingleAsync(user => user.Id == ownerUserId);
                Assert.Equal(ownerOriginalNickName, owner.NickName);
                Assert.Equal(ownerOriginalPhone, owner.Phone);
                Assert.Equal(ownerOriginalEmail, owner.Email);
            }

            // 更新本人资料（ApiResponse<string>.Ok(message) 走消息重载，Data 为空，成功语义在 Msg）
            using (var updateResponse = await authedClient.PutAsJsonAsync(
                       "/api/system/user/profile",
                       new UpdateProfileDto
                       {
                           NickName = updatedNickName,
                           Gender = GenderType.Female,
                           Phone = updatedPhone,
                           Email = updatedEmail
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var updatePayload = await ReadApiResponseAsync<string>(updateResponse);
                Assert.Equal(ResponseCode.Success, updatePayload.Code);
                Assert.Equal("个人资料更新成功", updatePayload.Msg);
            }

            using (var updatedProfileResponse = await authedClient.GetAsync("/api/system/user/profile"))
            {
                Assert.Equal(HttpStatusCode.OK, updatedProfileResponse.StatusCode);
                var profile = await ReadApiDataAsync<ProfileDto>(updatedProfileResponse);
                Assert.Equal(ownerUserId, profile.Id);
                Assert.Equal(ownerUsername, profile.Username);
                Assert.Equal(updatedNickName, profile.NickName);
                Assert.Equal(GenderType.Female, profile.Gender);
                Assert.Equal(updatedPhone, profile.Phone);
                Assert.Equal(updatedEmail, profile.Email);
            }

            await using (var verifyProfileContext = fixture.CreateDbContext())
            {
                var owner = await verifyProfileContext.Users.AsNoTracking()
                    .SingleAsync(user => user.Id == ownerUserId);
                var other = await verifyProfileContext.Users.AsNoTracking()
                    .SingleAsync(user => user.Id == otherUserId);

                Assert.Equal(updatedNickName, owner.NickName);
                Assert.Equal(GenderType.Female, owner.Gender);
                Assert.Equal(updatedPhone, owner.Phone);
                Assert.Equal(updatedEmail, owner.Email);
                Assert.Equal(ownerUserId, owner.UpdateBy);
                Assert.Equal(ownerUsername, owner.UpdateName);

                Assert.Equal(otherOriginalNickName, other.NickName);
                Assert.Equal(GenderType.Female, other.Gender);
                Assert.Equal(otherOriginalPhone, other.Phone);
                Assert.Equal(otherOriginalEmail, other.Email);
                Assert.Null(other.UpdateBy);
            }

            // 旧密码错误不得改密
            using (var wrongOldPasswordResponse = await authedClient.PutAsJsonAsync(
                       "/api/system/user/profile/updatePwd",
                       new ChangePasswordDto
                       {
                           OldPassword = "WrongOldPassword!999",
                           NewPassword = newPassword
                       }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, wrongOldPasswordResponse.StatusCode);
                var wrongBody = await wrongOldPasswordResponse.Content.ReadAsStringAsync();
                var wrongPayload = JsonSerializer.Deserialize<ApiResponse<object>>(wrongBody, JsonOptions);
                Assert.NotNull(wrongPayload);
                Assert.Equal("旧密码错误", wrongPayload.Msg);
                Assert.DoesNotContain(originalPassword, wrongBody, StringComparison.Ordinal);
                Assert.DoesNotContain(newPassword, wrongBody, StringComparison.Ordinal);
                Assert.DoesNotContain("WrongOldPassword!999", wrongBody, StringComparison.Ordinal);
            }

            // 新密码与旧密码相同被校验拒绝
            using (var samePasswordResponse = await authedClient.PutAsJsonAsync(
                       "/api/system/user/profile/updatePwd",
                       new ChangePasswordDto
                       {
                           OldPassword = originalPassword,
                           NewPassword = originalPassword
                       }))
            {
                Assert.Equal(HttpStatusCode.UnprocessableEntity, samePasswordResponse.StatusCode);
            }

            // 正确旧密码可改密（同上，成功消息在 Msg）
            using (var changePasswordResponse = await authedClient.PutAsJsonAsync(
                       "/api/system/user/profile/updatePwd",
                       new ChangePasswordDto
                       {
                           OldPassword = originalPassword,
                           NewPassword = newPassword
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, changePasswordResponse.StatusCode);
                var changePayload = await ReadApiResponseAsync<string>(changePasswordResponse);
                Assert.Equal(ResponseCode.Success, changePayload.Code);
                Assert.Equal("密码修改成功", changePayload.Msg);
            }

            await using (var verifyPasswordContext = fixture.CreateDbContext())
            {
                var owner = await verifyPasswordContext.Users.AsNoTracking()
                    .SingleAsync(user => user.Id == ownerUserId);
                var other = await verifyPasswordContext.Users.AsNoTracking()
                    .SingleAsync(user => user.Id == otherUserId);

                Assert.True(PasswordHasher.Verify(owner.PasswordHash, newPassword));
                Assert.False(PasswordHasher.Verify(owner.PasswordHash, originalPassword));
                Assert.DoesNotContain(newPassword, owner.PasswordHash, StringComparison.Ordinal);
                Assert.DoesNotContain(originalPassword, owner.PasswordHash, StringComparison.Ordinal);
                Assert.True(PasswordHasher.Verify(other.PasswordHash, originalPassword));
            }

            // 旧密码登录失败，新密码登录成功
            using (var oldPasswordLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = ownerUsername,
                Password = originalPassword
            }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, oldPasswordLogin.StatusCode);
            }

            LoginResDto relogin;
            using (var newPasswordLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = ownerUsername,
                Password = newPassword
            }))
            {
                Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);
                relogin = await ReadApiDataAsync<LoginResDto>(newPasswordLogin);
                Assert.False(string.IsNullOrWhiteSpace(relogin.Token));
            }

            using var reloginClient = factory.CreateClient();
            reloginClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, relogin.Token);
            reloginClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            using (var profileAfterRelogin = await reloginClient.GetAsync("/api/system/user/profile"))
            {
                Assert.Equal(HttpStatusCode.OK, profileAfterRelogin.StatusCode);
                var profile = await ReadApiDataAsync<ProfileDto>(profileAfterRelogin);
                Assert.Equal(updatedNickName, profile.NickName);
                Assert.Equal(updatedEmail, profile.Email);
            }

            // 他人账号密码与资料保持不变
            using (var otherLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = otherUsername,
                Password = originalPassword
            }))
            {
                Assert.Equal(HttpStatusCode.OK, otherLogin.StatusCode);
                var otherLoginData = await ReadApiDataAsync<LoginResDto>(otherLogin);
                using var otherClient = factory.CreateClient();
                otherClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(AuthConstants.BearerScheme, otherLoginData.Token);
                otherClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
                using var otherProfile = await otherClient.GetAsync("/api/system/user/profile");
                Assert.Equal(HttpStatusCode.OK, otherProfile.StatusCode);
                var profile = await ReadApiDataAsync<ProfileDto>(otherProfile);
                Assert.Equal(otherUserId, profile.Id);
                Assert.Equal(otherOriginalNickName, profile.NickName);
                Assert.Equal(otherOriginalPhone, profile.Phone);
                Assert.Equal(otherOriginalEmail, profile.Email);
            }

            await using var auditContext = fixture.CreateDbContext();
            var loginLogs = await auditContext.LoginLogs.AsNoTracking()
                .Where(log => log.Username == ownerUsername || log.Username == otherUsername)
                .ToListAsync();
            Assert.Contains(loginLogs, log =>
                log.Username == ownerUsername
                && log.IsSuccess
                && log.UserId == ownerUserId);
            Assert.Contains(loginLogs, log =>
                log.Username == ownerUsername
                && !log.IsSuccess
                && log.UserId == ownerUserId
                && log.FailureReason == "用户不存在或密码错误");
            Assert.All(loginLogs, log =>
            {
                Assert.DoesNotContain(originalPassword, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
                Assert.DoesNotContain(newPassword, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
            });
            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, ownerUsername, otherUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, ownerUsername, otherUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, ownerUsername, otherUsername);
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == ownerUsername || user.Username == otherUsername));
            Assert.False(await residualContext.Roles.AnyAsync(role => role.Code == roleCode));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == ownerUsername || log.Username == otherUsername));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == ownerUserId || relation.UserId == otherUserId));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == ownerUsername || log.CreateName == otherUsername));
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
                // 已登记则跳过，保证 finally 与主路径均可幂等登记。
            }
        }
    }

    private static async Task RegisterResidualLoginLogsAsync(
        PostgreSqlTestFixture fixture,
        BatchCleanupRegistry registry,
        string ownerUsername,
        string otherUsername)
    {
        await using var context = fixture.CreateDbContext();
        var residualLogs = await context.LoginLogs.AsNoTracking()
            .Where(log => log.Username == ownerUsername || log.Username == otherUsername)
            .ToListAsync();
        RegisterLoginLogs(registry, residualLogs);
    }

    private static async Task RegisterBatchOperationLogsAsync(
        PostgreSqlTestFixture fixture,
        BatchCleanupRegistry registry,
        string ownerUsername,
        string otherUsername)
    {
        await using var context = fixture.CreateDbContext();
        var operationLogs = await context.OperationLogs.AsNoTracking()
            .Where(log => log.CreateName == ownerUsername || log.CreateName == otherUsername)
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
