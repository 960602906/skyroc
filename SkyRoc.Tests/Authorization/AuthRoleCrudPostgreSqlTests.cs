using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Role;
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
///     T3 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证角色 CRUD 权限矩阵与最小权限边界。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AuthRoleCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/查询/更新/删除/批量删除角色；最小权限用户仅能读取；未认证与无权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task AuthRole_CrudAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedRoleReadButtonId = Guid.NewGuid();
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
        var targetRoleCode = $"{batch.Id}T";
        var targetRoleName = $"{batch.Id}TN";
        var batchTargetRoleCode = $"{batch.Id}B";
        var batchTargetRoleName = $"{batch.Id}BN";
        var expandedRoleCode = $"{batch.Id}E";
        var expandedRoleName = $"{batch.Id}EN";
        var deniedRoleCode = $"{batch.Id}D";
        var deniedRoleName = $"{batch.Id}DN";
        var password = "SkyRocRolePerm!2026";
        var userAgent = $"SkyRoc-T3-Role/{batch.Id}";
        var createName = "T3-AuthRole";

        var roleReadPermission = PermissionCodes.System.Roles.Read;
        var roleCreatePermission = PermissionCodes.System.Roles.Create;
        var roleUpdatePermission = PermissionCodes.System.Roles.Update;
        var roleDeletePermission = PermissionCodes.System.Roles.Delete;
        var roleAssignMenusPermission = PermissionCodes.System.Roles.AssignMenus;

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
                Desc = "T3 角色权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T3角色操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008701",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T3角色只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900008702",
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

            // 最小权限角色初始仅挂角色读取权限；写权限菜单稍后由操作员分配
            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = seedMenuId,
                    Name = seedMenuName,
                    Path = $"/{batch.Id}s",
                    Title = "T3角色只读菜单",
                    Component = "page.t3.role.seed",
                    MenuType = MenuType.Menu,
                    Order = 9501,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T3角色写权限菜单",
                    Component = "page.t3.role.write",
                    MenuType = MenuType.Menu,
                    Order = 9502,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedRoleReadButtonId,
                    Code = roleReadPermission,
                    Desc = "T3 角色读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = roleCreatePermission,
                    Desc = "T3 角色创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = roleUpdatePermission,
                    Desc = "T3 角色更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = roleDeletePermission,
                    Desc = "T3 角色删除权限按钮",
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

        Guid? targetRoleId = null;
        Guid? batchTargetRoleId = null;
        Guid? expandedRoleId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问角色接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/roles"))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousList.StatusCode);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/roles",
                       new CreateRoleDto
                       {
                           Code = $"{batch.Id}X",
                           Name = $"{batch.Id}XN",
                           Desc = "T3未认证创建应拒绝",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousCreate.StatusCode);
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

            // 操作员创建目标角色
            RoleDto targetRole;
            using (var createTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/roles",
                       new CreateRoleDto
                       {
                           Code = targetRoleCode,
                           Name = targetRoleName,
                           Desc = "T3自动测试目标角色",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createTargetResponse.StatusCode);
                targetRole = await ReadApiDataAsync<RoleDto>(createTargetResponse);
                Assert.Equal(targetRoleCode, targetRole.Code);
                Assert.Equal(targetRoleName, targetRole.Name);
                targetRoleId = targetRole.Id;
                registry.Register<Role>(targetRole.Id, nameof(Role.Code), targetRoleCode);
            }

            // 操作员创建批量删除目标角色
            RoleDto batchTargetRole;
            using (var createBatchTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/roles",
                       new CreateRoleDto
                       {
                           Code = batchTargetRoleCode,
                           Name = batchTargetRoleName,
                           Desc = "T3自动测试批量删除角色",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBatchTargetResponse.StatusCode);
                batchTargetRole = await ReadApiDataAsync<RoleDto>(createBatchTargetResponse);
                Assert.Equal(batchTargetRoleCode, batchTargetRole.Code);
                batchTargetRoleId = batchTargetRole.Id;
                registry.Register<Role>(batchTargetRole.Id, nameof(Role.Code), batchTargetRoleCode);
            }

            // 操作员可读列表（按编码筛选，避免无排序分页漏检）与全量、详情
            using (var listResponse = await adminClient.GetAsync(
                       $"/api/roles/list?current=1&size=20&code={Uri.EscapeDataString(targetRoleCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<RoleDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == targetRole.Id || item.Code == targetRoleCode);
            }

            using (var allResponse = await adminClient.GetAsync("/api/roles"))
            {
                Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
                // 全量接口仅验证授权与契约成功；存在性以筛选分页与详情为准
            }

            using (var detailResponse = await adminClient.GetAsync($"/api/roles/{targetRole.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
                var detail = await ReadApiDataAsync<RoleDto>(detailResponse);
                Assert.Equal(targetRole.Id, detail.Id);
                Assert.Equal(targetRoleCode, detail.Code);
            }

            // 操作员更新目标角色描述
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/roles",
                       new UpdateRoleDto
                       {
                           Id = targetRole.Id,
                           Code = targetRoleCode,
                           Name = targetRoleName,
                           Desc = "T3自动测试目标角色-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var payload = await ReadApiResponseAsync<string>(updateResponse);
                Assert.Equal(ResponseCode.Success, payload.Code);
            }

            using (var detailAfterUpdate = await adminClient.GetAsync($"/api/roles/{targetRole.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterUpdate.StatusCode);
                var detail = await ReadApiDataAsync<RoleDto>(detailAfterUpdate);
                Assert.Equal("T3自动测试目标角色-已更新", detail.Desc);
            }

            // 操作员批量删除
            using (var batchDeleteResponse = await adminClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/roles/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { batchTargetRole.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, batchDeleteResponse.StatusCode);
            }

            await using (var afterBatchDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterBatchDelete.Roles.AnyAsync(role =>
                    role.Id == batchTargetRole.Id || role.Code == batchTargetRoleCode));
                batchTargetRoleId = null;
            }

            // 最小权限用户登录：仅角色读
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
                Assert.Contains(roleReadPermission, info.Permissions);
                Assert.DoesNotContain(roleCreatePermission, info.Permissions);
                Assert.DoesNotContain(roleUpdatePermission, info.Permissions);
                Assert.DoesNotContain(roleDeletePermission, info.Permissions);
                Assert.DoesNotContain(roleAssignMenusPermission, info.Permissions);
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
            using (var allowedAll = await limitedClient.GetAsync("/api/roles"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/roles/{targetRole.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var allowedList = await limitedClient.GetAsync("/api/roles/list?current=1&size=20"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
            }

            // 无写权限 → 403
            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/roles",
                       new CreateRoleDto
                       {
                           Code = deniedRoleCode,
                           Name = deniedRoleName,
                           Desc = "T3只读用户创建应拒绝",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedCreate.StatusCode);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/roles",
                       new UpdateRoleDto
                       {
                           Id = targetRole.Id,
                           Code = targetRoleCode,
                           Name = targetRoleName,
                           Desc = "T3只读用户更新应拒绝",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedUpdate.StatusCode);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/roles/{targetRole.Id}"))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedDelete.StatusCode);
            }

            using (var deniedBatchDelete = await limitedClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/roles/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { targetRole.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedBatchDelete.StatusCode);
            }

            using (var deniedAssignMenus = await limitedClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedAssignMenus.StatusCode);
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
                Assert.Contains(roleReadPermission, info.Permissions);
                Assert.Contains(roleCreatePermission, info.Permissions);
                Assert.Contains(roleUpdatePermission, info.Permissions);
                Assert.Contains(roleDeletePermission, info.Permissions);
                Assert.DoesNotContain(roleAssignMenusPermission, info.Permissions);
            }

            RoleDto expandedRole;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/roles",
                       new CreateRoleDto
                       {
                           Code = expandedRoleCode,
                           Name = expandedRoleName,
                           Desc = "T3扩权后创建角色",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedRole = await ReadApiDataAsync<RoleDto>(createExpanded);
                Assert.Equal(expandedRoleCode, expandedRole.Code);
                expandedRoleId = expandedRole.Id;
                registry.Register<Role>(expandedRole.Id, nameof(Role.Code), expandedRoleCode);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/roles",
                       new UpdateRoleDto
                       {
                           Id = expandedRole.Id,
                           Code = expandedRoleCode,
                           Name = expandedRoleName,
                           Desc = "T3扩权后创建角色-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/roles/{expandedRole.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.Roles.AnyAsync(role =>
                    role.Id == expandedRole.Id || role.Code == expandedRoleCode));
                expandedRoleId = null;
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
                Assert.Contains(roleReadPermission, info.Permissions);
                Assert.DoesNotContain(roleCreatePermission, info.Permissions);
                Assert.DoesNotContain(roleUpdatePermission, info.Permissions);
                Assert.DoesNotContain(roleDeletePermission, info.Permissions);
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
                       "/api/roles",
                       new CreateRoleDto
                       {
                           Code = $"{batch.Id}F",
                           Name = $"{batch.Id}FN",
                           Desc = "T3缩权后创建应拒绝",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedCreateAfterShrink.StatusCode);
            }

            // 操作员删除目标角色
            using (var deleteTarget = await adminClient.DeleteAsync($"/api/roles/{targetRole.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteTarget.StatusCode);
            }

            await using (var afterDeleteTarget = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteTarget.Roles.AnyAsync(role =>
                    role.Id == targetRole.Id || role.Code == targetRoleCode));
                targetRoleId = null;
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
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(
                fixture,
                registry,
                adminUsername,
                limitedUsername);
            await RegisterBatchOperationLogsAsync(
                fixture,
                registry,
                adminUsername,
                limitedUsername);

            // 先清本轮批次登记实体；UserRole 随用户级联，RoleMenu 随角色/菜单级联
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            // 兜底：临时用户、菜单按钮、残留关系（不触碰既有 Admin 角色）
            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUsernames = new[]
                {
                    adminUsername,
                    limitedUsername
                };

                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };

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

                var residualRoleCodes = new List<string>
                {
                    limitedRoleCode,
                    targetRoleCode,
                    batchTargetRoleCode,
                    expandedRoleCode,
                    deniedRoleCode,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualRoleIds = new List<Guid> { limitedRoleId };
                if (targetRoleId.HasValue)
                    residualRoleIds.Add(targetRoleId.Value);
                if (batchTargetRoleId.HasValue)
                    residualRoleIds.Add(batchTargetRoleId.Value);
                if (expandedRoleId.HasValue)
                    residualRoleIds.Add(expandedRoleId.Value);

                // 先清角色菜单与用户角色，再删角色
                var residualRoleMenus = await cleanupContext.RoleMenus
                    .Where(relation => residualRoleIds.Contains(relation.RoleId)
                                       || relation.MenuId == seedMenuId
                                       || relation.MenuId == writeMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoles = await cleanupContext.Roles
                    .Where(role => residualRoleIds.Contains(role.Id)
                                   || residualRoleCodes.Contains(role.Code)
                                   || (role.Code != null && role.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualRoles.Count > 0)
                {
                    cleanupContext.Roles.RemoveRange(residualRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtonIds = new List<Guid>
                {
                    seedRoleReadButtonId,
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
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername
                || user.Username == limitedUsername
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId
                || role.Code == limitedRoleCode
                || role.Code == targetRoleCode
                || role.Code == batchTargetRoleCode
                || role.Code == expandedRoleCode
                || (role.Code != null && role.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Id == seedMenuId
                || menu.Id == writeMenuId
                || menu.Name == seedMenuName
                || menu.Name == writeMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedRoleReadButtonId
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
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
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
