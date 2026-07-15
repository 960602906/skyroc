using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Department;
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
///     T3 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证部门 CRUD 权限矩阵与最小权限边界。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AuthDepartmentCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/更新/启停/删除部门及子部门；最小权限用户仅能读取已授权部门接口；未认证与无权限拒绝；缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task AuthDepartment_CrudAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedDeptReadButtonId = Guid.NewGuid();
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
        var rootDeptCode = $"{batch.Id}D1";
        var childDeptCode = $"{batch.Id}D2";
        var password = "SkyRocDeptPerm!2026";
        var userAgent = $"SkyRoc-T3-Dept/{batch.Id}";
        var createName = "T3-AuthDept";

        var deptReadPermission = PermissionCodes.System.Departments.Read;
        var deptCreatePermission = PermissionCodes.System.Departments.Create;
        var deptUpdatePermission = PermissionCodes.System.Departments.Update;
        var deptDeletePermission = PermissionCodes.System.Departments.Delete;

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
                Desc = "T3 部门权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T3部门操作员",
                    Gender = GenderType.Male,
                    Phone = "13900007701",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T3部门只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900007702",
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

            // 最小权限角色初始仅挂部门读取权限；写权限菜单稍后由操作员分配
            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = seedMenuId,
                    Name = seedMenuName,
                    Path = $"/{batch.Id}s",
                    Title = "T3部门只读菜单",
                    Component = "page.t3.dept.seed",
                    MenuType = MenuType.Menu,
                    Order = 9301,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T3部门写权限菜单",
                    Component = "page.t3.dept.write",
                    MenuType = MenuType.Menu,
                    Order = 9302,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedDeptReadButtonId,
                    Code = deptReadPermission,
                    Desc = "T3 部门读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = deptCreatePermission,
                    Desc = "T3 部门创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = deptUpdatePermission,
                    Desc = "T3 部门更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = deptDeletePermission,
                    Desc = "T3 部门删除权限按钮",
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

        Guid? rootDeptId = null;
        Guid? childDeptId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问部门接口
            using (var anonymousTree = await anonymousClient.GetAsync("/api/departments/tree"))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousTree.StatusCode);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/departments",
                       new CreateDepartmentDto
                       {
                           Name = "T3未认证创建应拒绝",
                           Code = $"{batch.Id}DX",
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

            // 操作员创建根部门
            DepartmentDto rootDept;
            using (var createRootResponse = await adminClient.PostAsJsonAsync(
                       "/api/departments",
                       new CreateDepartmentDto
                       {
                           Name = "T3自动测试根部门",
                           Code = rootDeptCode,
                           Phone = "13900007711",
                           Email = $"{batch.Id}-root@skyroc-autotest.example",
                           Sort = 9101,
                           Remark = "T3 部门权限切片根部门，仅本轮自动测试使用",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createRootResponse.StatusCode);
                rootDept = await ReadApiDataAsync<DepartmentDto>(createRootResponse);
                Assert.Equal(rootDeptCode, rootDept.Code);
                Assert.Equal("T3自动测试根部门", rootDept.Name);
                rootDeptId = rootDept.Id;
                registry.Register<Department>(rootDept.Id, nameof(Department.Code), rootDeptCode);
            }

            // 操作员创建子部门
            DepartmentDto childDept;
            using (var createChildResponse = await adminClient.PostAsJsonAsync(
                       "/api/departments",
                       new CreateDepartmentDto
                       {
                           Name = "T3自动测试子部门",
                           Code = childDeptCode,
                           ParentId = rootDept.Id,
                           Phone = "13900007712",
                           Email = $"{batch.Id}-child@skyroc-autotest.example",
                           Sort = 9102,
                           Remark = "T3 部门权限切片子部门，仅本轮自动测试使用",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createChildResponse.StatusCode);
                childDept = await ReadApiDataAsync<DepartmentDto>(createChildResponse);
                Assert.Equal(childDeptCode, childDept.Code);
                Assert.Equal(rootDept.Id, childDept.ParentId);
                childDeptId = childDept.Id;
                registry.Register<Department>(childDept.Id, nameof(Department.Code), childDeptCode);
            }

            // 操作员可读树与详情
            using (var treeResponse = await adminClient.GetAsync("/api/departments/tree"))
            {
                Assert.Equal(HttpStatusCode.OK, treeResponse.StatusCode);
                var tree = await ReadApiDataAsync<PagedResult<DepartmentTreeDto>>(treeResponse);
                Assert.NotNull(tree.Records);
                Assert.Contains(tree.Records!, item => item.Id == rootDept.Id || item.Code == rootDeptCode);
            }

            using (var detailResponse = await adminClient.GetAsync($"/api/departments/{rootDept.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
                var detail = await ReadApiDataAsync<DepartmentDto>(detailResponse);
                Assert.Equal(rootDept.Id, detail.Id);
                Assert.Equal(rootDeptCode, detail.Code);
            }

            using (var usersResponse = await adminClient.GetAsync($"/api/departments/{rootDept.Id}/users"))
            {
                Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
            }

            // 操作员更新子部门备注
            using (var updateChildResponse = await adminClient.PutAsJsonAsync(
                       "/api/departments",
                       new UpdateDepartmentDto
                       {
                           Id = childDept.Id,
                           Name = "T3自动测试子部门-已更新",
                           Code = childDeptCode,
                           ParentId = rootDept.Id,
                           Phone = "13900007712",
                           Email = $"{batch.Id}-child@skyroc-autotest.example",
                           Sort = 9102,
                           Remark = "T3 部门权限切片子部门-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateChildResponse.StatusCode);
                var updated = await ReadApiDataAsync<DepartmentDto>(updateChildResponse);
                Assert.Equal("T3自动测试子部门-已更新", updated.Name);
                Assert.Equal("T3 部门权限切片子部门-已更新", updated.Remark);
            }

            // 操作员禁用再启用子部门
            using (var disableResponse = await adminClient.PatchAsync(
                       $"/api/departments/{childDept.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
                var disabled = await ReadApiDataAsync<DepartmentDto>(disableResponse);
                Assert.Equal(Status.Disable, disabled.Status);
            }

            using (var enableResponse = await adminClient.PatchAsync(
                       $"/api/departments/{childDept.Id}/status?status={(int)Status.Enable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
                var enabled = await ReadApiDataAsync<DepartmentDto>(enableResponse);
                Assert.Equal(Status.Enable, enabled.Status);
            }

            // 有子部门时删除根部门应业务失败（BusinessException → 502）
            using (var deleteRootWithChild = await adminClient.DeleteAsync($"/api/departments/{rootDept.Id}"))
            {
                Assert.Equal(HttpStatusCode.BadGateway, deleteRootWithChild.StatusCode);
            }

            await using (var afterFailedDelete = fixture.CreateDbContext())
            {
                Assert.True(await afterFailedDelete.Departments.AnyAsync(dept => dept.Id == rootDept.Id));
                Assert.True(await afterFailedDelete.Departments.AnyAsync(dept => dept.Id == childDept.Id));
            }

            // 最小权限用户登录：仅部门读
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
                Assert.Contains(deptReadPermission, info.Permissions);
                Assert.DoesNotContain(deptCreatePermission, info.Permissions);
                Assert.DoesNotContain(deptUpdatePermission, info.Permissions);
                Assert.DoesNotContain(deptDeletePermission, info.Permissions);
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
            using (var allowedTree = await limitedClient.GetAsync("/api/departments/tree"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedTree.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/departments/{rootDept.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var allowedUsers = await limitedClient.GetAsync($"/api/departments/{rootDept.Id}/users"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedUsers.StatusCode);
            }

            // 无写权限 → 403
            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/departments",
                       new CreateDepartmentDto
                       {
                           Name = "T3只读用户创建应拒绝",
                           Code = $"{batch.Id}D3",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedCreate.StatusCode);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/departments",
                       new UpdateDepartmentDto
                       {
                           Id = childDept.Id,
                           Name = "T3只读用户更新应拒绝",
                           Code = childDeptCode,
                           ParentId = rootDept.Id,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedUpdate.StatusCode);
            }

            using (var deniedToggle = await limitedClient.PatchAsync(
                       $"/api/departments/{childDept.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedToggle.StatusCode);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/departments/{childDept.Id}"))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedDelete.StatusCode);
            }

            using (var deniedBatchDelete = await limitedClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/departments/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { childDept.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedBatchDelete.StatusCode);
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
                Assert.Contains(deptReadPermission, info.Permissions);
                Assert.Contains(deptCreatePermission, info.Permissions);
                Assert.Contains(deptUpdatePermission, info.Permissions);
                Assert.Contains(deptDeletePermission, info.Permissions);
            }

            var expandedDeptCode = $"{batch.Id}D3";
            DepartmentDto expandedDept;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/departments",
                       new CreateDepartmentDto
                       {
                           Name = "T3扩权后创建部门",
                           Code = expandedDeptCode,
                           Phone = "13900007713",
                           Sort = 9103,
                           Remark = "T3 扩权用户创建，随后删除",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedDept = await ReadApiDataAsync<DepartmentDto>(createExpanded);
                Assert.Equal(expandedDeptCode, expandedDept.Code);
                registry.Register<Department>(expandedDept.Id, nameof(Department.Code), expandedDeptCode);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/departments",
                       new UpdateDepartmentDto
                       {
                           Id = expandedDept.Id,
                           Name = "T3扩权后创建部门-已更新",
                           Code = expandedDeptCode,
                           Phone = "13900007713",
                           Sort = 9103,
                           Remark = "T3 扩权用户更新后删除",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/departments/{expandedDept.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
                var payload = await ReadApiDataAsync<bool>(deleteExpanded);
                Assert.True(payload);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.Departments.AnyAsync(dept => dept.Id == expandedDept.Id));
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
                Assert.Contains(deptReadPermission, info.Permissions);
                Assert.DoesNotContain(deptCreatePermission, info.Permissions);
                Assert.DoesNotContain(deptUpdatePermission, info.Permissions);
                Assert.DoesNotContain(deptDeletePermission, info.Permissions);
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
                       "/api/departments",
                       new CreateDepartmentDto
                       {
                           Name = "T3缩权后创建应拒绝",
                           Code = $"{batch.Id}D4",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedCreateAfterShrink.StatusCode);
            }

            // 操作员先删子部门再删根部门
            using (var deleteChild = await adminClient.DeleteAsync($"/api/departments/{childDept.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteChild.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteChild));
            }

            using (var deleteRoot = await adminClient.DeleteAsync($"/api/departments/{rootDept.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteRoot.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteRoot));
            }

            await using (var afterDeleteDepts = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteDepts.Departments.AnyAsync(dept =>
                    dept.Id == rootDept.Id || dept.Id == childDept.Id
                    || dept.Code == rootDeptCode || dept.Code == childDeptCode));
                rootDeptId = null;
                childDeptId = null;
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

            // 兜底：临时部门、菜单按钮、残留关系与用户（不触碰既有 Admin 角色）
            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualDeptCodes = new[]
                {
                    rootDeptCode,
                    childDeptCode,
                    $"{batch.Id}D3",
                    $"{batch.Id}D4",
                    $"{batch.Id}DX"
                };
                var residualDepts = await cleanupContext.Departments
                    .Where(dept =>
                        (rootDeptId.HasValue && dept.Id == rootDeptId.Value)
                        || (childDeptId.HasValue && dept.Id == childDeptId.Value)
                        || residualDeptCodes.Contains(dept.Code)
                        || (dept.Code != null && dept.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualDepts.Count > 0)
                {
                    // 先子后父，避免外键约束（若存在）
                    foreach (var child in residualDepts.Where(d => d.ParentId != null).ToList())
                        cleanupContext.Departments.Remove(child);
                    await cleanupContext.SaveChangesAsync();
                    foreach (var parent in residualDepts.Where(d => d.ParentId == null).ToList())
                        cleanupContext.Departments.Remove(parent);
                    await cleanupContext.SaveChangesAsync();
                }

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
                                       || relation.MenuId == writeMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtonIds = new List<Guid>
                {
                    seedDeptReadButtonId,
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
                || menu.Id == writeMenuId
                || menu.Name == seedMenuName
                || menu.Name == writeMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedDeptReadButtonId
                || button.Id == writeCreateButtonId
                || button.Id == writeUpdateButtonId
                || button.Id == writeDeleteButtonId
                || button.CreateName == createName));
            Assert.False(await residualContext.Departments.AnyAsync(dept =>
                dept.Code == rootDeptCode
                || dept.Code == childDeptCode
                || dept.Code == $"{batch.Id}D3"
                || dept.Code == $"{batch.Id}D4"
                || (dept.Code != null && dept.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == adminUserId || relation.UserId == limitedUserId));
            Assert.False(await residualContext.RoleMenus.AnyAsync(relation =>
                relation.RoleId == limitedRoleId
                || relation.MenuId == seedMenuId
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
