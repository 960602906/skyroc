using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Customers;
using Application.DTOs.Role;
using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.Customers;

/// <summary>
///     T4 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证客户标签树形 CRUD、排序备注、启停与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class CustomerTagCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建根/子标签、树查询、更新排序备注、启停、批量删除；有子级时删除根拒绝；最小权限仅读；未认证与无权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task CustomerTag_CrudTreeStatusAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedCustomerTagReadButtonId = Guid.NewGuid();
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
        var rootTagCode = $"{batch.Id}P";
        var rootTagName = $"{batch.Id}-联调根标签";
        var childTagCode = $"{batch.Id}C";
        var childTagName = $"{batch.Id}-联调子标签";
        var batchTagCode = $"{batch.Id}B";
        var batchTagName = $"{batch.Id}-批量删除标签";
        var expandedTagCode = $"{batch.Id}E";
        var expandedTagName = $"{batch.Id}-扩权标签";
        var deniedTagCode = $"{batch.Id}D";
        var deniedTagName = $"{batch.Id}-拒绝标签";
        var password = "SkyRocCustomerTagPerm!2026";
        var userAgent = $"SkyRoc-T4-CustomerTag/{batch.Id}";
        var createName = "T4-CustomerTagCrud";

        var customerReadPermission = PermissionCodes.Business.Customers.Read;
        var customerCreatePermission = PermissionCodes.Business.Customers.Create;
        var customerUpdatePermission = PermissionCodes.Business.Customers.Update;
        var customerDeletePermission = PermissionCodes.Business.Customers.Delete;

        Guid adminRoleId;
        Guid managedCustomerTagId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            // 复用 T2 受管客户标签主数据，仅用于断言不被本轮改动/删除
            var managedCustomerTagCode = DemoDataStableKeyCatalog.Create("CUSTOMER-TAG", 1);
            var managedCustomerTag = await seedContext.CustomerTags.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedCustomerTagCode);
            Assert.NotNull(managedCustomerTag);
            managedCustomerTagId = managedCustomerTag.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T4 客户标签权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T4客户标签操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008951",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T4客户标签只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900008952",
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
                    Title = "T4客户标签只读菜单",
                    Component = "page.t4.customer-tag.seed",
                    MenuType = MenuType.Menu,
                    Order = 9661,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T4客户标签写权限菜单",
                    Component = "page.t4.customer-tag.write",
                    MenuType = MenuType.Menu,
                    Order = 9662,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedCustomerTagReadButtonId,
                    Code = customerReadPermission,
                    Desc = "T4 客户/标签读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = customerCreatePermission,
                    Desc = "T4 客户/标签创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = customerUpdatePermission,
                    Desc = "T4 客户/标签更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = customerDeletePermission,
                    Desc = "T4 客户/标签删除权限按钮",
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

        Guid? rootTagId = null;
        Guid? childTagId = null;
        Guid? batchTagId = null;
        Guid? expandedTagId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问客户标签接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/customer-tags"))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousList.StatusCode);
            }

            using (var anonymousTree = await anonymousClient.GetAsync("/api/customer-tags/tree"))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousTree.StatusCode);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/customer-tags",
                       new CreateCustomerTagDto
                       {
                           Code = $"{batch.Id}X",
                           Name = $"{batch.Id}-未认证标签",
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

            // 操作员创建根标签并填写排序/备注
            CustomerTagDto rootTag;
            using (var createRootResponse = await adminClient.PostAsJsonAsync(
                       "/api/customer-tags",
                       new CreateCustomerTagDto
                       {
                           Code = rootTagCode,
                           Name = rootTagName,
                           Sort = 100,
                           Remark = "T4客户标签CRUD切片根标签",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createRootResponse.StatusCode);
                rootTag = await ReadApiDataAsync<CustomerTagDto>(createRootResponse);
                Assert.Equal(rootTagCode, rootTag.Code);
                Assert.Equal(rootTagName, rootTag.Name);
                Assert.Null(rootTag.ParentId);
                Assert.Equal(100, rootTag.Sort);
                Assert.Equal("T4客户标签CRUD切片根标签", rootTag.Remark);
                Assert.Equal(Status.Enable, rootTag.Status);
                rootTagId = rootTag.Id;
                registry.Register<CustomerTag>(rootTag.Id, nameof(CustomerTag.Code), rootTagCode);
            }

            // 操作员创建子标签挂到根下
            CustomerTagDto childTag;
            using (var createChildResponse = await adminClient.PostAsJsonAsync(
                       "/api/customer-tags",
                       new CreateCustomerTagDto
                       {
                           Code = childTagCode,
                           Name = childTagName,
                           ParentId = rootTag.Id,
                           Sort = 110,
                           Remark = "T4客户标签CRUD切片子标签",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createChildResponse.StatusCode);
                childTag = await ReadApiDataAsync<CustomerTagDto>(createChildResponse);
                Assert.Equal(childTagCode, childTag.Code);
                Assert.Equal(childTagName, childTag.Name);
                Assert.Equal(rootTag.Id, childTag.ParentId);
                Assert.Equal(110, childTag.Sort);
                Assert.Equal("T4客户标签CRUD切片子标签", childTag.Remark);
                childTagId = childTag.Id;
                registry.Register<CustomerTag>(childTag.Id, nameof(CustomerTag.Code), childTagCode);
            }

            // 批量删除目标标签（无子级独立节点）
            CustomerTagDto batchTag;
            using (var createBatchTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/customer-tags",
                       new CreateCustomerTagDto
                       {
                           Code = batchTagCode,
                           Name = batchTagName,
                           Sort = 200,
                           Remark = "T4客户标签CRUD切片批量删除标签",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBatchTargetResponse.StatusCode);
                batchTag = await ReadApiDataAsync<CustomerTagDto>(createBatchTargetResponse);
                Assert.Equal(batchTagCode, batchTag.Code);
                batchTagId = batchTag.Id;
                registry.Register<CustomerTag>(batchTag.Id, nameof(CustomerTag.Code), batchTagCode);
            }

            // 详情应回填树字段与业务备注
            using (var detailAfterCreate = await adminClient.GetAsync($"/api/customer-tags/{rootTag.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterCreate.StatusCode);
                var detail = await ReadApiDataAsync<CustomerTagDto>(detailAfterCreate);
                Assert.Equal(rootTagCode, detail.Code);
                Assert.Equal(rootTagName, detail.Name);
                Assert.Null(detail.ParentId);
                Assert.Equal(100, detail.Sort);
                Assert.Equal("T4客户标签CRUD切片根标签", detail.Remark);
                Assert.Equal(Status.Enable, detail.Status);
            }

            using (var childDetail = await adminClient.GetAsync($"/api/customer-tags/{childTag.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, childDetail.StatusCode);
                var detail = await ReadApiDataAsync<CustomerTagDto>(childDetail);
                Assert.Equal(rootTag.Id, detail.ParentId);
                Assert.Equal(110, detail.Sort);
            }

            await using (var entityContext = fixture.CreateDbContext())
            {
                var rootEntity = await entityContext.CustomerTags.AsNoTracking()
                    .SingleAsync(item => item.Id == rootTag.Id);
                Assert.Equal(rootTagCode, rootEntity.Code);
                Assert.Equal("T4客户标签CRUD切片根标签", rootEntity.Remark);
                Assert.Null(rootEntity.ParentId);

                var childEntity = await entityContext.CustomerTags.AsNoTracking()
                    .SingleAsync(item => item.Id == childTag.Id);
                Assert.Equal(rootTag.Id, childEntity.ParentId);
                Assert.Equal(110, childEntity.Sort);
            }

            // 树/分页/全量
            using (var treeResponse = await adminClient.GetAsync("/api/customer-tags/tree"))
            {
                Assert.Equal(HttpStatusCode.OK, treeResponse.StatusCode);
                var tree = await ReadApiDataAsync<PagedResult<CustomerTagDto>>(treeResponse);
                Assert.NotNull(tree.Records);
                var rootNode = tree.Records!.FirstOrDefault(item => item.Id == rootTag.Id || item.Code == rootTagCode);
                Assert.NotNull(rootNode);
                Assert.NotNull(rootNode!.Children);
                Assert.Contains(rootNode.Children!, item => item.Id == childTag.Id || item.Code == childTagCode);
            }

            using (var listResponse = await adminClient.GetAsync(
                       $"/api/customer-tags/list?current=1&size=20&code={Uri.EscapeDataString(rootTagCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<CustomerTagDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == rootTag.Id || item.Code == rootTagCode);
            }

            using (var allResponse = await adminClient.GetAsync("/api/customer-tags"))
            {
                Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            }

            // 更新根标签排序/备注
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/customer-tags",
                       new UpdateCustomerTagDto
                       {
                           Id = rootTag.Id,
                           Code = rootTagCode,
                           Name = rootTagName,
                           Sort = 150,
                           Remark = "T4客户标签CRUD切片根标签-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var updated = await ReadApiDataAsync<CustomerTagDto>(updateResponse);
                Assert.Equal(150, updated.Sort);
                Assert.Equal("T4客户标签CRUD切片根标签-已更新", updated.Remark);
            }

            await using (var afterUpdate = fixture.CreateDbContext())
            {
                var rootEntity = await afterUpdate.CustomerTags.AsNoTracking()
                    .SingleAsync(item => item.Id == rootTag.Id);
                Assert.Equal(150, rootEntity.Sort);
                Assert.Equal("T4客户标签CRUD切片根标签-已更新", rootEntity.Remark);
            }

            // 停用与启用子标签
            using (var disableResponse = await adminClient.PatchAsync(
                       $"/api/customer-tags/{childTag.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
                var disabled = await ReadApiDataAsync<CustomerTagDto>(disableResponse);
                Assert.Equal(Status.Disable, disabled.Status);
            }

            using (var enableResponse = await adminClient.PatchAsync(
                       $"/api/customer-tags/{childTag.Id}/status?status={(int)Status.Enable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
                var enabled = await ReadApiDataAsync<CustomerTagDto>(enableResponse);
                Assert.Equal(Status.Enable, enabled.Status);
            }

            // 有子标签时删除根标签应业务失败（BusinessException → 502）
            using (var deleteRootWithChild = await adminClient.DeleteAsync($"/api/customer-tags/{rootTag.Id}"))
            {
                Assert.Equal(HttpStatusCode.BadGateway, deleteRootWithChild.StatusCode);
            }

            await using (var afterFailedDelete = fixture.CreateDbContext())
            {
                Assert.True(await afterFailedDelete.CustomerTags.AnyAsync(item => item.Id == rootTag.Id));
                Assert.True(await afterFailedDelete.CustomerTags.AnyAsync(item => item.Id == childTag.Id));
            }

            // 操作员批量删除无子级节点
            using (var batchDeleteResponse = await adminClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/customer-tags/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { batchTag.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, batchDeleteResponse.StatusCode);
            }

            await using (var afterBatchDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterBatchDelete.CustomerTags.AnyAsync(item =>
                    item.Id == batchTag.Id || item.Code == batchTagCode));
                batchTagId = null;
            }

            // 最小权限用户登录：仅客户读（标签读）
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
                Assert.Contains(customerReadPermission, info.Permissions);
                Assert.DoesNotContain(customerCreatePermission, info.Permissions);
                Assert.DoesNotContain(customerUpdatePermission, info.Permissions);
                Assert.DoesNotContain(customerDeletePermission, info.Permissions);
            }

            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
            }

            using (var allowedAll = await limitedClient.GetAsync("/api/customer-tags"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedTree = await limitedClient.GetAsync("/api/customer-tags/tree"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedTree.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/customer-tags/{rootTag.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/customer-tags",
                       new CreateCustomerTagDto
                       {
                           Code = deniedTagCode,
                           Name = deniedTagName,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedCreate.StatusCode);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/customer-tags",
                       new UpdateCustomerTagDto
                       {
                           Id = rootTag.Id,
                           Code = rootTagCode,
                           Name = rootTagName,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedUpdate.StatusCode);
            }

            using (var deniedStatus = await limitedClient.PatchAsync(
                       $"/api/customer-tags/{childTag.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedStatus.StatusCode);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/customer-tags/{childTag.Id}"))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedDelete.StatusCode);
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
                Assert.Contains(customerReadPermission, info.Permissions);
                Assert.Contains(customerCreatePermission, info.Permissions);
                Assert.Contains(customerUpdatePermission, info.Permissions);
                Assert.Contains(customerDeletePermission, info.Permissions);
            }

            CustomerTagDto expandedTag;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/customer-tags",
                       new CreateCustomerTagDto
                       {
                           Code = expandedTagCode,
                           Name = expandedTagName,
                           Sort = 300,
                           Remark = "T4扩权客户标签",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedTag = await ReadApiDataAsync<CustomerTagDto>(createExpanded);
                Assert.Equal(expandedTagCode, expandedTag.Code);
                expandedTagId = expandedTag.Id;
                registry.Register<CustomerTag>(expandedTag.Id, nameof(CustomerTag.Code), expandedTagCode);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/customer-tags",
                       new UpdateCustomerTagDto
                       {
                           Id = expandedTag.Id,
                           Code = expandedTagCode,
                           Name = expandedTagName,
                           Sort = 310,
                           Remark = "T4扩权客户标签-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/customer-tags/{expandedTag.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.CustomerTags.AnyAsync(item =>
                    item.Id == expandedTag.Id || item.Code == expandedTagCode));
                expandedTagId = null;
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
                Assert.Contains(customerReadPermission, info.Permissions);
                Assert.DoesNotContain(customerCreatePermission, info.Permissions);
                Assert.DoesNotContain(customerUpdatePermission, info.Permissions);
                Assert.DoesNotContain(customerDeletePermission, info.Permissions);
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
                       "/api/customer-tags",
                       new CreateCustomerTagDto
                       {
                           Code = $"{batch.Id}F",
                           Name = $"{batch.Id}-缩权拒绝标签",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedCreateAfterShrink.StatusCode);
            }

            // 先删子后删根
            using (var deleteChild = await adminClient.DeleteAsync($"/api/customer-tags/{childTag.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteChild.StatusCode);
            }

            await using (var afterDeleteChild = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteChild.CustomerTags.AnyAsync(item =>
                    item.Id == childTag.Id || item.Code == childTagCode));
                childTagId = null;
            }

            using (var deleteRoot = await adminClient.DeleteAsync($"/api/customer-tags/{rootTag.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteRoot.StatusCode);
            }

            await using (var afterDeleteRoot = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteRoot.CustomerTags.AnyAsync(item =>
                    item.Id == rootTag.Id || item.Code == rootTagCode));
                rootTagId = null;
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

                // 兜底清理可能残留的客户标签（先子后根）
                var residualTagCodes = new List<string>
                {
                    rootTagCode,
                    childTagCode,
                    batchTagCode,
                    expandedTagCode,
                    deniedTagCode,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualTagIds = new List<Guid>();
                if (rootTagId.HasValue)
                    residualTagIds.Add(rootTagId.Value);
                if (childTagId.HasValue)
                    residualTagIds.Add(childTagId.Value);
                if (batchTagId.HasValue)
                    residualTagIds.Add(batchTagId.Value);
                if (expandedTagId.HasValue)
                    residualTagIds.Add(expandedTagId.Value);

                var residualTags = await cleanupContext.CustomerTags
                    .Where(item => residualTagIds.Contains(item.Id)
                                   || residualTagCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualTags.Count > 0)
                {
                    // 先删有 ParentId 的子节点，再删根，避免外键阻碍
                    var children = residualTags.Where(item => item.ParentId.HasValue).ToList();
                    var roots = residualTags.Where(item => !item.ParentId.HasValue).ToList();
                    if (children.Count > 0)
                    {
                        cleanupContext.CustomerTags.RemoveRange(children);
                        await cleanupContext.SaveChangesAsync();
                    }

                    if (roots.Count > 0)
                    {
                        cleanupContext.CustomerTags.RemoveRange(roots);
                        await cleanupContext.SaveChangesAsync();
                    }
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
                    seedCustomerTagReadButtonId,
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
                button.Id == seedCustomerTagReadButtonId
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
            Assert.False(await residualContext.CustomerTags.AnyAsync(item =>
                item.Code == rootTagCode
                || item.Code == childTagCode
                || item.Code == batchTagCode
                || item.Code == expandedTagCode
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            // 既有 Admin 角色与受管客户标签必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.CustomerTags.AnyAsync(item => item.Id == managedCustomerTagId));
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
