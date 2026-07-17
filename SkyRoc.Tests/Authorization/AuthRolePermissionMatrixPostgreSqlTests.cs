using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Role;
using Application.DTOs.User;
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
///     T3 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证角色菜单分配、用户角色分配及最小权限/通配权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AuthRolePermissionMatrixPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可分配角色菜单与用户角色；最小权限用户仅能访问授权读接口，通配角色持有 *:*:* 可跨资源访问；未认证与无权限均拒绝；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task AuthRolePermission_AssignMenusRolesAndEnforceMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var readMenuId = Guid.NewGuid();
        var writeMenuId = Guid.NewGuid();
        var readButtonId = Guid.NewGuid();
        var writeButtonId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username/role code/name ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var readMenuName = $"{batch.Id}M1";
        var writeMenuName = $"{batch.Id}M2";
        var extraUsername = $"{batch.Id}X";
        var deniedUsername = $"{batch.Id}Y";
        var password = "SkyRocPermMatrix!2026";
        var userAgent = $"SkyRoc-T3-Perm/{batch.Id}";
        var createName = "T3-AuthRolePerm";

        var readPermission = PermissionCodes.System.Users.Read;
        var writePermission = PermissionCodes.System.Users.Create;
        var assignMenusPermission = PermissionCodes.System.Roles.AssignMenus;
        var assignRolesPermission = PermissionCodes.System.Users.AssignRoles;
        var rolesReadPermission = PermissionCodes.System.Roles.Read;

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
                Desc = "T3 权限矩阵最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T3权限操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008881",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T3最小权限用户",
                    Gender = GenderType.Female,
                    Phone = "13900008882",
                    Email = $"{batch.Id}-l@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                });

            // 操作员绑定既有 Admin 角色；最小权限用户先不绑角色，后续经 HTTP 分配
            await seedContext.UserRoles.AddAsync(new UserRole
            {
                UserId = adminUserId,
                RoleId = adminRoleId
            });

            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = readMenuId,
                    Name = readMenuName,
                    Path = $"/{batch.Id}r",
                    Title = "T3最小读权限菜单",
                    Component = "page.t3.perm.read",
                    MenuType = MenuType.Menu,
                    Order = 9101,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T3写权限菜单",
                    Component = "page.t3.perm.write",
                    MenuType = MenuType.Menu,
                    Order = 9102,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = readButtonId,
                    Code = readPermission,
                    Desc = "T3 用户读取权限按钮",
                    MenuId = readMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeButtonId,
                    Code = writePermission,
                    Desc = "T3 用户创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                });

            // 最小权限角色初始只挂读菜单；写菜单后续由操作员 HTTP 分配
            await seedContext.RoleMenus.AddAsync(new RoleMenu
            {
                RoleId = limitedRoleId,
                MenuId = readMenuId
            });

            await seedContext.SaveChangesAsync();
        }

        registry.Register<Role>(limitedRoleId, nameof(Role.Code), limitedRoleCode);
        registry.Register<User>(adminUserId, nameof(User.Username), adminUsername);
        registry.Register<User>(limitedUserId, nameof(User.Username), limitedUsername);
        registry.Register<Menu>(readMenuId, nameof(Menu.Name), readMenuName);
        registry.Register<Menu>(writeMenuId, nameof(Menu.Name), writeMenuName);
        // MenuButton.Code 为权限码、无批次前缀；随菜单级联删除，finally 再按主键兜底。

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问受保护分配接口
            using (var anonymousAssignMenus = await anonymousClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto { RoleId = limitedRoleId, MenuIds = [readMenuId] }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousAssignMenus, ResponseCode.Unauthorized);
            }

            using (var anonymousAssignRoles = await anonymousClient.PostAsJsonAsync(
                       "/api/users/assignRoles",
                       new AssignRolesDto { UserId = limitedUserId, RoleIds = [limitedRoleId] }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousAssignRoles, ResponseCode.Unauthorized);
            }

            using (var anonymousUsers = await anonymousClient.GetAsync("/api/users"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousUsers, ResponseCode.Unauthorized);
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

            // 通配权限：getUserInfo 含 *:*:*
            using (var adminInfoResponse = await adminClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, adminInfoResponse.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(adminInfoResponse);
                Assert.Equal(adminUserId, info.UserId);
                Assert.Contains(SeedConstants.AdminRoleCode, info.Roles, StringComparer.OrdinalIgnoreCase);
                Assert.Contains(PermissionCodes.All, info.Permissions);
                Assert.Contains(PermissionCodes.All, info.Buttons);
            }

            // 通配可访问任意受保护系统接口
            using (var adminUsersResponse = await adminClient.GetAsync("/api/users"))
            {
                Assert.Equal(HttpStatusCode.OK, adminUsersResponse.StatusCode);
            }

            using (var adminRolesResponse = await adminClient.GetAsync("/api/roles"))
            {
                Assert.Equal(HttpStatusCode.OK, adminRolesResponse.StatusCode);
            }

            // 操作员为最小权限角色分配读+写菜单（整体替换）
            using (var assignMenusResponse = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [readMenuId, writeMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignMenusResponse.StatusCode);
                var payload = await ReadApiResponseAsync<string>(assignMenusResponse);
                Assert.Equal(ResponseCode.Success, payload.Code);
            }

            await using (var afterAssignMenus = fixture.CreateDbContext())
            {
                var roleMenus = await afterAssignMenus.RoleMenus.AsNoTracking()
                    .Where(relation => relation.RoleId == limitedRoleId)
                    .ToListAsync();
                Assert.Equal(2, roleMenus.Count);
                Assert.Contains(roleMenus, relation => relation.MenuId == readMenuId);
                Assert.Contains(roleMenus, relation => relation.MenuId == writeMenuId);
            }

            using (var roleDetailResponse = await adminClient.GetAsync($"/api/roles/{limitedRoleId}"))
            {
                Assert.Equal(HttpStatusCode.OK, roleDetailResponse.StatusCode);
                var roleDetail = await ReadApiDataAsync<RoleDto>(roleDetailResponse);
                Assert.NotNull(roleDetail.Menu);
                Assert.Equal(2, roleDetail.Menu!.Count);
                Assert.Contains(roleDetail.Menu, menu => menu.Id == readMenuId);
                Assert.Contains(roleDetail.Menu, menu => menu.Id == writeMenuId);
            }

            // 操作员给最小权限用户分配角色
            using (var assignRolesResponse = await adminClient.PostAsJsonAsync(
                       "/api/users/assignRoles",
                       new AssignRolesDto
                       {
                           UserId = limitedUserId,
                           RoleIds = [limitedRoleId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignRolesResponse.StatusCode);
                var payload = await ReadApiResponseAsync<string>(assignRolesResponse);
                Assert.Equal(ResponseCode.Success, payload.Code);
            }

            await using (var afterAssignRoles = fixture.CreateDbContext())
            {
                var userRoles = await afterAssignRoles.UserRoles.AsNoTracking()
                    .Where(relation => relation.UserId == limitedUserId)
                    .ToListAsync();
                Assert.Single(userRoles);
                Assert.Equal(limitedRoleId, userRoles[0].RoleId);
            }

            using (var userDetailResponse = await adminClient.GetAsync($"/api/users/{limitedUserId}"))
            {
                Assert.Equal(HttpStatusCode.OK, userDetailResponse.StatusCode);
                var userDetail = await ReadApiDataAsync<UserDto>(userDetailResponse);
                Assert.Equal(limitedRoleId, userDetail.RoleId);
                Assert.NotNull(userDetail.Role);
                Assert.Equal(limitedRoleCode, userDetail.Role!.Code);
            }

            // 最小权限用户登录：应拿到读+写按钮权限，不含 *:*:*
            LoginResDto limitedLogin;
            using (var limitedLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, limitedLoginResponse.StatusCode);
                limitedLogin = await ReadApiDataAsync<LoginResDto>(limitedLoginResponse);
                Assert.False(string.IsNullOrWhiteSpace(limitedLogin.Token));
            }

            using var limitedClient = factory.CreateClient();
            limitedClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedLogin.Token);
            limitedClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var limitedInfoResponse = await limitedClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, limitedInfoResponse.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(limitedInfoResponse);
                Assert.Equal(limitedUserId, info.UserId);
                Assert.Contains(limitedRoleCode, info.Roles);
                Assert.DoesNotContain(PermissionCodes.All, info.Permissions);
                Assert.Contains(readPermission, info.Permissions);
                Assert.Contains(writePermission, info.Permissions);
                Assert.DoesNotContain(assignMenusPermission, info.Permissions);
                Assert.DoesNotContain(assignRolesPermission, info.Permissions);
                Assert.DoesNotContain(rolesReadPermission, info.Permissions);
            }

            // getRoutes 应包含已分配的两个菜单路径
            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                Assert.Equal("/home", routes.Home);
                Assert.NotNull(routes.Routes);
                var paths = FlattenRoutePaths(routes.Routes!).ToList();
                Assert.Contains($"/{batch.Id}r", paths);
                Assert.Contains($"/{batch.Id}w", paths);
            }

            // 最小权限：可读用户列表
            using (var allowedRead = await limitedClient.GetAsync("/api/users"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedRead.StatusCode);
            }

            // 最小权限：持有 system:user:create 时创建接口不得因授权失败
            using (var allowedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/users",
                       new CreateUserDto
                       {
                           Username = extraUsername,
                           NickName = "T3矩阵旁路用户",
                           Gender = GenderType.Male,
                           Phone = "13900008883",
                           Email = $"{batch.Id}x@skyroc-autotest.example",
                           Password = password,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, allowedCreate.StatusCode);
                var createdPayload = await ReadApiResponseAsync<UserDto>(allowedCreate);
                Assert.NotEqual(ResponseCode.Forbidden, createdPayload.Code);
                Assert.NotEqual(ResponseCode.Unauthorized, createdPayload.Code);
                if (createdPayload.Code == ResponseCode.Success)
                {
                    Assert.NotNull(createdPayload.Data);
                    var created = createdPayload.Data;
                    Assert.Equal(extraUsername, created.Username);
                    registry.Register<User>(created.Id, nameof(User.Username), created.Username!);
                }
            }

            // 无角色读权限 → 403
            using (var deniedRoles = await limitedClient.GetAsync("/api/roles"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedRoles, ResponseCode.Forbidden);
            }

            // 无分配菜单权限 → 403
            using (var deniedAssignMenus = await limitedClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [readMenuId]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAssignMenus, ResponseCode.Forbidden);
            }

            // 无分配角色权限 → 403
            using (var deniedAssignRoles = await limitedClient.PostAsJsonAsync(
                       "/api/users/assignRoles",
                       new AssignRolesDto
                       {
                           UserId = limitedUserId,
                           RoleIds = [limitedRoleId]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAssignRoles, ResponseCode.Forbidden);
            }

            // 将最小角色缩回仅读菜单，重新登录后写权限应消失
            using (var shrinkMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [readMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, shrinkMenus.StatusCode);
            }

            await using (var afterShrink = fixture.CreateDbContext())
            {
                var roleMenus = await afterShrink.RoleMenus.AsNoTracking()
                    .Where(relation => relation.RoleId == limitedRoleId)
                    .ToListAsync();
                Assert.Single(roleMenus);
                Assert.Equal(readMenuId, roleMenus[0].MenuId);
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
                Assert.Contains(readPermission, info.Permissions);
                Assert.DoesNotContain(writePermission, info.Permissions);
                Assert.DoesNotContain(PermissionCodes.All, info.Permissions);
            }

            using (var createAfterShrink = await limitedReloginClient.PostAsJsonAsync(
                       "/api/users",
                       new CreateUserDto
                       {
                           Username = deniedUsername,
                           NickName = "T3缩权后应拒绝",
                           Gender = GenderType.Female,
                           Phone = "13900008884",
                           Email = $"{batch.Id}y@skyroc-autotest.example",
                           Password = password,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(createAfterShrink, ResponseCode.Forbidden);
            }

            // 移除用户角色后，再登录权限应为空且受保护接口 403
            using (var unassignRoles = await adminClient.SendAsync(new HttpRequestMessage(
                       HttpMethod.Delete,
                       "/api/users/unassignRoles")
            {
                Content = JsonContent.Create(new AssignRolesDto
                {
                    UserId = limitedUserId,
                    RoleIds = [limitedRoleId]
                })
            }))
            {
                Assert.Equal(HttpStatusCode.OK, unassignRoles.StatusCode);
            }

            await using (var afterUnassign = fixture.CreateDbContext())
            {
                Assert.False(await afterUnassign.UserRoles.AnyAsync(relation =>
                    relation.UserId == limitedUserId));
            }

            LoginResDto noRoleLogin;
            using (var noRoleLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, noRoleLoginResponse.StatusCode);
                noRoleLogin = await ReadApiDataAsync<LoginResDto>(noRoleLoginResponse);
            }

            using var noRoleClient = factory.CreateClient();
            noRoleClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, noRoleLogin.Token);
            noRoleClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var noRoleInfo = await noRoleClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, noRoleInfo.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(noRoleInfo);
                Assert.Empty(info.Roles);
                Assert.Empty(info.Permissions);
            }

            using (var noRoleUsers = await noRoleClient.GetAsync("/api/users"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(noRoleUsers, ResponseCode.Forbidden);
            }

            // 无效角色/菜单分配应失败
            using (var invalidRoleAssign = await adminClient.PostAsJsonAsync(
                       "/api/users/assignRoles",
                       new AssignRolesDto
                       {
                           UserId = limitedUserId,
                           RoleIds = [Guid.NewGuid()]
                       }))
            {
                var invalidRoleCode = await ApiHttpAssert.ReadBusinessCodeAsync(invalidRoleAssign);
                Assert.True(
                    invalidRoleCode is ResponseCode.DatabaseError
                        or ResponseCode.BadRequest
                        or ResponseCode.InternalError
                        or ResponseCode.ValidationError,
                    $"Unexpected business code for invalid role assign: {invalidRoleCode}");
            }

            using (var invalidMenuAssign = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [Guid.NewGuid()]
                       }))
            {
                var invalidMenuCode = await ApiHttpAssert.ReadBusinessCodeAsync(invalidMenuAssign);
                Assert.True(
                    invalidMenuCode is ResponseCode.DatabaseError
                        or ResponseCode.BadRequest
                        or ResponseCode.InternalError
                        or ResponseCode.ValidationError,
                    $"Unexpected business code for invalid menu assign: {invalidMenuCode}");
            }

            await using var auditContext = fixture.CreateDbContext();
            var loginLogs = await auditContext.LoginLogs.AsNoTracking()
                .Where(log => log.Username == adminUsername || log.Username == limitedUsername)
                .ToListAsync();
            Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
            Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
            Assert.All(loginLogs, log =>
            {
                Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
            });
            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername, extraUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername, extraUsername);

            // 先清本轮批次登记实体；UserRole 随用户级联，RoleMenu 随角色/菜单级联
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            // 兜底：临时菜单按钮、残留关系与旁路用户（不触碰既有 Admin 角色）
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
                                       || relation.MenuId == readMenuId
                                       || relation.MenuId == writeMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => button.Id == readButtonId || button.Id == writeButtonId)
                    .ToListAsync();
                if (residualButtons.Count > 0)
                {
                    cleanupContext.MenuButtons.RemoveRange(residualButtons);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualMenus = await cleanupContext.Menus
                    .Where(menu => menu.Id == readMenuId || menu.Id == writeMenuId)
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
                    .Where(user => user.Username == adminUsername
                                   || user.Username == limitedUsername
                                   || user.Username == extraUsername
                                   || user.Username == deniedUsername)
                    .ToListAsync();
                if (residualUsers.Count > 0)
                {
                    cleanupContext.Users.RemoveRange(residualUsers);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername
                || user.Username == limitedUsername
                || user.Username == extraUsername
                || user.Username == deniedUsername));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId || role.Code == limitedRoleCode));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Id == readMenuId || menu.Id == writeMenuId));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == readButtonId || button.Id == writeButtonId));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == adminUserId || relation.UserId == limitedUserId));
            Assert.False(await residualContext.RoleMenus.AnyAsync(relation =>
                relation.RoleId == limitedRoleId
                || relation.MenuId == readMenuId
                || relation.MenuId == writeMenuId));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername || log.Username == limitedUsername));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername || log.CreateName == limitedUsername));
            // 既有 Admin 角色必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
        }
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
