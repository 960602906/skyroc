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
///     T3 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证用户 CRUD 权限矩阵与最小权限边界。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AuthUserCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/查询/更新/删除/批量删除用户；最小权限用户仅能读取；未认证与无权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task AuthUser_CrudAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedUserReadButtonId = Guid.NewGuid();
        var writeMenuId = Guid.NewGuid();
        var writeCreateButtonId = Guid.NewGuid();
        var writeUpdateButtonId = Guid.NewGuid();
        var writeDeleteButtonId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username/role code/name ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var writeMenuName = $"{batch.Id}W";
        var targetUsername = $"{batch.Id}T";
        var batchTargetUsername = $"{batch.Id}B";
        var expandedUsername = $"{batch.Id}E";
        var password = "SkyRocUserPerm!2026";
        var userAgent = $"SkyRoc-T3-User/{batch.Id}";
        var createName = "T3-AuthUser";

        var userReadPermission = PermissionCodes.System.Users.Read;
        var userCreatePermission = PermissionCodes.System.Users.Create;
        var userUpdatePermission = PermissionCodes.System.Users.Update;
        var userDeletePermission = PermissionCodes.System.Users.Delete;
        var userAssignRolesPermission = PermissionCodes.System.Users.AssignRoles;

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
                Desc = "T3 用户权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T3用户操作员",
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
                    NickName = "T3用户只读用户",
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

            // 最小权限角色初始仅挂用户读取权限；写权限菜单稍后由操作员分配
            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = seedMenuId,
                    Name = seedMenuName,
                    Path = $"/{batch.Id}s",
                    Title = "T3用户只读菜单",
                    Component = "page.t3.user.seed",
                    MenuType = MenuType.Menu,
                    Order = 9401,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T3用户写权限菜单",
                    Component = "page.t3.user.write",
                    MenuType = MenuType.Menu,
                    Order = 9402,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedUserReadButtonId,
                    Code = userReadPermission,
                    Desc = "T3 用户读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = userCreatePermission,
                    Desc = "T3 用户创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = userUpdatePermission,
                    Desc = "T3 用户更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = userDeletePermission,
                    Desc = "T3 用户删除权限按钮",
                    MenuId = writeMenuId,
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
        registry.Register<Menu>(writeMenuId, nameof(Menu.Name), writeMenuName);
        // MenuButton.Code 为权限码、无批次前缀；finally 按主键与 CreateName 兜底。

        Guid? targetUserId = null;
        Guid? batchTargetUserId = null;
        Guid? expandedUserId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问用户接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/users"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/users",
                       new CreateUserDto
                       {
                           Username = $"{batch.Id}X",
                           NickName = "T3未认证创建应拒绝",
                           Gender = GenderType.Male,
                           Phone = "13900008899",
                           Email = $"{batch.Id}-x@skyroc-autotest.example",
                           Password = password,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousCreate, ResponseCode.Unauthorized);
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

            // 操作员创建目标用户
            UserDto targetUser;
            using (var createTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/users",
                       new CreateUserDto
                       {
                           Username = targetUsername,
                           NickName = "T3自动测试目标用户",
                           Gender = GenderType.Male,
                           Phone = "13900008811",
                           Email = $"{batch.Id}-t@skyroc-autotest.example",
                           Password = password,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createTargetResponse.StatusCode);
                targetUser = await ReadApiDataAsync<UserDto>(createTargetResponse);
                Assert.Equal(targetUsername, targetUser.Username);
                Assert.Equal("T3自动测试目标用户", targetUser.NickName);
                targetUserId = targetUser.Id;
                registry.Register<User>(targetUser.Id, nameof(User.Username), targetUsername);
            }

            // 操作员创建批量删除目标用户
            UserDto batchTargetUser;
            using (var createBatchTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/users",
                       new CreateUserDto
                       {
                           Username = batchTargetUsername,
                           NickName = "T3自动测试批量删除用户",
                           Gender = GenderType.Female,
                           Phone = "13900008812",
                           Email = $"{batch.Id}-b@skyroc-autotest.example",
                           Password = password,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBatchTargetResponse.StatusCode);
                batchTargetUser = await ReadApiDataAsync<UserDto>(createBatchTargetResponse);
                Assert.Equal(batchTargetUsername, batchTargetUser.Username);
                batchTargetUserId = batchTargetUser.Id;
                registry.Register<User>(batchTargetUser.Id, nameof(User.Username), batchTargetUsername);
            }

            // 操作员可读列表（按用户名筛选，避免无排序分页漏检）与全量、详情
            using (var listResponse = await adminClient.GetAsync(
                       $"/api/users/list?current=1&size=20&username={Uri.EscapeDataString(targetUsername)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<UserDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == targetUser.Id || item.Username == targetUsername);
            }

            using (var allResponse = await adminClient.GetAsync("/api/users"))
            {
                Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
                // 全量接口仅验证授权与契约成功；存在性以筛选分页与详情为准
            }

            using (var detailResponse = await adminClient.GetAsync($"/api/users/{targetUser.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
                var detail = await ReadApiDataAsync<UserDto>(detailResponse);
                Assert.Equal(targetUser.Id, detail.Id);
                Assert.Equal(targetUsername, detail.Username);
            }

            // 操作员更新目标用户昵称（不改密码）
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/users",
                       new UpdateUserDto
                       {
                           Id = targetUser.Id,
                           Username = targetUsername,
                           NickName = "T3自动测试目标用户-已更新",
                           Gender = GenderType.Male,
                           Phone = "13900008811",
                           Email = $"{batch.Id}-t@skyroc-autotest.example",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                // 控制器返回 ApiResponse<string>.Ok("...")；以业务码为准，避免字符串 Data 反序列化差异
                var payload = await ReadApiResponseAsync<string>(updateResponse);
                Assert.Equal(ResponseCode.Success, payload.Code);
            }

            using (var detailAfterUpdate = await adminClient.GetAsync($"/api/users/{targetUser.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterUpdate.StatusCode);
                var detail = await ReadApiDataAsync<UserDto>(detailAfterUpdate);
                Assert.Equal("T3自动测试目标用户-已更新", detail.NickName);
            }

            // 密码哈希仍可登录（更新未破坏 PasswordHash）
            using (var targetLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = targetUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, targetLogin.StatusCode);
                var login = await ReadApiDataAsync<LoginResDto>(targetLogin);
                Assert.False(string.IsNullOrWhiteSpace(login.Token));
            }

            // 操作员批量删除
            using (var batchDeleteResponse = await adminClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/users/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { batchTargetUser.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, batchDeleteResponse.StatusCode);
            }

            await using (var afterBatchDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterBatchDelete.Users.AnyAsync(user =>
                    user.Id == batchTargetUser.Id || user.Username == batchTargetUsername));
                batchTargetUserId = null;
            }

            // 最小权限用户登录：仅用户读
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

            using (var limitedInfoResponse = await limitedClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, limitedInfoResponse.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(limitedInfoResponse);
                Assert.Equal(limitedUserId, info.UserId);
                Assert.Contains(limitedRoleCode, info.Roles);
                Assert.DoesNotContain(PermissionCodes.All, info.Permissions);
                Assert.Contains(userReadPermission, info.Permissions);
                Assert.DoesNotContain(userCreatePermission, info.Permissions);
                Assert.DoesNotContain(userUpdatePermission, info.Permissions);
                Assert.DoesNotContain(userDeletePermission, info.Permissions);
                Assert.DoesNotContain(userAssignRolesPermission, info.Permissions);
            }

            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
            }

            // 只读允许
            using (var allowedAll = await limitedClient.GetAsync("/api/users"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/users/{targetUser.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var allowedList = await limitedClient.GetAsync("/api/users/list?current=1&size=20"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
            }

            // 无写权限 → 403
            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/users",
                       new CreateUserDto
                       {
                           Username = $"{batch.Id}D",
                           NickName = "T3只读用户创建应拒绝",
                           Gender = GenderType.Male,
                           Phone = "13900008821",
                           Email = $"{batch.Id}-d@skyroc-autotest.example",
                           Password = password,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/users",
                       new UpdateUserDto
                       {
                           Id = targetUser.Id,
                           Username = targetUsername,
                           NickName = "T3只读用户更新应拒绝",
                           Gender = GenderType.Male,
                           Phone = "13900008811",
                           Email = $"{batch.Id}-t@skyroc-autotest.example",
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdate, ResponseCode.Forbidden);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/users/{targetUser.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDelete, ResponseCode.Forbidden);
            }

            using (var deniedBatchDelete = await limitedClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/users/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { targetUser.Id })
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedBatchDelete, ResponseCode.Forbidden);
            }

            using (var deniedAssignRoles = await limitedClient.PostAsJsonAsync(
                       "/api/users/assignRoles",
                       new AssignRolesDto
                       {
                           UserId = targetUser.Id,
                           RoleIds = [limitedRoleId]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAssignRoles, ResponseCode.Forbidden);
            }

            // 扩权：分配写权限菜单后重新登录，应允许创建/更新/删除
            using (var expandMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId, writeMenuId]
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

            using var limitedWriteClient = factory.CreateClient();
            limitedWriteClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedExpandedLogin.Token);
            limitedWriteClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterExpand = await limitedWriteClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterExpand.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterExpand);
                Assert.Contains(userReadPermission, info.Permissions);
                Assert.Contains(userCreatePermission, info.Permissions);
                Assert.Contains(userUpdatePermission, info.Permissions);
                Assert.Contains(userDeletePermission, info.Permissions);
                Assert.DoesNotContain(userAssignRolesPermission, info.Permissions);
            }

            UserDto expandedUser;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/users",
                       new CreateUserDto
                       {
                           Username = expandedUsername,
                           NickName = "T3扩权后创建用户",
                           Gender = GenderType.Male,
                           Phone = "13900008813",
                           Email = $"{batch.Id}-e@skyroc-autotest.example",
                           Password = password,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedUser = await ReadApiDataAsync<UserDto>(createExpanded);
                Assert.Equal(expandedUsername, expandedUser.Username);
                expandedUserId = expandedUser.Id;
                registry.Register<User>(expandedUser.Id, nameof(User.Username), expandedUsername);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/users",
                       new UpdateUserDto
                       {
                           Id = expandedUser.Id,
                           Username = expandedUsername,
                           NickName = "T3扩权后创建用户-已更新",
                           Gender = GenderType.Male,
                           Phone = "13900008813",
                           Email = $"{batch.Id}-e@skyroc-autotest.example",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/users/{expandedUser.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.Users.AnyAsync(user =>
                    user.Id == expandedUser.Id || user.Username == expandedUsername));
                expandedUserId = null;
            }

            // 缩权：仅保留只读菜单后重新登录，写权限与写菜单路径应消失
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
                Assert.Contains(userReadPermission, info.Permissions);
                Assert.DoesNotContain(userCreatePermission, info.Permissions);
                Assert.DoesNotContain(userUpdatePermission, info.Permissions);
                Assert.DoesNotContain(userDeletePermission, info.Permissions);
            }

            using (var routesAfterShrink = await limitedReloginClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesAfterShrink.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesAfterShrink);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
            }

            using (var deniedCreateAfterShrink = await limitedReloginClient.PostAsJsonAsync(
                       "/api/users",
                       new CreateUserDto
                       {
                           Username = $"{batch.Id}F",
                           NickName = "T3缩权后创建应拒绝",
                           Gender = GenderType.Male,
                           Phone = "13900008822",
                           Email = $"{batch.Id}-f@skyroc-autotest.example",
                           Password = password,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateAfterShrink, ResponseCode.Forbidden);
            }

            // 操作员删除目标用户
            using (var deleteTarget = await adminClient.DeleteAsync($"/api/users/{targetUser.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteTarget.StatusCode);
            }

            await using (var afterDeleteTarget = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteTarget.Users.AnyAsync(user =>
                    user.Id == targetUser.Id || user.Username == targetUsername));
                targetUserId = null;
            }

            await using var auditContext = fixture.CreateDbContext();
            var loginLogs = await auditContext.LoginLogs.AsNoTracking()
                .Where(log => log.Username == adminUsername
                              || log.Username == limitedUsername
                              || log.Username == targetUsername)
                .ToListAsync();
            Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
            Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
            Assert.Contains(loginLogs, log => log.Username == targetUsername && log.IsSuccess);
            Assert.All(loginLogs, log =>
            {
                Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
            });
            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername, targetUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(
                fixture,
                registry,
                adminUsername,
                limitedUsername,
                targetUsername,
                batchTargetUsername,
                expandedUsername,
                $"{batch.Id}X",
                $"{batch.Id}D",
                $"{batch.Id}F");
            await RegisterBatchOperationLogsAsync(
                fixture,
                registry,
                adminUsername,
                limitedUsername,
                targetUsername,
                batchTargetUsername,
                expandedUsername);

            // 先清本轮批次登记实体；UserRole 随用户级联，RoleMenu 随角色/菜单级联
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            // 兜底：临时用户、菜单按钮、残留关系（不触碰既有 Admin 角色）
            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUsernames = new[]
                {
                    adminUsername,
                    limitedUsername,
                    targetUsername,
                    batchTargetUsername,
                    expandedUsername,
                    $"{batch.Id}X",
                    $"{batch.Id}D",
                    $"{batch.Id}F"
                };

                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };
                if (targetUserId.HasValue)
                    residualUserIds.Add(targetUserId.Value);
                if (batchTargetUserId.HasValue)
                    residualUserIds.Add(batchTargetUserId.Value);
                if (expandedUserId.HasValue)
                    residualUserIds.Add(expandedUserId.Value);

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
                    .Where(relation => relation.RoleId == limitedRoleId
                                       || relation.MenuId == seedMenuId
                                       || relation.MenuId == writeMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtonIds = new List<Guid>
                {
                    seedUserReadButtonId,
                    writeCreateButtonId,
                    writeUpdateButtonId,
                    writeDeleteButtonId
                };
                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => residualButtonIds.Contains(button.Id)
                                     || button.MenuId == seedMenuId
                                     || button.MenuId == writeMenuId
                                     || button.CreateName == createName)
                    .ToListAsync();
                if (residualButtons.Count > 0)
                {
                    cleanupContext.MenuButtons.RemoveRange(residualButtons);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualMenus = await cleanupContext.Menus
                    .Where(menu => menu.Id == seedMenuId
                                   || menu.Id == writeMenuId
                                   || menu.Name == seedMenuName
                                   || menu.Name == writeMenuName)
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
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername
                || user.Username == limitedUsername
                || user.Username == targetUsername
                || user.Username == batchTargetUsername
                || user.Username == expandedUsername
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId || role.Code == limitedRoleCode));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Id == seedMenuId
                || menu.Id == writeMenuId
                || menu.Name == seedMenuName
                || menu.Name == writeMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedUserReadButtonId
                || button.Id == writeCreateButtonId
                || button.Id == writeUpdateButtonId
                || button.Id == writeDeleteButtonId
                || button.CreateName == createName));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == adminUserId || relation.UserId == limitedUserId));
            Assert.False(await residualContext.RoleMenus.AnyAsync(relation =>
                relation.RoleId == limitedRoleId
                || relation.MenuId == seedMenuId
                || relation.MenuId == writeMenuId));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || log.Username == targetUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || log.CreateName == targetUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
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
