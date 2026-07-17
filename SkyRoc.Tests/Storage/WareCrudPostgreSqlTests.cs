using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Role;
using Application.DTOs.Storage;
using Domain.Entities;
using Domain.Entities.Storage;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Tests.Common;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.Storage;

/// <summary>
///     T4 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证仓库档案 CRUD、启停与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class WareCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/查询/更新/删除/批量删除仓库并切换启停；最小权限仅读；未认证与无权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task Ware_CrudStatusAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedWareReadButtonId = Guid.NewGuid();
        var writeMenuId = Guid.NewGuid();
        var writeCreateButtonId = Guid.NewGuid();
        var writeUpdateButtonId = Guid.NewGuid();
        var writeDeleteButtonId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username/role/code ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var writeMenuName = $"{batch.Id}W";
        var targetWareCode = $"{batch.Id}P";
        var targetWareName = $"{batch.Id}-联调冷链仓";
        var batchWareCode = $"{batch.Id}B";
        var batchWareName = $"{batch.Id}-批量删除仓";
        var expandedWareCode = $"{batch.Id}E";
        var expandedWareName = $"{batch.Id}-扩权仓";
        var deniedWareCode = $"{batch.Id}D";
        var deniedWareName = $"{batch.Id}-拒绝仓";
        var password = "SkyRocWarePerm!2026";
        var userAgent = $"SkyRoc-T4-Ware/{batch.Id}";
        var createName = "T4-WareCrud";

        var storageReadPermission = PermissionCodes.Business.Storage.Read;
        var storageCreatePermission = PermissionCodes.Business.Storage.Create;
        var storageUpdatePermission = PermissionCodes.Business.Storage.Update;
        var storageDeletePermission = PermissionCodes.Business.Storage.Delete;

        Guid adminRoleId;
        Guid managedWareId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            // 复用 T2 受管仓库主数据，仅用于断言不被本轮改动/删除
            var managedWareCode = DemoDataStableKeyCatalog.Create("WARE", 1);
            var managedWare = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedWareCode);
            Assert.NotNull(managedWare);
            managedWareId = managedWare.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T4 仓库权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T4仓库操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008921",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T4仓库只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900008922",
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

            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = seedMenuId,
                    Name = seedMenuName,
                    Path = $"/{batch.Id}s",
                    Title = "T4仓库只读菜单",
                    Component = "page.t4.ware.seed",
                    MenuType = MenuType.Menu,
                    Order = 9631,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T4仓库写权限菜单",
                    Component = "page.t4.ware.write",
                    MenuType = MenuType.Menu,
                    Order = 9632,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedWareReadButtonId,
                    Code = storageReadPermission,
                    Desc = "T4 库存/仓库读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = storageCreatePermission,
                    Desc = "T4 库存/仓库创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = storageUpdatePermission,
                    Desc = "T4 库存/仓库更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = storageDeletePermission,
                    Desc = "T4 库存/仓库删除权限按钮",
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

        Guid? targetWareId = null;
        Guid? batchWareId = null;
        Guid? expandedWareId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问仓库接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/wares"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/wares",
                       new CreateWareDto
                       {
                           Code = $"{batch.Id}X",
                           Name = $"{batch.Id}-未认证仓",
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

            // 操作员创建目标仓库（联系人/电话/地址/排序完整业务字段）
            WareDto targetWare;
            using (var createTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/wares",
                       new CreateWareDto
                       {
                           Code = targetWareCode,
                           Name = targetWareName,
                           ContactName = "王仓管",
                           ContactPhone = "13800139101",
                           Address = "上海市浦东新区联调物流园冷链仓 A 栋",
                           Sort = 100,
                           Remark = "T4仓库CRUD切片目标仓库",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createTargetResponse.StatusCode);
                targetWare = await ReadApiDataAsync<WareDto>(createTargetResponse);
                Assert.Equal(targetWareCode, targetWare.Code);
                Assert.Equal(targetWareName, targetWare.Name);
                Assert.Equal("王仓管", targetWare.ContactName);
                Assert.Equal("13800139101", targetWare.ContactPhone);
                Assert.Equal("上海市浦东新区联调物流园冷链仓 A 栋", targetWare.Address);
                Assert.Equal(100, targetWare.Sort);
                Assert.Equal("T4仓库CRUD切片目标仓库", targetWare.Remark);
                Assert.Equal(Status.Enable, targetWare.Status);
                targetWareId = targetWare.Id;
                registry.Register<Ware>(targetWare.Id, nameof(Ware.Code), targetWareCode);
            }

            // 批量删除目标仓库
            WareDto batchWare;
            using (var createBatchTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/wares",
                       new CreateWareDto
                       {
                           Code = batchWareCode,
                           Name = batchWareName,
                           ContactName = "钱批量",
                           ContactPhone = "13800139102",
                           Address = "上海市青浦区批量物流园 3 号",
                           Sort = 200,
                           Remark = "T4仓库CRUD切片批量删除仓库",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBatchTargetResponse.StatusCode);
                batchWare = await ReadApiDataAsync<WareDto>(createBatchTargetResponse);
                Assert.Equal(batchWareCode, batchWare.Code);
                batchWareId = batchWare.Id;
                registry.Register<Ware>(batchWare.Id, nameof(Ware.Code), batchWareCode);
            }

            // 详情应回填业务字段
            using (var detailAfterCreate = await adminClient.GetAsync($"/api/wares/{targetWare.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterCreate.StatusCode);
                var detail = await ReadApiDataAsync<WareDto>(detailAfterCreate);
                Assert.Equal(targetWareCode, detail.Code);
                Assert.Equal(targetWareName, detail.Name);
                Assert.Equal("王仓管", detail.ContactName);
                Assert.Equal(100, detail.Sort);
                Assert.Equal(Status.Enable, detail.Status);
            }

            await using (var entityContext = fixture.CreateDbContext())
            {
                var wareEntity = await entityContext.Wares.AsNoTracking()
                    .SingleAsync(item => item.Id == targetWare.Id);
                Assert.Equal(targetWareCode, wareEntity.Code);
                Assert.Equal("T4仓库CRUD切片目标仓库", wareEntity.Remark);
                Assert.Equal(100, wareEntity.Sort);
            }

            // 分页/全量/详情
            using (var listResponse = await adminClient.GetAsync(
                       $"/api/wares/list?current=1&size=20&code={Uri.EscapeDataString(targetWareCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<WareDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == targetWare.Id || item.Code == targetWareCode);
            }

            using (var allResponse = await adminClient.GetAsync("/api/wares"))
            {
                Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            }

            // 更新仓库业务字段
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/wares",
                       new UpdateWareDto
                       {
                           Id = targetWare.Id,
                           Code = targetWareCode,
                           Name = targetWareName,
                           ContactName = "王仓管-更新",
                           ContactPhone = "13800139991",
                           Address = "上海市浦东新区联调物流园冷链仓 B 栋",
                           Sort = 150,
                           Remark = "T4仓库CRUD切片目标仓库-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var updated = await ReadApiDataAsync<WareDto>(updateResponse);
                Assert.Equal("王仓管-更新", updated.ContactName);
                Assert.Equal("13800139991", updated.ContactPhone);
                Assert.Equal("上海市浦东新区联调物流园冷链仓 B 栋", updated.Address);
                Assert.Equal(150, updated.Sort);
                Assert.Equal("T4仓库CRUD切片目标仓库-已更新", updated.Remark);
            }

            await using (var afterUpdate = fixture.CreateDbContext())
            {
                var wareEntity = await afterUpdate.Wares.AsNoTracking()
                    .SingleAsync(item => item.Id == targetWare.Id);
                Assert.Equal("王仓管-更新", wareEntity.ContactName);
                Assert.Equal("T4仓库CRUD切片目标仓库-已更新", wareEntity.Remark);
                Assert.Equal(150, wareEntity.Sort);
            }

            // 停用与启用
            using (var disableResponse = await adminClient.PatchAsync(
                       $"/api/wares/{targetWare.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
                var disabled = await ReadApiDataAsync<WareDto>(disableResponse);
                Assert.Equal(Status.Disable, disabled.Status);
            }

            using (var enableResponse = await adminClient.PatchAsync(
                       $"/api/wares/{targetWare.Id}/status?status={(int)Status.Enable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
                var enabled = await ReadApiDataAsync<WareDto>(enableResponse);
                Assert.Equal(Status.Enable, enabled.Status);
            }

            // 操作员批量删除
            using (var batchDeleteResponse = await adminClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/wares/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { batchWare.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, batchDeleteResponse.StatusCode);
            }

            await using (var afterBatchDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterBatchDelete.Wares.AnyAsync(item =>
                    item.Id == batchWare.Id || item.Code == batchWareCode));
                batchWareId = null;
            }

            // 最小权限用户登录：仅库存读（仓库读）
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
                Assert.Contains(storageReadPermission, info.Permissions);
                Assert.DoesNotContain(storageCreatePermission, info.Permissions);
                Assert.DoesNotContain(storageUpdatePermission, info.Permissions);
                Assert.DoesNotContain(storageDeletePermission, info.Permissions);
            }

            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
            }

            using (var allowedAll = await limitedClient.GetAsync("/api/wares"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/wares/{targetWare.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/wares",
                       new CreateWareDto
                       {
                           Code = deniedWareCode,
                           Name = deniedWareName,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/wares",
                       new UpdateWareDto
                       {
                           Id = targetWare.Id,
                           Code = targetWareCode,
                           Name = targetWareName,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdate, ResponseCode.Forbidden);
            }

            using (var deniedStatus = await limitedClient.PatchAsync(
                       $"/api/wares/{targetWare.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedStatus, ResponseCode.Forbidden);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/wares/{targetWare.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDelete, ResponseCode.Forbidden);
            }

            // 扩权：分配写权限菜单后重新登录
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
                Assert.Contains(storageReadPermission, info.Permissions);
                Assert.Contains(storageCreatePermission, info.Permissions);
                Assert.Contains(storageUpdatePermission, info.Permissions);
                Assert.Contains(storageDeletePermission, info.Permissions);
            }

            WareDto expandedWare;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/wares",
                       new CreateWareDto
                       {
                           Code = expandedWareCode,
                           Name = expandedWareName,
                           ContactName = "扩权仓管",
                           ContactPhone = "13800139211",
                           Address = "上海市松江区扩权物流园 1 号",
                           Sort = 300,
                           Remark = "T4扩权仓库",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedWare = await ReadApiDataAsync<WareDto>(createExpanded);
                Assert.Equal(expandedWareCode, expandedWare.Code);
                expandedWareId = expandedWare.Id;
                registry.Register<Ware>(expandedWare.Id, nameof(Ware.Code), expandedWareCode);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/wares",
                       new UpdateWareDto
                       {
                           Id = expandedWare.Id,
                           Code = expandedWareCode,
                           Name = expandedWareName,
                           ContactName = "扩权仓管-已更新",
                           ContactPhone = "13800139212",
                           Address = "上海市松江区扩权物流园 2 号",
                           Sort = 310,
                           Remark = "T4扩权仓库-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/wares/{expandedWare.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.Wares.AnyAsync(item =>
                    item.Id == expandedWare.Id || item.Code == expandedWareCode));
                expandedWareId = null;
            }

            // 缩权后写权限与写菜单路径收口
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
                Assert.Contains(storageReadPermission, info.Permissions);
                Assert.DoesNotContain(storageCreatePermission, info.Permissions);
                Assert.DoesNotContain(storageUpdatePermission, info.Permissions);
                Assert.DoesNotContain(storageDeletePermission, info.Permissions);
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
                       "/api/wares",
                       new CreateWareDto
                       {
                           Code = $"{batch.Id}F",
                           Name = $"{batch.Id}-缩权拒绝仓",
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateAfterShrink, ResponseCode.Forbidden);
            }

            // 删除目标仓库
            using (var deleteTarget = await adminClient.DeleteAsync($"/api/wares/{targetWare.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteTarget.StatusCode);
            }

            await using (var afterDeleteTarget = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteTarget.Wares.AnyAsync(item =>
                    item.Id == targetWare.Id || item.Code == targetWareCode));
                targetWareId = null;
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

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUsernames = new[]
                {
                    adminUsername,
                    limitedUsername
                };

                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };

                // 兜底清理可能残留的仓库
                var residualWareCodes = new List<string>
                {
                    targetWareCode,
                    batchWareCode,
                    expandedWareCode,
                    deniedWareCode,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualWareIds = new List<Guid>();
                if (targetWareId.HasValue)
                    residualWareIds.Add(targetWareId.Value);
                if (batchWareId.HasValue)
                    residualWareIds.Add(batchWareId.Value);
                if (expandedWareId.HasValue)
                    residualWareIds.Add(expandedWareId.Value);

                var residualWares = await cleanupContext.Wares
                    .Where(item => residualWareIds.Contains(item.Id)
                                   || residualWareCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualWares.Count > 0)
                {
                    cleanupContext.Wares.RemoveRange(residualWares);
                    await cleanupContext.SaveChangesAsync();
                }

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
                    limitedRoleCode
                };
                var residualRoleIds = new List<Guid> { limitedRoleId };

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
                    seedWareReadButtonId,
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
                || (role.Code != null && role.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Id == seedMenuId
                || menu.Id == writeMenuId
                || menu.Name == seedMenuName
                || menu.Name == writeMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedWareReadButtonId
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
            Assert.False(await residualContext.Wares.AnyAsync(item =>
                item.Code == targetWareCode
                || item.Code == batchWareCode
                || item.Code == expandedWareCode
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            // 既有 Admin 角色与受管仓库必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
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
