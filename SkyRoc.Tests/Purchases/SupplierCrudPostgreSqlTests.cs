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
///     T4 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证供应商档案 CRUD、启停与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class SupplierCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/查询/更新/删除/批量删除供应商并切换启停；最小权限仅读；未认证与无权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task Supplier_CrudStatusAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedSupplierReadButtonId = Guid.NewGuid();
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
        var targetSupplierCode = $"{batch.Id}P";
        var targetSupplierName = $"{batch.Id}-联调蔬菜供应商";
        var batchSupplierCode = $"{batch.Id}B";
        var batchSupplierName = $"{batch.Id}-批量删除供应商";
        var expandedSupplierCode = $"{batch.Id}E";
        var expandedSupplierName = $"{batch.Id}-扩权供应商";
        var deniedSupplierCode = $"{batch.Id}D";
        var deniedSupplierName = $"{batch.Id}-拒绝供应商";
        var password = "SkyRocSupplierPerm!2026";
        var userAgent = $"SkyRoc-T4-Supplier/{batch.Id}";
        var createName = "T4-SupplierCrud";

        var purchaseReadPermission = PermissionCodes.Business.Purchases.Read;
        var purchaseCreatePermission = PermissionCodes.Business.Purchases.Create;
        var purchaseUpdatePermission = PermissionCodes.Business.Purchases.Update;
        var purchaseDeletePermission = PermissionCodes.Business.Purchases.Delete;

        Guid adminRoleId;
        Guid managedSupplierId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            // 复用 T2 受管供应商主数据，仅用于断言不被本轮改动/删除
            var managedSupplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);
            var managedSupplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedSupplierCode);
            Assert.NotNull(managedSupplier);
            managedSupplierId = managedSupplier.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T4 供应商权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T4供应商操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008911",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T4供应商只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900008912",
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
                    Title = "T4供应商只读菜单",
                    Component = "page.t4.supplier.seed",
                    MenuType = MenuType.Menu,
                    Order = 9621,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T4供应商写权限菜单",
                    Component = "page.t4.supplier.write",
                    MenuType = MenuType.Menu,
                    Order = 9622,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedSupplierReadButtonId,
                    Code = purchaseReadPermission,
                    Desc = "T4 采购/供应商读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = purchaseCreatePermission,
                    Desc = "T4 采购/供应商创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = purchaseUpdatePermission,
                    Desc = "T4 采购/供应商更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = purchaseDeletePermission,
                    Desc = "T4 采购/供应商删除权限按钮",
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

        Guid? targetSupplierId = null;
        Guid? batchSupplierId = null;
        Guid? expandedSupplierId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问供应商接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/suppliers"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/suppliers",
                       new CreateSupplierDto
                       {
                           Code = $"{batch.Id}X",
                           Name = $"{batch.Id}-未认证供应商",
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

            // 操作员创建目标供应商（联系人/开户/税号完整业务字段）
            SupplierDto targetSupplier;
            using (var createTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/suppliers",
                       new CreateSupplierDto
                       {
                           Code = targetSupplierCode,
                           Name = targetSupplierName,
                           ContactName = "周供货",
                           ContactPhone = "13800139001",
                           Address = "上海市嘉定区联调农产品批发市场 A 区 12 号",
                           BankName = "中国农业银行上海嘉定支行",
                           BankAccount = "6228480030123456789",
                           TaxNo = "91310000MA1SUPP001",
                           Remark = "T4供应商CRUD切片目标供应商",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createTargetResponse.StatusCode);
                targetSupplier = await ReadApiDataAsync<SupplierDto>(createTargetResponse);
                Assert.Equal(targetSupplierCode, targetSupplier.Code);
                Assert.Equal(targetSupplierName, targetSupplier.Name);
                Assert.Equal("周供货", targetSupplier.ContactName);
                Assert.Equal("13800139001", targetSupplier.ContactPhone);
                Assert.Equal("上海市嘉定区联调农产品批发市场 A 区 12 号", targetSupplier.Address);
                Assert.Equal("中国农业银行上海嘉定支行", targetSupplier.BankName);
                Assert.Equal("6228480030123456789", targetSupplier.BankAccount);
                Assert.Equal("91310000MA1SUPP001", targetSupplier.TaxNo);
                Assert.Equal("T4供应商CRUD切片目标供应商", targetSupplier.Remark);
                Assert.Equal(Status.Enable, targetSupplier.Status);
                targetSupplierId = targetSupplier.Id;
                registry.Register<Supplier>(targetSupplier.Id, nameof(Supplier.Code), targetSupplierCode);
            }

            // 批量删除目标供应商
            SupplierDto batchSupplier;
            using (var createBatchTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/suppliers",
                       new CreateSupplierDto
                       {
                           Code = batchSupplierCode,
                           Name = batchSupplierName,
                           ContactName = "钱批量",
                           ContactPhone = "13800139002",
                           Address = "上海市青浦区批量农批市场 3 号",
                           BankName = "中国建设银行上海青浦支行",
                           BankAccount = "6217001234567890123",
                           TaxNo = "91310000MA1SUPP00B",
                           Remark = "T4供应商CRUD切片批量删除供应商",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBatchTargetResponse.StatusCode);
                batchSupplier = await ReadApiDataAsync<SupplierDto>(createBatchTargetResponse);
                Assert.Equal(batchSupplierCode, batchSupplier.Code);
                batchSupplierId = batchSupplier.Id;
                registry.Register<Supplier>(batchSupplier.Id, nameof(Supplier.Code), batchSupplierCode);
            }

            // 详情应回填业务字段
            using (var detailAfterCreate = await adminClient.GetAsync($"/api/suppliers/{targetSupplier.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterCreate.StatusCode);
                var detail = await ReadApiDataAsync<SupplierDto>(detailAfterCreate);
                Assert.Equal(targetSupplierCode, detail.Code);
                Assert.Equal(targetSupplierName, detail.Name);
                Assert.Equal("周供货", detail.ContactName);
                Assert.Equal("91310000MA1SUPP001", detail.TaxNo);
                Assert.Equal(Status.Enable, detail.Status);
            }

            await using (var entityContext = fixture.CreateDbContext())
            {
                var supplierEntity = await entityContext.Suppliers.AsNoTracking()
                    .SingleAsync(item => item.Id == targetSupplier.Id);
                Assert.Equal(targetSupplierCode, supplierEntity.Code);
                Assert.Equal("T4供应商CRUD切片目标供应商", supplierEntity.Remark);
                Assert.Equal("6228480030123456789", supplierEntity.BankAccount);
            }

            // 分页/全量/详情
            using (var listResponse = await adminClient.GetAsync(
                       $"/api/suppliers/list?current=1&size=20&code={Uri.EscapeDataString(targetSupplierCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<SupplierDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == targetSupplier.Id || item.Code == targetSupplierCode);
            }

            using (var allResponse = await adminClient.GetAsync("/api/suppliers"))
            {
                Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            }

            // 更新供应商业务字段
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/suppliers",
                       new UpdateSupplierDto
                       {
                           Id = targetSupplier.Id,
                           Code = targetSupplierCode,
                           Name = targetSupplierName,
                           ContactName = "周供货-更新",
                           ContactPhone = "13800139999",
                           Address = "上海市嘉定区联调农产品批发市场 B 区 8 号",
                           BankName = "中国农业银行上海嘉定支行",
                           BankAccount = "6228480030987654321",
                           TaxNo = "91310000MA1SUPP001",
                           Remark = "T4供应商CRUD切片目标供应商-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var updated = await ReadApiDataAsync<SupplierDto>(updateResponse);
                Assert.Equal("周供货-更新", updated.ContactName);
                Assert.Equal("13800139999", updated.ContactPhone);
                Assert.Equal("上海市嘉定区联调农产品批发市场 B 区 8 号", updated.Address);
                Assert.Equal("6228480030987654321", updated.BankAccount);
                Assert.Equal("T4供应商CRUD切片目标供应商-已更新", updated.Remark);
            }

            await using (var afterUpdate = fixture.CreateDbContext())
            {
                var supplierEntity = await afterUpdate.Suppliers.AsNoTracking()
                    .SingleAsync(item => item.Id == targetSupplier.Id);
                Assert.Equal("周供货-更新", supplierEntity.ContactName);
                Assert.Equal("T4供应商CRUD切片目标供应商-已更新", supplierEntity.Remark);
            }

            // 停用与启用
            using (var disableResponse = await adminClient.PatchAsync(
                       $"/api/suppliers/{targetSupplier.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
                var disabled = await ReadApiDataAsync<SupplierDto>(disableResponse);
                Assert.Equal(Status.Disable, disabled.Status);
            }

            using (var enableResponse = await adminClient.PatchAsync(
                       $"/api/suppliers/{targetSupplier.Id}/status?status={(int)Status.Enable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
                var enabled = await ReadApiDataAsync<SupplierDto>(enableResponse);
                Assert.Equal(Status.Enable, enabled.Status);
            }

            // 操作员批量删除
            using (var batchDeleteResponse = await adminClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/suppliers/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { batchSupplier.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, batchDeleteResponse.StatusCode);
            }

            await using (var afterBatchDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterBatchDelete.Suppliers.AnyAsync(item =>
                    item.Id == batchSupplier.Id || item.Code == batchSupplierCode));
                batchSupplierId = null;
            }

            // 最小权限用户登录：仅采购读（供应商读）
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

            using (var allowedAll = await limitedClient.GetAsync("/api/suppliers"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/suppliers/{targetSupplier.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/suppliers",
                       new CreateSupplierDto
                       {
                           Code = deniedSupplierCode,
                           Name = deniedSupplierName,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/suppliers",
                       new UpdateSupplierDto
                       {
                           Id = targetSupplier.Id,
                           Code = targetSupplierCode,
                           Name = targetSupplierName,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdate, ResponseCode.Forbidden);
            }

            using (var deniedStatus = await limitedClient.PatchAsync(
                       $"/api/suppliers/{targetSupplier.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedStatus, ResponseCode.Forbidden);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/suppliers/{targetSupplier.Id}"))
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

            SupplierDto expandedSupplier;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/suppliers",
                       new CreateSupplierDto
                       {
                           Code = expandedSupplierCode,
                           Name = expandedSupplierName,
                           ContactName = "扩权联系人",
                           ContactPhone = "13800139111",
                           Address = "上海市松江区扩权农批市场 1 号",
                           BankName = "交通银行上海松江支行",
                           BankAccount = "6222601234567890123",
                           TaxNo = "91310000MA1SUPP00E",
                           Remark = "T4扩权供应商",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedSupplier = await ReadApiDataAsync<SupplierDto>(createExpanded);
                Assert.Equal(expandedSupplierCode, expandedSupplier.Code);
                expandedSupplierId = expandedSupplier.Id;
                registry.Register<Supplier>(expandedSupplier.Id, nameof(Supplier.Code), expandedSupplierCode);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/suppliers",
                       new UpdateSupplierDto
                       {
                           Id = expandedSupplier.Id,
                           Code = expandedSupplierCode,
                           Name = expandedSupplierName,
                           ContactName = "扩权联系人-已更新",
                           ContactPhone = "13800139112",
                           Address = "上海市松江区扩权农批市场 2 号",
                           BankName = "交通银行上海松江支行",
                           BankAccount = "6222601234567890123",
                           TaxNo = "91310000MA1SUPP00E",
                           Remark = "T4扩权供应商-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/suppliers/{expandedSupplier.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.Suppliers.AnyAsync(item =>
                    item.Id == expandedSupplier.Id || item.Code == expandedSupplierCode));
                expandedSupplierId = null;
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
                       "/api/suppliers",
                       new CreateSupplierDto
                       {
                           Code = $"{batch.Id}F",
                           Name = $"{batch.Id}-缩权拒绝供应商",
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateAfterShrink, ResponseCode.Forbidden);
            }

            // 删除目标供应商
            using (var deleteTarget = await adminClient.DeleteAsync($"/api/suppliers/{targetSupplier.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteTarget.StatusCode);
            }

            await using (var afterDeleteTarget = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteTarget.Suppliers.AnyAsync(item =>
                    item.Id == targetSupplier.Id || item.Code == targetSupplierCode));
                targetSupplierId = null;
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

                // 兜底清理可能残留的供应商
                var residualSupplierCodes = new List<string>
                {
                    targetSupplierCode,
                    batchSupplierCode,
                    expandedSupplierCode,
                    deniedSupplierCode,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualSupplierIds = new List<Guid>();
                if (targetSupplierId.HasValue)
                    residualSupplierIds.Add(targetSupplierId.Value);
                if (batchSupplierId.HasValue)
                    residualSupplierIds.Add(batchSupplierId.Value);
                if (expandedSupplierId.HasValue)
                    residualSupplierIds.Add(expandedSupplierId.Value);

                var residualSuppliers = await cleanupContext.Suppliers
                    .Where(item => residualSupplierIds.Contains(item.Id)
                                   || residualSupplierCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualSuppliers.Count > 0)
                {
                    cleanupContext.Suppliers.RemoveRange(residualSuppliers);
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
                    seedSupplierReadButtonId,
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
                button.Id == seedSupplierReadButtonId
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
            Assert.False(await residualContext.Suppliers.AnyAsync(item =>
                item.Code == targetSupplierCode
                || item.Code == batchSupplierCode
                || item.Code == expandedSupplierCode
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            // 既有 Admin 角色与受管供应商必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
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
