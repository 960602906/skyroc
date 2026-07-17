using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Purchases;
using Application.DTOs.Role;
using Domain.Entities;
using Domain.Entities.Purchases;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Tests.Common;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.Purchases;

/// <summary>
///     T4 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证采购员档案 CRUD、关联用户/部门、启停与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class PurchaserCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/查询/更新/删除/批量删除采购员并维护用户部门关系与启停；最小权限仅读；未认证与无权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task Purchaser_CrudRelationsStatusAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedPurchaserReadButtonId = Guid.NewGuid();
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
        var targetPurchaserCode = $"{batch.Id}P";
        var targetPurchaserName = $"{batch.Id}-联调采购专员";
        var batchPurchaserCode = $"{batch.Id}B";
        var batchPurchaserName = $"{batch.Id}-批量删除采购员";
        var expandedPurchaserCode = $"{batch.Id}E";
        var expandedPurchaserName = $"{batch.Id}-扩权采购员";
        var deniedPurchaserCode = $"{batch.Id}D";
        var deniedPurchaserName = $"{batch.Id}-拒绝采购员";
        var password = "SkyRocPurchaserPerm!2026";
        var userAgent = $"SkyRoc-T4-Purchaser/{batch.Id}";
        var createName = "T4-PurchaserCrud";

        var purchaseReadPermission = PermissionCodes.Business.Purchases.Read;
        var purchaseCreatePermission = PermissionCodes.Business.Purchases.Create;
        var purchaseUpdatePermission = PermissionCodes.Business.Purchases.Update;
        var purchaseDeletePermission = PermissionCodes.Business.Purchases.Delete;

        Guid adminRoleId;
        Guid managedPurchaserId;
        Guid managedUserId;
        string managedUserName = null!;
        Guid managedSecondaryUserId;
        string managedSecondaryUserName = null!;
        Guid managedDepartmentId;
        string managedDepartmentName = null!;
        Guid managedSecondaryDepartmentId;
        string managedSecondaryDepartmentName = null!;

        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            // 复用 T2 受管采购员与组织关系主数据，不修改非受管主数据
            var managedPurchaserCode = DemoDataStableKeyCatalog.Create("PURCHASER", 1);
            var managedPurchaser = await seedContext.Purchasers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedPurchaserCode);
            Assert.NotNull(managedPurchaser);
            managedPurchaserId = managedPurchaser.Id;

            var managedUserCode = DemoDataStableKeyCatalog.Create("SYSTEM-USER", 1);
            var managedSecondaryUserCode = DemoDataStableKeyCatalog.Create("SYSTEM-USER", 2);
            var managedDepartmentCode = DemoDataStableKeyCatalog.Create("DEPARTMENT", 1);
            var managedSecondaryDepartmentCode = DemoDataStableKeyCatalog.Create("DEPARTMENT", 2);

            var managedUser = await seedContext.Users.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Username == managedUserCode);
            Assert.NotNull(managedUser);
            managedUserId = managedUser.Id;
            managedUserName = managedUser.Username;

            var managedSecondaryUser = await seedContext.Users.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Username == managedSecondaryUserCode);
            Assert.NotNull(managedSecondaryUser);
            managedSecondaryUserId = managedSecondaryUser.Id;
            managedSecondaryUserName = managedSecondaryUser.Username;

            var managedDepartment = await seedContext.Departments.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedDepartmentCode);
            Assert.NotNull(managedDepartment);
            managedDepartmentId = managedDepartment.Id;
            managedDepartmentName = managedDepartment.Name;

            var managedSecondaryDepartment = await seedContext.Departments.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedSecondaryDepartmentCode);
            Assert.NotNull(managedSecondaryDepartment);
            managedSecondaryDepartmentId = managedSecondaryDepartment.Id;
            managedSecondaryDepartmentName = managedSecondaryDepartment.Name;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T4 采购员权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T4采购员操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008931",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T4采购员只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900008932",
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
                    Title = "T4采购员只读菜单",
                    Component = "page.t4.purchaser.seed",
                    MenuType = MenuType.Menu,
                    Order = 9641,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T4采购员写权限菜单",
                    Component = "page.t4.purchaser.write",
                    MenuType = MenuType.Menu,
                    Order = 9642,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedPurchaserReadButtonId,
                    Code = purchaseReadPermission,
                    Desc = "T4 采购/采购员读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = purchaseCreatePermission,
                    Desc = "T4 采购/采购员创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = purchaseUpdatePermission,
                    Desc = "T4 采购/采购员更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = purchaseDeletePermission,
                    Desc = "T4 采购/采购员删除权限按钮",
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

        Guid? targetPurchaserId = null;
        Guid? batchPurchaserId = null;
        Guid? expandedPurchaserId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问采购员接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/purchasers"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/purchasers",
                       new CreatePurchaserDto
                       {
                           Code = $"{batch.Id}X",
                           Name = $"{batch.Id}-未认证采购员",
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

            // 操作员创建目标采购员并绑定受管用户/部门
            PurchaserDto targetPurchaser;
            using (var createTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/purchasers",
                       new CreatePurchaserDto
                       {
                           Code = targetPurchaserCode,
                           Name = targetPurchaserName,
                           Phone = "13800139301",
                           UserId = managedUserId,
                           DepartmentId = managedDepartmentId,
                           Remark = "T4采购员CRUD切片目标采购员",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createTargetResponse.StatusCode);
                targetPurchaser = await ReadApiDataAsync<PurchaserDto>(createTargetResponse);
                Assert.Equal(targetPurchaserCode, targetPurchaser.Code);
                Assert.Equal(targetPurchaserName, targetPurchaser.Name);
                Assert.Equal("13800139301", targetPurchaser.Phone);
                Assert.Equal(managedUserId, targetPurchaser.UserId);
                Assert.Equal(managedDepartmentId, targetPurchaser.DepartmentId);
                Assert.Equal("T4采购员CRUD切片目标采购员", targetPurchaser.Remark);
                Assert.Equal(Status.Enable, targetPurchaser.Status);
                targetPurchaserId = targetPurchaser.Id;
                registry.Register<Purchaser>(targetPurchaser.Id, nameof(Purchaser.Code), targetPurchaserCode);
            }

            // 批量删除目标采购员
            PurchaserDto batchPurchaser;
            using (var createBatchTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/purchasers",
                       new CreatePurchaserDto
                       {
                           Code = batchPurchaserCode,
                           Name = batchPurchaserName,
                           Phone = "13800139302",
                           UserId = managedSecondaryUserId,
                           DepartmentId = managedSecondaryDepartmentId,
                           Remark = "T4采购员CRUD切片批量删除采购员",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBatchTargetResponse.StatusCode);
                batchPurchaser = await ReadApiDataAsync<PurchaserDto>(createBatchTargetResponse);
                Assert.Equal(batchPurchaserCode, batchPurchaser.Code);
                batchPurchaserId = batchPurchaser.Id;
                registry.Register<Purchaser>(batchPurchaser.Id, nameof(Purchaser.Code), batchPurchaserCode);
            }

            // 详情应回填用户/部门名称
            using (var detailAfterCreate = await adminClient.GetAsync($"/api/purchasers/{targetPurchaser.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterCreate.StatusCode);
                var detail = await ReadApiDataAsync<PurchaserDto>(detailAfterCreate);
                Assert.Equal(targetPurchaserCode, detail.Code);
                Assert.Equal(targetPurchaserName, detail.Name);
                Assert.Equal(managedUserId, detail.UserId);
                Assert.Equal(managedUserName, detail.UserName);
                Assert.Equal(managedDepartmentId, detail.DepartmentId);
                Assert.Equal(managedDepartmentName, detail.DepartmentName);
                Assert.Equal(Status.Enable, detail.Status);
            }

            await using (var entityContext = fixture.CreateDbContext())
            {
                var purchaserEntity = await entityContext.Purchasers.AsNoTracking()
                    .SingleAsync(item => item.Id == targetPurchaser.Id);
                Assert.Equal(targetPurchaserCode, purchaserEntity.Code);
                Assert.Equal("T4采购员CRUD切片目标采购员", purchaserEntity.Remark);
                Assert.Equal(managedUserId, purchaserEntity.UserId);
                Assert.Equal(managedDepartmentId, purchaserEntity.DepartmentId);
            }

            // 分页/全量/详情
            using (var listResponse = await adminClient.GetAsync(
                       $"/api/purchasers/list?current=1&size=20&code={Uri.EscapeDataString(targetPurchaserCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<PurchaserDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == targetPurchaser.Id || item.Code == targetPurchaserCode);
            }

            using (var allResponse = await adminClient.GetAsync("/api/purchasers"))
            {
                Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            }

            // 更新采购员业务字段与关联关系
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/purchasers",
                       new UpdatePurchaserDto
                       {
                           Id = targetPurchaser.Id,
                           Code = targetPurchaserCode,
                           Name = targetPurchaserName,
                           Phone = "13800139992",
                           UserId = managedSecondaryUserId,
                           DepartmentId = managedSecondaryDepartmentId,
                           Remark = "T4采购员CRUD切片目标采购员-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var updated = await ReadApiDataAsync<PurchaserDto>(updateResponse);
                Assert.Equal("13800139992", updated.Phone);
                Assert.Equal(managedSecondaryUserId, updated.UserId);
                Assert.Equal(managedSecondaryDepartmentId, updated.DepartmentId);
                Assert.Equal("T4采购员CRUD切片目标采购员-已更新", updated.Remark);
            }

            using (var detailAfterUpdate = await adminClient.GetAsync($"/api/purchasers/{targetPurchaser.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterUpdate.StatusCode);
                var detail = await ReadApiDataAsync<PurchaserDto>(detailAfterUpdate);
                Assert.Equal(managedSecondaryUserId, detail.UserId);
                Assert.Equal(managedSecondaryUserName, detail.UserName);
                Assert.Equal(managedSecondaryDepartmentId, detail.DepartmentId);
                Assert.Equal(managedSecondaryDepartmentName, detail.DepartmentName);
                Assert.Equal("T4采购员CRUD切片目标采购员-已更新", detail.Remark);
            }

            await using (var afterUpdate = fixture.CreateDbContext())
            {
                var purchaserEntity = await afterUpdate.Purchasers.AsNoTracking()
                    .SingleAsync(item => item.Id == targetPurchaser.Id);
                Assert.Equal("13800139992", purchaserEntity.Phone);
                Assert.Equal(managedSecondaryUserId, purchaserEntity.UserId);
                Assert.Equal(managedSecondaryDepartmentId, purchaserEntity.DepartmentId);
                Assert.Equal("T4采购员CRUD切片目标采购员-已更新", purchaserEntity.Remark);
            }

            // 停用与启用
            using (var disableResponse = await adminClient.PatchAsync(
                       $"/api/purchasers/{targetPurchaser.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
                var disabled = await ReadApiDataAsync<PurchaserDto>(disableResponse);
                Assert.Equal(Status.Disable, disabled.Status);
            }

            using (var enableResponse = await adminClient.PatchAsync(
                       $"/api/purchasers/{targetPurchaser.Id}/status?status={(int)Status.Enable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
                var enabled = await ReadApiDataAsync<PurchaserDto>(enableResponse);
                Assert.Equal(Status.Enable, enabled.Status);
            }

            // 操作员批量删除
            using (var batchDeleteResponse = await adminClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/purchasers/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { batchPurchaser.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, batchDeleteResponse.StatusCode);
            }

            await using (var afterBatchDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterBatchDelete.Purchasers.AnyAsync(item =>
                    item.Id == batchPurchaser.Id || item.Code == batchPurchaserCode));
                batchPurchaserId = null;
            }

            // 最小权限用户登录：仅采购读（采购员读）
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
                Assert.Contains(purchaseReadPermission, info.Permissions);
                Assert.DoesNotContain(purchaseCreatePermission, info.Permissions);
                Assert.DoesNotContain(purchaseUpdatePermission, info.Permissions);
                Assert.DoesNotContain(purchaseDeletePermission, info.Permissions);
            }

            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
            }

            using (var allowedAll = await limitedClient.GetAsync("/api/purchasers"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/purchasers/{targetPurchaser.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/purchasers",
                       new CreatePurchaserDto
                       {
                           Code = deniedPurchaserCode,
                           Name = deniedPurchaserName,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/purchasers",
                       new UpdatePurchaserDto
                       {
                           Id = targetPurchaser.Id,
                           Code = targetPurchaserCode,
                           Name = targetPurchaserName,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdate, ResponseCode.Forbidden);
            }

            using (var deniedStatus = await limitedClient.PatchAsync(
                       $"/api/purchasers/{targetPurchaser.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedStatus, ResponseCode.Forbidden);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/purchasers/{targetPurchaser.Id}"))
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
                Assert.Contains(purchaseReadPermission, info.Permissions);
                Assert.Contains(purchaseCreatePermission, info.Permissions);
                Assert.Contains(purchaseUpdatePermission, info.Permissions);
                Assert.Contains(purchaseDeletePermission, info.Permissions);
            }

            PurchaserDto expandedPurchaser;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/purchasers",
                       new CreatePurchaserDto
                       {
                           Code = expandedPurchaserCode,
                           Name = expandedPurchaserName,
                           Phone = "13800139311",
                           UserId = managedUserId,
                           DepartmentId = managedDepartmentId,
                           Remark = "T4扩权采购员",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedPurchaser = await ReadApiDataAsync<PurchaserDto>(createExpanded);
                Assert.Equal(expandedPurchaserCode, expandedPurchaser.Code);
                expandedPurchaserId = expandedPurchaser.Id;
                registry.Register<Purchaser>(expandedPurchaser.Id, nameof(Purchaser.Code), expandedPurchaserCode);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/purchasers",
                       new UpdatePurchaserDto
                       {
                           Id = expandedPurchaser.Id,
                           Code = expandedPurchaserCode,
                           Name = expandedPurchaserName,
                           Phone = "13800139312",
                           UserId = managedSecondaryUserId,
                           DepartmentId = managedSecondaryDepartmentId,
                           Remark = "T4扩权采购员-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/purchasers/{expandedPurchaser.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.Purchasers.AnyAsync(item =>
                    item.Id == expandedPurchaser.Id || item.Code == expandedPurchaserCode));
                expandedPurchaserId = null;
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
                Assert.Contains(purchaseReadPermission, info.Permissions);
                Assert.DoesNotContain(purchaseCreatePermission, info.Permissions);
                Assert.DoesNotContain(purchaseUpdatePermission, info.Permissions);
                Assert.DoesNotContain(purchaseDeletePermission, info.Permissions);
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
                       "/api/purchasers",
                       new CreatePurchaserDto
                       {
                           Code = $"{batch.Id}F",
                           Name = $"{batch.Id}-缩权拒绝采购员",
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateAfterShrink, ResponseCode.Forbidden);
            }

            // 删除目标采购员
            using (var deleteTarget = await adminClient.DeleteAsync($"/api/purchasers/{targetPurchaser.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteTarget.StatusCode);
            }

            await using (var afterDeleteTarget = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteTarget.Purchasers.AnyAsync(item =>
                    item.Id == targetPurchaser.Id || item.Code == targetPurchaserCode));
                targetPurchaserId = null;
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

                // 兜底清理可能残留的采购员
                var residualPurchaserCodes = new List<string>
                {
                    targetPurchaserCode,
                    batchPurchaserCode,
                    expandedPurchaserCode,
                    deniedPurchaserCode,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualPurchaserIds = new List<Guid>();
                if (targetPurchaserId.HasValue)
                    residualPurchaserIds.Add(targetPurchaserId.Value);
                if (batchPurchaserId.HasValue)
                    residualPurchaserIds.Add(batchPurchaserId.Value);
                if (expandedPurchaserId.HasValue)
                    residualPurchaserIds.Add(expandedPurchaserId.Value);

                var residualPurchasers = await cleanupContext.Purchasers
                    .Where(item => residualPurchaserIds.Contains(item.Id)
                                   || residualPurchaserCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualPurchasers.Count > 0)
                {
                    cleanupContext.Purchasers.RemoveRange(residualPurchasers);
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
                    seedPurchaserReadButtonId,
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
                button.Id == seedPurchaserReadButtonId
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
            Assert.False(await residualContext.Purchasers.AnyAsync(item =>
                item.Code == targetPurchaserCode
                || item.Code == batchPurchaserCode
                || item.Code == expandedPurchaserCode
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            // 既有 Admin 角色、受管采购员与组织关系主数据必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Purchasers.AnyAsync(item => item.Id == managedPurchaserId));
            Assert.True(await residualContext.Users.AnyAsync(item => item.Id == managedUserId));
            Assert.True(await residualContext.Users.AnyAsync(item => item.Id == managedSecondaryUserId));
            Assert.True(await residualContext.Departments.AnyAsync(item => item.Id == managedDepartmentId));
            Assert.True(await residualContext.Departments.AnyAsync(item => item.Id == managedSecondaryDepartmentId));
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
