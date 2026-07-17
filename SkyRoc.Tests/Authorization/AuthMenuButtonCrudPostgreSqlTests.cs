using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Menu;
using Application.DTOs.MenuButton;
using Application.DTOs.Role;
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
///     T3 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证菜单/按钮 CRUD 权限联动与最小权限边界。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AuthMenuButtonCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/更新/删除菜单与按钮；最小权限用户仅能读取已授权资源；按钮权限码联动到业务接口；未认证与无权限拒绝；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task AuthMenuButton_CrudAndPermissionLinkage_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedMenuReadButtonId = Guid.NewGuid();
        var seedMenuButtonReadButtonId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username/role code/name ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var createdMenuName = $"{batch.Id}C";
        var password = "SkyRocMenuBtn!2026";
        var userAgent = $"SkyRoc-T3-MenuBtn/{batch.Id}";
        var createName = "T3-AuthMenuBtn";

        var menuReadPermission = PermissionCodes.System.Menus.Read;
        var menuCreatePermission = PermissionCodes.System.Menus.Create;
        var menuUpdatePermission = PermissionCodes.System.Menus.Update;
        var menuDeletePermission = PermissionCodes.System.Menus.Delete;
        var menuButtonReadPermission = PermissionCodes.System.MenuButtons.Read;
        var menuButtonCreatePermission = PermissionCodes.System.MenuButtons.Create;
        var menuButtonUpdatePermission = PermissionCodes.System.MenuButtons.Update;
        var menuButtonDeletePermission = PermissionCodes.System.MenuButtons.Delete;
        var departmentReadPermission = PermissionCodes.System.Departments.Read;

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
                Desc = "T3 菜单按钮 CRUD 最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T3菜单操作员",
                    Gender = GenderType.Male,
                    Phone = "13900009901",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T3菜单只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900009902",
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

            // 最小权限角色初始只挂菜单/按钮读取权限，CRUD 与业务权限由后续 HTTP 创建的按钮联动
            await seedContext.Menus.AddAsync(new Menu
            {
                Id = seedMenuId,
                Name = seedMenuName,
                Path = $"/{batch.Id}s",
                Title = "T3菜单管理只读菜单",
                Component = "page.t3.menu.seed",
                MenuType = MenuType.Menu,
                Order = 9201,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedMenuReadButtonId,
                    Code = menuReadPermission,
                    Desc = "T3 菜单读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = seedMenuButtonReadButtonId,
                    Code = menuButtonReadPermission,
                    Desc = "T3 菜单按钮读取权限按钮",
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
        // MenuButton.Code 为权限码、无批次前缀；随菜单级联删除，finally 再按主键兜底。

        Guid? createdMenuId = null;
        Guid? linkedButtonId = null;
        Guid? extraButtonId = null;
        string? createdMenuActualName = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问菜单/按钮接口
            using (var anonymousMenus = await anonymousClient.GetAsync("/api/menus"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousMenus, ResponseCode.Unauthorized);
            }

            using (var anonymousCreateMenu = await anonymousClient.PostAsJsonAsync(
                       "/api/menus",
                       new CreateMenuDto
                       {
                           Name = createdMenuName,
                           Path = $"/{batch.Id}c",
                           Title = "T3未认证创建应拒绝",
                           MenuType = MenuType.Menu,
                           Component = "page.t3.menu.anon",
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousCreateMenu, ResponseCode.Unauthorized);
            }

            using (var anonymousCreateButton = await anonymousClient.PostAsJsonAsync(
                       "/api/menu-buttons",
                       new CreateMenuButtonDto
                       {
                           Code = departmentReadPermission,
                           Desc = "T3未认证按钮应拒绝",
                           MenuId = seedMenuId
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousCreateButton, ResponseCode.Unauthorized);
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

            // 操作员创建业务联动菜单
            MenuDto createdMenu;
            using (var createMenuResponse = await adminClient.PostAsJsonAsync(
                       "/api/menus",
                       new CreateMenuDto
                       {
                           Name = createdMenuName,
                           Path = $"/{batch.Id}c",
                           Title = "T3菜单按钮联动菜单",
                           MenuType = MenuType.Menu,
                           Component = "page.t3.menu.linked",
                           Order = 9202,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createMenuResponse.StatusCode);
                createdMenu = await ReadApiDataAsync<MenuDto>(createMenuResponse);
                Assert.Equal(createdMenuName, createdMenu.Name);
                createdMenuId = createdMenu.Id;
                createdMenuActualName = createdMenu.Name;
                registry.Register<Menu>(createdMenu.Id, nameof(Menu.Name), createdMenu.Name!);
            }

            // 操作员在新菜单上创建部门读取按钮（业务权限码联动）
            MenuButtonDto linkedButton;
            using (var createButtonResponse = await adminClient.PostAsJsonAsync(
                       "/api/menu-buttons",
                       new CreateMenuButtonDto
                       {
                           Code = departmentReadPermission,
                           Desc = "T3 部门读取联动按钮",
                           MenuId = createdMenu.Id
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createButtonResponse.StatusCode);
                linkedButton = await ReadApiDataAsync<MenuButtonDto>(createButtonResponse);
                Assert.Equal(departmentReadPermission, linkedButton.Code);
                Assert.Equal(createdMenu.Id, linkedButton.MenuId);
                linkedButtonId = linkedButton.Id;
            }

            // 额外按钮，用于验证删除与更新
            MenuButtonDto extraButton;
            using (var extraButtonResponse = await adminClient.PostAsJsonAsync(
                       "/api/menu-buttons",
                       new CreateMenuButtonDto
                       {
                           Code = "system:menu-button:extra-t3",
                           Desc = "T3 待删除额外按钮",
                           MenuId = createdMenu.Id
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, extraButtonResponse.StatusCode);
                extraButton = await ReadApiDataAsync<MenuButtonDto>(extraButtonResponse);
                extraButtonId = extraButton.Id;
            }

            // 操作员可读取菜单与按钮
            using (var menuDetail = await adminClient.GetAsync($"/api/menus/{createdMenu.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, menuDetail.StatusCode);
                var detail = await ReadApiDataAsync<MenuDto>(menuDetail);
                Assert.Equal(createdMenu.Id, detail.Id);
                Assert.Equal(createdMenuName, detail.Name);
            }

            using (var buttonDetail = await adminClient.GetAsync($"/api/menu-buttons/{linkedButton.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, buttonDetail.StatusCode);
                var detail = await ReadApiDataAsync<MenuButtonDto>(buttonDetail);
                Assert.Equal(linkedButton.Id, detail.Id);
                Assert.Equal(departmentReadPermission, detail.Code);
            }

            // 操作员更新菜单标题
            using (var updateMenuResponse = await adminClient.PutAsJsonAsync(
                       "/api/menus",
                       new UpdateMenuDto
                       {
                           Id = createdMenu.Id,
                           Name = createdMenuName,
                           Path = $"/{batch.Id}c",
                           Title = "T3菜单按钮联动菜单-已更新",
                           MenuType = MenuType.Menu,
                           Component = "page.t3.menu.linked",
                           Order = 9202,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateMenuResponse.StatusCode);
                var payload = await ReadApiResponseAsync<string>(updateMenuResponse);
                Assert.Equal(ResponseCode.Success, payload.Code);
            }

            await using (var afterUpdateMenu = fixture.CreateDbContext())
            {
                var menu = await afterUpdateMenu.Menus.AsNoTracking()
                    .SingleAsync(item => item.Id == createdMenu.Id);
                Assert.Equal("T3菜单按钮联动菜单-已更新", menu.Title);
            }

            // 操作员更新联动按钮描述
            using (var updateButtonResponse = await adminClient.PutAsJsonAsync(
                       $"/api/menu-buttons?menuId={createdMenu.Id}",
                       new UpdateMenuButtonDto
                       {
                           Id = linkedButton.Id,
                           Code = departmentReadPermission,
                           Desc = "T3 部门读取联动按钮-已更新",
                           MenuId = createdMenu.Id
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateButtonResponse.StatusCode);
                var updated = await ReadApiDataAsync<MenuButtonDto>(updateButtonResponse);
                Assert.Equal("T3 部门读取联动按钮-已更新", updated.Desc);
            }

            // 操作员删除额外按钮
            using (var deleteExtraButton = await adminClient.DeleteAsync($"/api/menu-buttons/{extraButton.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExtraButton.StatusCode);
            }

            await using (var afterDeleteButton = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteButton.MenuButtons.AnyAsync(button => button.Id == extraButton.Id));
                extraButtonId = null;
            }

            // 将联动菜单分配给最小权限角色（保留种子只读菜单）
            using (var assignMenusResponse = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId, createdMenu.Id]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignMenusResponse.StatusCode);
            }

            await using (var afterAssign = fixture.CreateDbContext())
            {
                var roleMenus = await afterAssign.RoleMenus.AsNoTracking()
                    .Where(relation => relation.RoleId == limitedRoleId)
                    .ToListAsync();
                Assert.Equal(2, roleMenus.Count);
                Assert.Contains(roleMenus, relation => relation.MenuId == seedMenuId);
                Assert.Contains(roleMenus, relation => relation.MenuId == createdMenu.Id);
            }

            // 最小权限用户登录：应拿到菜单读、按钮读、部门读，不含菜单写权限
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
                Assert.Contains(menuReadPermission, info.Permissions);
                Assert.Contains(menuButtonReadPermission, info.Permissions);
                Assert.Contains(departmentReadPermission, info.Permissions);
                Assert.DoesNotContain(menuCreatePermission, info.Permissions);
                Assert.DoesNotContain(menuUpdatePermission, info.Permissions);
                Assert.DoesNotContain(menuDeletePermission, info.Permissions);
                Assert.DoesNotContain(menuButtonCreatePermission, info.Permissions);
                Assert.DoesNotContain(menuButtonUpdatePermission, info.Permissions);
                Assert.DoesNotContain(menuButtonDeletePermission, info.Permissions);
            }

            // getRoutes 包含种子与联动菜单路径
            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.Contains($"/{batch.Id}c", paths);
            }

            // 只读允许：菜单列表、菜单详情、按钮详情、部门树
            using (var allowedMenus = await limitedClient.GetAsync("/api/menus"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedMenus.StatusCode);
            }

            using (var allowedMenuDetail = await limitedClient.GetAsync($"/api/menus/{createdMenu.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedMenuDetail.StatusCode);
            }

            using (var allowedButtonDetail = await limitedClient.GetAsync($"/api/menu-buttons/{linkedButton.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedButtonDetail.StatusCode);
            }

            using (var allowedDepartments = await limitedClient.GetAsync("/api/departments/tree"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDepartments.StatusCode);
            }

            // 无写权限 → 403
            using (var deniedCreateMenu = await limitedClient.PostAsJsonAsync(
                       "/api/menus",
                       new CreateMenuDto
                       {
                           Name = $"{batch.Id}D",
                           Path = $"/{batch.Id}d",
                           Title = "T3只读用户创建应拒绝",
                           MenuType = MenuType.Menu,
                           Component = "page.t3.menu.denied",
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateMenu, ResponseCode.Forbidden);
            }

            using (var deniedUpdateMenu = await limitedClient.PutAsJsonAsync(
                       "/api/menus",
                       new UpdateMenuDto
                       {
                           Id = createdMenu.Id,
                           Name = createdMenuName,
                           Path = $"/{batch.Id}c",
                           Title = "T3只读用户更新应拒绝",
                           MenuType = MenuType.Menu,
                           Component = "page.t3.menu.linked",
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdateMenu, ResponseCode.Forbidden);
            }

            using (var deniedDeleteMenu = await limitedClient.DeleteAsync($"/api/menus/{createdMenu.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDeleteMenu, ResponseCode.Forbidden);
            }

            using (var deniedCreateButton = await limitedClient.PostAsJsonAsync(
                       "/api/menu-buttons",
                       new CreateMenuButtonDto
                       {
                           Code = "system:menu-button:denied",
                           Desc = "T3只读用户按钮创建应拒绝",
                           MenuId = createdMenu.Id
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateButton, ResponseCode.Forbidden);
            }

            using (var deniedUpdateButton = await limitedClient.PutAsJsonAsync(
                       $"/api/menu-buttons?menuId={createdMenu.Id}",
                       new UpdateMenuButtonDto
                       {
                           Id = linkedButton.Id,
                           Code = departmentReadPermission,
                           Desc = "T3只读用户按钮更新应拒绝",
                           MenuId = createdMenu.Id
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdateButton, ResponseCode.Forbidden);
            }

            using (var deniedDeleteButton = await limitedClient.DeleteAsync($"/api/menu-buttons/{linkedButton.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDeleteButton, ResponseCode.Forbidden);
            }

            // 缩权：移除联动菜单后重新登录，部门读权限与菜单路径应消失
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
                Assert.Contains(menuReadPermission, info.Permissions);
                Assert.Contains(menuButtonReadPermission, info.Permissions);
                Assert.DoesNotContain(departmentReadPermission, info.Permissions);
            }

            using (var routesAfterShrink = await limitedReloginClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesAfterShrink.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesAfterShrink);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}c", paths);
            }

            using (var deniedDepartments = await limitedReloginClient.GetAsync("/api/departments/tree"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDepartments, ResponseCode.Forbidden);
            }

            // 操作员删除联动菜单（级联清理按钮）
            using (var deleteMenuResponse = await adminClient.DeleteAsync($"/api/menus/{createdMenu.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteMenuResponse.StatusCode);
            }

            await using (var afterDeleteMenu = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteMenu.Menus.AnyAsync(menu => menu.Id == createdMenu.Id));
                Assert.False(await afterDeleteMenu.MenuButtons.AnyAsync(button =>
                    button.Id == linkedButton.Id || button.MenuId == createdMenu.Id));
                createdMenuId = null;
                linkedButtonId = null;
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
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            // 先清本轮批次登记实体；UserRole 随用户级联，RoleMenu 随角色/菜单级联
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            // 兜底：临时菜单按钮、残留关系与用户（不触碰既有 Admin 角色）
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
                                       || (createdMenuId.HasValue && relation.MenuId == createdMenuId.Value))
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtonIds = new List<Guid>
                {
                    seedMenuReadButtonId,
                    seedMenuButtonReadButtonId
                };
                if (linkedButtonId.HasValue)
                    residualButtonIds.Add(linkedButtonId.Value);
                if (extraButtonId.HasValue)
                    residualButtonIds.Add(extraButtonId.Value);

                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => residualButtonIds.Contains(button.Id)
                                     || button.MenuId == seedMenuId
                                     || (createdMenuId.HasValue && button.MenuId == createdMenuId.Value)
                                     || button.CreateName == createName)
                    .ToListAsync();
                if (residualButtons.Count > 0)
                {
                    cleanupContext.MenuButtons.RemoveRange(residualButtons);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualMenuIds = new List<Guid> { seedMenuId };
                if (createdMenuId.HasValue)
                    residualMenuIds.Add(createdMenuId.Value);

                var residualMenus = await cleanupContext.Menus
                    .Where(menu => residualMenuIds.Contains(menu.Id)
                                   || menu.Name == seedMenuName
                                   || menu.Name == createdMenuName)
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
                || menu.Name == seedMenuName
                || menu.Name == createdMenuName
                || (createdMenuId.HasValue && menu.Id == createdMenuId.Value)));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedMenuReadButtonId
                || button.Id == seedMenuButtonReadButtonId
                || (linkedButtonId.HasValue && button.Id == linkedButtonId.Value)
                || (extraButtonId.HasValue && button.Id == extraButtonId.Value)
                || button.CreateName == createName));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == adminUserId || relation.UserId == limitedUserId));
            Assert.False(await residualContext.RoleMenus.AnyAsync(relation =>
                relation.RoleId == limitedRoleId || relation.MenuId == seedMenuId));
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
