using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Goods;
using Application.DTOs.Role;
using Domain.Entities;
using Domain.Entities.Goods;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Tests.Common;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.Goods;

/// <summary>
///     T4 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证商品分类树形 CRUD、税务字段、启停与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class GoodsTypeCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建根/子分类、树查询、更新税务字段、启停、批量删除；有子级时删除根拒绝；最小权限仅读；未认证与无权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task GoodsType_CrudTreeTaxStatusAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedGoodsTypeReadButtonId = Guid.NewGuid();
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
        var rootTypeCode = $"{batch.Id}P";
        var rootTypeName = $"{batch.Id}-联调根分类";
        var childTypeCode = $"{batch.Id}C";
        var childTypeName = $"{batch.Id}-联调子分类";
        var batchTypeCode = $"{batch.Id}B";
        var batchTypeName = $"{batch.Id}-批量删除分类";
        var expandedTypeCode = $"{batch.Id}E";
        var expandedTypeName = $"{batch.Id}-扩权分类";
        var deniedTypeCode = $"{batch.Id}D";
        var deniedTypeName = $"{batch.Id}-拒绝分类";
        var password = "SkyRocGoodsTypePerm!2026";
        var userAgent = $"SkyRoc-T4-GoodsType/{batch.Id}";
        var createName = "T4-GoodsTypeCrud";

        var goodsReadPermission = PermissionCodes.Business.Goods.Read;
        var goodsCreatePermission = PermissionCodes.Business.Goods.Create;
        var goodsUpdatePermission = PermissionCodes.Business.Goods.Update;
        var goodsDeletePermission = PermissionCodes.Business.Goods.Delete;

        Guid adminRoleId;
        Guid managedGoodsTypeId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            // 复用 T2 受管商品分类主数据，仅用于断言不被本轮改动/删除
            var managedGoodsTypeCode = DemoDataStableKeyCatalog.Create("GOODS-TYPE", 1);
            var managedGoodsType = await seedContext.GoodsTypes.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedGoodsTypeCode);
            Assert.NotNull(managedGoodsType);
            managedGoodsTypeId = managedGoodsType.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T4 商品分类权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T4商品分类操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008941",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T4商品分类只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900008942",
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
                    Title = "T4商品分类只读菜单",
                    Component = "page.t4.goods-type.seed",
                    MenuType = MenuType.Menu,
                    Order = 9651,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T4商品分类写权限菜单",
                    Component = "page.t4.goods-type.write",
                    MenuType = MenuType.Menu,
                    Order = 9652,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedGoodsTypeReadButtonId,
                    Code = goodsReadPermission,
                    Desc = "T4 商品/分类读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = goodsCreatePermission,
                    Desc = "T4 商品/分类创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = goodsUpdatePermission,
                    Desc = "T4 商品/分类更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = goodsDeletePermission,
                    Desc = "T4 商品/分类删除权限按钮",
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

        Guid? rootTypeId = null;
        Guid? childTypeId = null;
        Guid? batchTypeId = null;
        Guid? expandedTypeId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问商品分类接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/goods-types"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousTree = await anonymousClient.GetAsync("/api/goods-types/tree"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousTree, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/goods-types",
                       new CreateGoodsTypeDto
                       {
                           Code = $"{batch.Id}X",
                           Name = $"{batch.Id}-未认证分类",
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

            // 操作员创建根分类并填写税务/排序字段
            GoodsTypeDto rootType;
            using (var createRootResponse = await adminClient.PostAsJsonAsync(
                       "/api/goods-types",
                       new CreateGoodsTypeDto
                       {
                           Code = rootTypeCode,
                           Name = rootTypeName,
                           ImageUrl = "https://cdn.skyroc-autotest.example/goods-type/root.png",
                           TaxCategoryCode = "1010101010000000000",
                           TaxCategoryName = "蔬菜",
                           InvoiceGoodsShortName = "*蔬菜*",
                           DefaultTaxRate = 0.09m,
                           IsTaxExempt = false,
                           TaxPolicyBasis = "生鲜农产品适用增值税率",
                           Sort = 100,
                           Remark = "T4商品分类CRUD切片根分类",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createRootResponse.StatusCode);
                rootType = await ReadApiDataAsync<GoodsTypeDto>(createRootResponse);
                Assert.Equal(rootTypeCode, rootType.Code);
                Assert.Equal(rootTypeName, rootType.Name);
                Assert.Null(rootType.ParentId);
                Assert.Equal("https://cdn.skyroc-autotest.example/goods-type/root.png", rootType.ImageUrl);
                Assert.Equal("1010101010000000000", rootType.TaxCategoryCode);
                Assert.Equal("蔬菜", rootType.TaxCategoryName);
                Assert.Equal("*蔬菜*", rootType.InvoiceGoodsShortName);
                Assert.Equal(0.09m, rootType.DefaultTaxRate);
                Assert.False(rootType.IsTaxExempt);
                Assert.Equal("生鲜农产品适用增值税率", rootType.TaxPolicyBasis);
                Assert.Equal(100, rootType.Sort);
                Assert.Equal("T4商品分类CRUD切片根分类", rootType.Remark);
                Assert.Equal(Status.Enable, rootType.Status);
                rootTypeId = rootType.Id;
                registry.Register<GoodsType>(rootType.Id, nameof(GoodsType.Code), rootTypeCode);
            }

            // 操作员创建子分类挂到根下
            GoodsTypeDto childType;
            using (var createChildResponse = await adminClient.PostAsJsonAsync(
                       "/api/goods-types",
                       new CreateGoodsTypeDto
                       {
                           Code = childTypeCode,
                           Name = childTypeName,
                           ParentId = rootType.Id,
                           TaxCategoryCode = "1010101020000000000",
                           TaxCategoryName = "叶菜",
                           InvoiceGoodsShortName = "*叶菜*",
                           DefaultTaxRate = 0.00m,
                           IsTaxExempt = true,
                           TaxPolicyBasis = "叶菜免税政策依据",
                           Sort = 110,
                           Remark = "T4商品分类CRUD切片子分类",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createChildResponse.StatusCode);
                childType = await ReadApiDataAsync<GoodsTypeDto>(createChildResponse);
                Assert.Equal(childTypeCode, childType.Code);
                Assert.Equal(childTypeName, childType.Name);
                Assert.Equal(rootType.Id, childType.ParentId);
                Assert.True(childType.IsTaxExempt);
                Assert.Equal(0.00m, childType.DefaultTaxRate);
                Assert.Equal(110, childType.Sort);
                childTypeId = childType.Id;
                registry.Register<GoodsType>(childType.Id, nameof(GoodsType.Code), childTypeCode);
            }

            // 批量删除目标分类（无子级独立节点）
            GoodsTypeDto batchType;
            using (var createBatchTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/goods-types",
                       new CreateGoodsTypeDto
                       {
                           Code = batchTypeCode,
                           Name = batchTypeName,
                           Sort = 200,
                           Remark = "T4商品分类CRUD切片批量删除分类",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBatchTargetResponse.StatusCode);
                batchType = await ReadApiDataAsync<GoodsTypeDto>(createBatchTargetResponse);
                Assert.Equal(batchTypeCode, batchType.Code);
                batchTypeId = batchType.Id;
                registry.Register<GoodsType>(batchType.Id, nameof(GoodsType.Code), batchTypeCode);
            }

            // 详情应回填税务与树字段
            using (var detailAfterCreate = await adminClient.GetAsync($"/api/goods-types/{rootType.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterCreate.StatusCode);
                var detail = await ReadApiDataAsync<GoodsTypeDto>(detailAfterCreate);
                Assert.Equal(rootTypeCode, detail.Code);
                Assert.Equal(rootTypeName, detail.Name);
                Assert.Equal("1010101010000000000", detail.TaxCategoryCode);
                Assert.Equal("*蔬菜*", detail.InvoiceGoodsShortName);
                Assert.Equal(0.09m, detail.DefaultTaxRate);
                Assert.Equal(Status.Enable, detail.Status);
            }

            using (var childDetail = await adminClient.GetAsync($"/api/goods-types/{childType.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, childDetail.StatusCode);
                var detail = await ReadApiDataAsync<GoodsTypeDto>(childDetail);
                Assert.Equal(rootType.Id, detail.ParentId);
                Assert.True(detail.IsTaxExempt);
            }

            await using (var entityContext = fixture.CreateDbContext())
            {
                var rootEntity = await entityContext.GoodsTypes.AsNoTracking()
                    .SingleAsync(item => item.Id == rootType.Id);
                Assert.Equal(rootTypeCode, rootEntity.Code);
                Assert.Equal("T4商品分类CRUD切片根分类", rootEntity.Remark);
                Assert.Equal(0.09m, rootEntity.DefaultTaxRate);
                Assert.Null(rootEntity.ParentId);

                var childEntity = await entityContext.GoodsTypes.AsNoTracking()
                    .SingleAsync(item => item.Id == childType.Id);
                Assert.Equal(rootType.Id, childEntity.ParentId);
                Assert.True(childEntity.IsTaxExempt);
            }

            // 树/分页/全量
            using (var treeResponse = await adminClient.GetAsync("/api/goods-types/tree"))
            {
                Assert.Equal(HttpStatusCode.OK, treeResponse.StatusCode);
                var tree = await ReadApiDataAsync<PagedResult<GoodsTypeDto>>(treeResponse);
                Assert.NotNull(tree.Records);
                var rootNode = tree.Records!.FirstOrDefault(item => item.Id == rootType.Id || item.Code == rootTypeCode);
                Assert.NotNull(rootNode);
                Assert.NotNull(rootNode!.Children);
                Assert.Contains(rootNode.Children!, item => item.Id == childType.Id || item.Code == childTypeCode);
            }

            using (var listResponse = await adminClient.GetAsync(
                       $"/api/goods-types/list?current=1&size=20&code={Uri.EscapeDataString(rootTypeCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<GoodsTypeDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == rootType.Id || item.Code == rootTypeCode);
            }

            using (var allResponse = await adminClient.GetAsync("/api/goods-types"))
            {
                Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            }

            // 更新根分类业务/税务字段
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/goods-types",
                       new UpdateGoodsTypeDto
                       {
                           Id = rootType.Id,
                           Code = rootTypeCode,
                           Name = rootTypeName,
                           ImageUrl = "https://cdn.skyroc-autotest.example/goods-type/root-updated.png",
                           TaxCategoryCode = "1010101010000000001",
                           TaxCategoryName = "根茎蔬菜",
                           InvoiceGoodsShortName = "*根茎蔬菜*",
                           DefaultTaxRate = 0.13m,
                           IsTaxExempt = false,
                           TaxPolicyBasis = "根茎蔬菜适用增值税率-已更新",
                           Sort = 150,
                           Remark = "T4商品分类CRUD切片根分类-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var updated = await ReadApiDataAsync<GoodsTypeDto>(updateResponse);
                Assert.Equal("1010101010000000001", updated.TaxCategoryCode);
                Assert.Equal("根茎蔬菜", updated.TaxCategoryName);
                Assert.Equal("*根茎蔬菜*", updated.InvoiceGoodsShortName);
                Assert.Equal(0.13m, updated.DefaultTaxRate);
                Assert.Equal(150, updated.Sort);
                Assert.Equal("T4商品分类CRUD切片根分类-已更新", updated.Remark);
            }

            await using (var afterUpdate = fixture.CreateDbContext())
            {
                var rootEntity = await afterUpdate.GoodsTypes.AsNoTracking()
                    .SingleAsync(item => item.Id == rootType.Id);
                Assert.Equal(0.13m, rootEntity.DefaultTaxRate);
                Assert.Equal("T4商品分类CRUD切片根分类-已更新", rootEntity.Remark);
                Assert.Equal(150, rootEntity.Sort);
            }

            // 停用与启用子分类
            using (var disableResponse = await adminClient.PatchAsync(
                       $"/api/goods-types/{childType.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
                var disabled = await ReadApiDataAsync<GoodsTypeDto>(disableResponse);
                Assert.Equal(Status.Disable, disabled.Status);
            }

            using (var enableResponse = await adminClient.PatchAsync(
                       $"/api/goods-types/{childType.Id}/status?status={(int)Status.Enable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
                var enabled = await ReadApiDataAsync<GoodsTypeDto>(enableResponse);
                Assert.Equal(Status.Enable, enabled.Status);
            }

            // 有子分类时删除根分类应业务失败（BusinessException → 502）
            using (var deleteRootWithChild = await adminClient.DeleteAsync($"/api/goods-types/{rootType.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deleteRootWithChild, ResponseCode.DatabaseError);
            }

            await using (var afterFailedDelete = fixture.CreateDbContext())
            {
                Assert.True(await afterFailedDelete.GoodsTypes.AnyAsync(item => item.Id == rootType.Id));
                Assert.True(await afterFailedDelete.GoodsTypes.AnyAsync(item => item.Id == childType.Id));
            }

            // 操作员批量删除无子级节点
            using (var batchDeleteResponse = await adminClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/goods-types/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { batchType.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, batchDeleteResponse.StatusCode);
            }

            await using (var afterBatchDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterBatchDelete.GoodsTypes.AnyAsync(item =>
                    item.Id == batchType.Id || item.Code == batchTypeCode));
                batchTypeId = null;
            }

            // 最小权限用户登录：仅商品读（分类读）
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
                Assert.Contains(goodsReadPermission, info.Permissions);
                Assert.DoesNotContain(goodsCreatePermission, info.Permissions);
                Assert.DoesNotContain(goodsUpdatePermission, info.Permissions);
                Assert.DoesNotContain(goodsDeletePermission, info.Permissions);
            }

            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
            }

            using (var allowedAll = await limitedClient.GetAsync("/api/goods-types"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedTree = await limitedClient.GetAsync("/api/goods-types/tree"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedTree.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/goods-types/{rootType.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/goods-types",
                       new CreateGoodsTypeDto
                       {
                           Code = deniedTypeCode,
                           Name = deniedTypeName,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/goods-types",
                       new UpdateGoodsTypeDto
                       {
                           Id = rootType.Id,
                           Code = rootTypeCode,
                           Name = rootTypeName,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdate, ResponseCode.Forbidden);
            }

            using (var deniedStatus = await limitedClient.PatchAsync(
                       $"/api/goods-types/{childType.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedStatus, ResponseCode.Forbidden);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/goods-types/{childType.Id}"))
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
                Assert.Contains(goodsReadPermission, info.Permissions);
                Assert.Contains(goodsCreatePermission, info.Permissions);
                Assert.Contains(goodsUpdatePermission, info.Permissions);
                Assert.Contains(goodsDeletePermission, info.Permissions);
            }

            GoodsTypeDto expandedType;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/goods-types",
                       new CreateGoodsTypeDto
                       {
                           Code = expandedTypeCode,
                           Name = expandedTypeName,
                           Sort = 300,
                           Remark = "T4扩权商品分类",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedType = await ReadApiDataAsync<GoodsTypeDto>(createExpanded);
                Assert.Equal(expandedTypeCode, expandedType.Code);
                expandedTypeId = expandedType.Id;
                registry.Register<GoodsType>(expandedType.Id, nameof(GoodsType.Code), expandedTypeCode);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/goods-types",
                       new UpdateGoodsTypeDto
                       {
                           Id = expandedType.Id,
                           Code = expandedTypeCode,
                           Name = expandedTypeName,
                           Sort = 310,
                           Remark = "T4扩权商品分类-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/goods-types/{expandedType.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.GoodsTypes.AnyAsync(item =>
                    item.Id == expandedType.Id || item.Code == expandedTypeCode));
                expandedTypeId = null;
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
                Assert.Contains(goodsReadPermission, info.Permissions);
                Assert.DoesNotContain(goodsCreatePermission, info.Permissions);
                Assert.DoesNotContain(goodsUpdatePermission, info.Permissions);
                Assert.DoesNotContain(goodsDeletePermission, info.Permissions);
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
                       "/api/goods-types",
                       new CreateGoodsTypeDto
                       {
                           Code = $"{batch.Id}F",
                           Name = $"{batch.Id}-缩权拒绝分类",
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateAfterShrink, ResponseCode.Forbidden);
            }

            // 先删子后删根
            using (var deleteChild = await adminClient.DeleteAsync($"/api/goods-types/{childType.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteChild.StatusCode);
            }

            await using (var afterDeleteChild = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteChild.GoodsTypes.AnyAsync(item =>
                    item.Id == childType.Id || item.Code == childTypeCode));
                childTypeId = null;
            }

            using (var deleteRoot = await adminClient.DeleteAsync($"/api/goods-types/{rootType.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteRoot.StatusCode);
            }

            await using (var afterDeleteRoot = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteRoot.GoodsTypes.AnyAsync(item =>
                    item.Id == rootType.Id || item.Code == rootTypeCode));
                rootTypeId = null;
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

                // 兜底清理可能残留的商品分类（先子后根）
                var residualTypeCodes = new List<string>
                {
                    rootTypeCode,
                    childTypeCode,
                    batchTypeCode,
                    expandedTypeCode,
                    deniedTypeCode,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualTypeIds = new List<Guid>();
                if (rootTypeId.HasValue)
                    residualTypeIds.Add(rootTypeId.Value);
                if (childTypeId.HasValue)
                    residualTypeIds.Add(childTypeId.Value);
                if (batchTypeId.HasValue)
                    residualTypeIds.Add(batchTypeId.Value);
                if (expandedTypeId.HasValue)
                    residualTypeIds.Add(expandedTypeId.Value);

                var residualTypes = await cleanupContext.GoodsTypes
                    .Where(item => residualTypeIds.Contains(item.Id)
                                   || residualTypeCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualTypes.Count > 0)
                {
                    // 先删有 ParentId 的子节点，再删根，避免外键阻碍
                    var children = residualTypes.Where(item => item.ParentId.HasValue).ToList();
                    var roots = residualTypes.Where(item => !item.ParentId.HasValue).ToList();
                    if (children.Count > 0)
                    {
                        cleanupContext.GoodsTypes.RemoveRange(children);
                        await cleanupContext.SaveChangesAsync();
                    }

                    if (roots.Count > 0)
                    {
                        cleanupContext.GoodsTypes.RemoveRange(roots);
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
                    seedGoodsTypeReadButtonId,
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
                button.Id == seedGoodsTypeReadButtonId
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
            Assert.False(await residualContext.GoodsTypes.AnyAsync(item =>
                item.Code == rootTypeCode
                || item.Code == childTypeCode
                || item.Code == batchTypeCode
                || item.Code == expandedTypeCode
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            // 既有 Admin 角色与受管商品分类主数据必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.GoodsTypes.AnyAsync(item => item.Id == managedGoodsTypeId));
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
