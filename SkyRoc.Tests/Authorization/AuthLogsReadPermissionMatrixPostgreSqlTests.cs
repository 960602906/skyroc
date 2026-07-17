using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Role;
using Application.DTOs.System;
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
///     T3 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证操作/登录审计只读权限矩阵与脱敏边界。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AuthLogsReadPermissionMatrixPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     未认证拒绝；Admin 可查操作/登录日志；最小权限仅相邻部门读时日志 403；扩权 system:log:read 后可查并脱敏；缩权后 403；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task AuthLogs_ReadPermissionMatrixAndDesensitization_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedDeptReadButtonId = Guid.NewGuid();
        var logMenuId = Guid.NewGuid();
        var logReadButtonId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username/role code/name ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var logMenuName = $"{batch.Id}G";
        var password = "SkyRocLogPerm!2026";
        var userAgent = $"SkyRoc-T3-Logs/{batch.Id}";
        var createName = "T3-AuthLogs";

        var deptReadPermission = PermissionCodes.System.Departments.Read;
        var logReadPermission = PermissionCodes.System.Logs.Read;

        Guid adminRoleId;
        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T3 审计日志只读权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T3日志操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008801",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T3日志最小权限用户",
                    Gender = GenderType.Female,
                    Phone = "13900008802",
                    Email = $"{batch.Id}-l@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.UserRoles.AddRangeAsync(
                new UserRole
                {
                    UserId = adminUserId,
                    RoleId = adminRoleId
                },
                new UserRole
                {
                    UserId = limitedUserId,
                    RoleId = limitedRoleId
                });

            // 最小权限角色初始仅挂相邻部门读取权限，明确不含 system:log:read
            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = seedMenuId,
                    Name = seedMenuName,
                    Path = $"/{batch.Id}s",
                    Title = "T3部门只读相邻菜单",
                    Component = "page.t3.logs.seed",
                    MenuType = MenuType.Menu,
                    Order = 9401,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = logMenuId,
                    Name = logMenuName,
                    Path = $"/{batch.Id}g",
                    Title = "T3审计日志只读菜单",
                    Component = "page.t3.logs.read",
                    MenuType = MenuType.Menu,
                    Order = 9402,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedDeptReadButtonId,
                    Code = deptReadPermission,
                    Desc = "T3 部门读取相邻权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = logReadButtonId,
                    Code = logReadPermission,
                    Desc = "T3 审计日志读取权限按钮",
                    MenuId = logMenuId,
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
        registry.Register<Menu>(logMenuId, nameof(Menu.Name), logMenuName);
        // MenuButton.Code 为权限码、无批次前缀；finally 按主键与 CreateName 兜底。

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问日志接口
            using (var anonymousOperations = await anonymousClient.GetAsync("/api/logs/operations?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousOperations, ResponseCode.Unauthorized);
            }

            using (var anonymousLogins = await anonymousClient.GetAsync("/api/logs/logins?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousLogins, ResponseCode.Unauthorized);
            }

            // 错误密码登录：写入失败审计，供后续脱敏断言
            using (var badLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = "Definitely-Wrong-Password!"
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(badLogin, ResponseCode.DatabaseError);
            }

            // 操作员登录（Admin → *:*:*）
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

            using (var adminInfo = await adminClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, adminInfo.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(adminInfo);
                Assert.Equal(adminUserId, info.UserId);
                Assert.Contains(PermissionCodes.All, info.Permissions);
            }

            // 操作员可读操作日志，并按关键字命中本轮用户名
            using (var opsResponse = await adminClient.GetAsync(
                       $"/api/logs/operations?current=1&size=50&keyword={Uri.EscapeDataString(adminUsername)}"))
            {
                Assert.Equal(HttpStatusCode.OK, opsResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<OperationLogDto>>(opsResponse);
                Assert.NotNull(page.Records);
                Assert.True(page.Total >= 0);
                // 读接口通常不写操作审计；至少保证响应契约与脱敏
                Assert.All(page.Records!, log =>
                {
                    AssertDoesNotLeakSecrets(log.RequestParams);
                    AssertDoesNotLeakSecrets(log.ResponseResult);
                    AssertDoesNotLeakSecrets(log.ErrorMessage);
                    AssertDoesNotLeakSecrets(log.Url);
                    Assert.DoesNotContain(password, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain(password, log.ResponseResult ?? string.Empty, StringComparison.Ordinal);
                });
            }

            // 操作员可读登录日志，并按用户名筛选本轮失败/成功记录
            using (var loginsResponse = await adminClient.GetAsync(
                       $"/api/logs/logins?current=1&size=50&username={Uri.EscapeDataString(limitedUsername)}"))
            {
                Assert.Equal(HttpStatusCode.OK, loginsResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<LoginLogDto>>(loginsResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, log =>
                    log.Username == limitedUsername && !log.IsSuccess);
                Assert.All(page.Records!, log =>
                {
                    Assert.Equal(limitedUsername, log.Username);
                    AssertDoesNotLeakSecrets(log.FailureReason);
                    Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain("Definitely-Wrong-Password!", log.FailureReason ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain(adminLogin.Token, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
                });
            }

            using (var adminLogins = await adminClient.GetAsync(
                       $"/api/logs/logins?current=1&size=20&username={Uri.EscapeDataString(adminUsername)}&isSuccess=true"))
            {
                Assert.Equal(HttpStatusCode.OK, adminLogins.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<LoginLogDto>>(adminLogins);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, log => log.Username == adminUsername && log.IsSuccess);
            }

            // 触发一次写操作，确保操作审计中存在本轮 CreateName=adminUsername 的记录，便于权限与脱敏核对
            using (var assignMenusNoop = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignMenusNoop.StatusCode);
            }

            await using (var opsDb = fixture.CreateDbContext())
            {
                var adminOps = await opsDb.OperationLogs.AsNoTracking()
                    .Where(log => log.CreateName == adminUsername)
                    .OrderByDescending(log => log.CreateTime)
                    .Take(20)
                    .ToListAsync();
                Assert.NotEmpty(adminOps);
                Assert.All(adminOps, log =>
                {
                    AssertDoesNotLeakSecrets(log.RequestParams);
                    AssertDoesNotLeakSecrets(log.ResponseResult);
                    Assert.DoesNotContain(password, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                });
            }

            using (var opsAfterWrite = await adminClient.GetAsync(
                       $"/api/logs/operations?current=1&size=50&keyword={Uri.EscapeDataString(adminUsername)}"))
            {
                Assert.Equal(HttpStatusCode.OK, opsAfterWrite.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<OperationLogDto>>(opsAfterWrite);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, log =>
                    string.Equals(log.CreateName, adminUsername, StringComparison.Ordinal)
                    || (log.RequestParams?.Contains(limitedRoleId.ToString(), StringComparison.OrdinalIgnoreCase) ?? false)
                    || (log.Url?.Contains("/api/roles/assignMenus", StringComparison.OrdinalIgnoreCase) ?? false));
                Assert.All(page.Records!, log =>
                {
                    AssertDoesNotLeakSecrets(log.RequestParams);
                    Assert.DoesNotContain(password, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain(adminLogin.Token, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                });
            }

            // 最小权限用户：仅相邻部门读，日志接口 403
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
                Assert.Equal(HttpStatusCode.OK, limitedInfo.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(limitedInfo);
                Assert.Equal(limitedUserId, info.UserId);
                Assert.Contains(limitedRoleCode, info.Roles);
                Assert.DoesNotContain(PermissionCodes.All, info.Permissions);
                Assert.Contains(deptReadPermission, info.Permissions);
                Assert.DoesNotContain(logReadPermission, info.Permissions);
            }

            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}g", paths);
            }

            // 相邻权限可用，证明不是整站 403
            using (var allowedDeptTree = await limitedClient.GetAsync("/api/departments/tree"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDeptTree.StatusCode);
            }

            using (var deniedOps = await limitedClient.GetAsync("/api/logs/operations?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedOps, ResponseCode.Forbidden);
            }

            using (var deniedLogins = await limitedClient.GetAsync("/api/logs/logins?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedLogins, ResponseCode.Forbidden);
            }

            // 扩权：分配日志只读菜单后重新登录
            using (var expandMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId, logMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, expandMenus.StatusCode);
            }

            LoginResDto limitedExpandedLogin;
            using (var expandLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, expandLoginResponse.StatusCode);
                limitedExpandedLogin = await ReadApiDataAsync<LoginResDto>(expandLoginResponse);
            }

            using var limitedReadClient = factory.CreateClient();
            limitedReadClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedExpandedLogin.Token);
            limitedReadClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterExpand = await limitedReadClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterExpand.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterExpand);
                Assert.Contains(deptReadPermission, info.Permissions);
                Assert.Contains(logReadPermission, info.Permissions);
            }

            using (var routesAfterExpand = await limitedReadClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesAfterExpand.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesAfterExpand);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.Contains($"/{batch.Id}g", paths);
            }

            using (var allowedOps = await limitedReadClient.GetAsync(
                       $"/api/logs/operations?current=1&size=20&keyword={Uri.EscapeDataString(adminUsername)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedOps.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<OperationLogDto>>(allowedOps);
                Assert.NotNull(page.Records);
                Assert.All(page.Records!, log =>
                {
                    AssertDoesNotLeakSecrets(log.RequestParams);
                    Assert.DoesNotContain(password, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                });
            }

            using (var allowedLogins = await limitedReadClient.GetAsync(
                       $"/api/logs/logins?current=1&size=20&username={Uri.EscapeDataString(limitedUsername)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedLogins.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<LoginLogDto>>(allowedLogins);
                Assert.NotNull(page.Records);
                Assert.NotEmpty(page.Records!);
                Assert.Contains(page.Records!, log => log.Username == limitedUsername);
                Assert.All(page.Records!, log =>
                {
                    AssertDoesNotLeakSecrets(log.FailureReason);
                    Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
                });
            }

            // 缩权：仅保留相邻部门读
            using (var shrinkMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, shrinkMenus.StatusCode);
            }

            LoginResDto limitedRelogin;
            using (var reloginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, reloginResponse.StatusCode);
                limitedRelogin = await ReadApiDataAsync<LoginResDto>(reloginResponse);
            }

            using var limitedReloginClient = factory.CreateClient();
            limitedReloginClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedRelogin.Token);
            limitedReloginClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterShrink = await limitedReloginClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterShrink.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterShrink);
                Assert.Contains(deptReadPermission, info.Permissions);
                Assert.DoesNotContain(logReadPermission, info.Permissions);
            }

            using (var routesAfterShrink = await limitedReloginClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesAfterShrink.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesAfterShrink);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}g", paths);
            }

            using (var deniedOpsAfterShrink = await limitedReloginClient.GetAsync("/api/logs/operations?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedOpsAfterShrink, ResponseCode.Forbidden);
            }

            using (var deniedLoginsAfterShrink = await limitedReloginClient.GetAsync("/api/logs/logins?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedLoginsAfterShrink, ResponseCode.Forbidden);
            }

            await using var auditContext = fixture.CreateDbContext();
            var loginLogs = await auditContext.LoginLogs.AsNoTracking()
                .Where(log => log.Username == adminUsername || log.Username == limitedUsername)
                .ToListAsync();
            Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
            Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
            Assert.Contains(loginLogs, log => log.Username == limitedUsername && !log.IsSuccess);
            Assert.All(loginLogs, log =>
            {
                AssertDoesNotLeakSecrets(log.FailureReason);
                Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
            });
            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUserRoles = await cleanupContext.UserRoles
                    .Where(relation => relation.UserId == adminUserId || relation.UserId == limitedUserId)
                    .ToListAsync();
                if (residualUserRoles.Count > 0)
                {
                    cleanupContext.UserRoles.RemoveRange(residualUserRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoleMenus = await cleanupContext.RoleMenus
                    .Where(relation => relation.RoleId == limitedRoleId
                                       || relation.MenuId == seedMenuId
                                       || relation.MenuId == logMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtonIds = new List<Guid>
                {
                    seedDeptReadButtonId,
                    logReadButtonId
                };
                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => residualButtonIds.Contains(button.Id)
                                     || button.MenuId == seedMenuId
                                     || button.MenuId == logMenuId
                                     || button.CreateName == createName)
                    .ToListAsync();
                if (residualButtons.Count > 0)
                {
                    cleanupContext.MenuButtons.RemoveRange(residualButtons);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualMenus = await cleanupContext.Menus
                    .Where(menu => menu.Id == seedMenuId
                                   || menu.Id == logMenuId
                                   || menu.Name == seedMenuName
                                   || menu.Name == logMenuName)
                    .ToListAsync();
                if (residualMenus.Count > 0)
                {
                    cleanupContext.Menus.RemoveRange(residualMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRole = await cleanupContext.Roles
                    .FirstOrDefaultAsync(role => role.Id == limitedRoleId);
                if (residualRole is not null)
                {
                    cleanupContext.Roles.Remove(residualRole);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualUsers = await cleanupContext.Users
                    .Where(user => user.Username == adminUsername || user.Username == limitedUsername)
                    .ToListAsync();
                if (residualUsers.Count > 0)
                {
                    cleanupContext.Users.RemoveRange(residualUsers);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername || user.Username == limitedUsername));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId || role.Code == limitedRoleCode));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Id == seedMenuId
                || menu.Id == logMenuId
                || menu.Name == seedMenuName
                || menu.Name == logMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedDeptReadButtonId
                || button.Id == logReadButtonId
                || button.CreateName == createName));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == adminUserId || relation.UserId == limitedUserId));
            Assert.False(await residualContext.RoleMenus.AnyAsync(relation =>
                relation.RoleId == limitedRoleId
                || relation.MenuId == seedMenuId
                || relation.MenuId == logMenuId));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername || log.Username == limitedUsername));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername || log.CreateName == limitedUsername));
            // 既有 Admin 角色必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
        }
    }

    private static void AssertDoesNotLeakSecrets(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        // 禁止明文 Bearer 访问令牌片段；键名 password=*** 属于合法脱敏结果
        Assert.DoesNotContain("Bearer ", value, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJ", value, StringComparison.Ordinal);
    }

    private static IEnumerable<string> FlattenRoutePaths(IEnumerable<RoutesDto> routes)
    {
        foreach (var route in routes)
        {
            if (!string.IsNullOrWhiteSpace(route.Path))
                yield return route.Path!;
            if (route.Children is null)
                continue;
            foreach (var childPath in FlattenRoutePaths(route.Children))
                yield return childPath;
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
                // 已登记则跳过
            }
        }
    }

    private static async Task RegisterResidualLoginLogsAsync(
        PostgreSqlTestFixture fixture,
        BatchCleanupRegistry registry,
        string adminUsername,
        string limitedUsername)
    {
        await using var context = fixture.CreateDbContext();
        var residualLogs = await context.LoginLogs.AsNoTracking()
            .Where(log => log.Username == adminUsername || log.Username == limitedUsername)
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
