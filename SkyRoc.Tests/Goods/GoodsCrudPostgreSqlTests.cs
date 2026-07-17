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
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Goods;

/// <summary>
///     T4 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证商品档案 CRUD、单位换算、上下架/启停与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class GoodsCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/查询/更新/删除/批量删除商品并维护单位换算与上下架；最小权限仅读；未认证与无权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task Goods_CrudUnitsSaleStatusAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedGoodsReadButtonId = Guid.NewGuid();
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
        var targetGoodsCode = $"{batch.Id}G";
        var targetGoodsName = $"{batch.Id}-联调青椒";
        var batchGoodsCode = $"{batch.Id}B";
        var batchGoodsName = $"{batch.Id}-批量删除青椒";
        var expandedGoodsCode = $"{batch.Id}E";
        var expandedGoodsName = $"{batch.Id}-扩权青椒";
        var deniedGoodsCode = $"{batch.Id}D";
        var deniedGoodsName = $"{batch.Id}-拒绝青椒";
        var baseUnitCode = $"{batch.Id}U1";
        var packageUnitCode = $"{batch.Id}U2";
        var baseUnitName = $"{batch.Id}斤";
        var packageUnitName = $"{batch.Id}箱";
        var password = "SkyRocGoodsPerm!2026";
        var userAgent = $"SkyRoc-T4-Goods/{batch.Id}";
        var createName = "T4-GoodsCrud";

        var goodsReadPermission = PermissionCodes.Business.Goods.Read;
        var goodsCreatePermission = PermissionCodes.Business.Goods.Create;
        var goodsUpdatePermission = PermissionCodes.Business.Goods.Update;
        var goodsDeletePermission = PermissionCodes.Business.Goods.Delete;

        Guid adminRoleId;
        Guid managedGoodsTypeId;
        Guid managedSupplierId;
        Guid managedSecondarySupplierId;
        Guid managedWareId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            // 复用 T2 受管基础资料，不修改非受管主数据
            var goodsTypeCode = DemoDataStableKeyCatalog.Create("GOODS-TYPE", 1);
            var supplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);
            var secondarySupplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 2);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);

            var goodsType = await seedContext.GoodsTypes.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsTypeCode);
            Assert.NotNull(goodsType);
            managedGoodsTypeId = goodsType.Id;

            var supplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == supplierCode);
            Assert.NotNull(supplier);
            managedSupplierId = supplier.Id;

            var secondarySupplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == secondarySupplierCode);
            Assert.NotNull(secondarySupplier);
            managedSecondarySupplierId = secondarySupplier.Id;

            var ware = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == wareCode);
            Assert.NotNull(ware);
            managedWareId = ware.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T4 商品权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T4商品操作员",
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
                    NickName = "T4商品只读用户",
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

            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = seedMenuId,
                    Name = seedMenuName,
                    Path = $"/{batch.Id}s",
                    Title = "T4商品只读菜单",
                    Component = "page.t4.goods.seed",
                    MenuType = MenuType.Menu,
                    Order = 9601,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T4商品写权限菜单",
                    Component = "page.t4.goods.write",
                    MenuType = MenuType.Menu,
                    Order = 9602,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedGoodsReadButtonId,
                    Code = goodsReadPermission,
                    Desc = "T4 商品读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = goodsCreatePermission,
                    Desc = "T4 商品创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = goodsUpdatePermission,
                    Desc = "T4 商品更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = goodsDeletePermission,
                    Desc = "T4 商品删除权限按钮",
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

        Guid? targetGoodsId = null;
        Guid? batchGoodsId = null;
        Guid? expandedGoodsId = null;
        Guid? baseUnitId = null;
        Guid? packageUnitId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问商品接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/goods"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/goods",
                       new CreateGoodsDto
                       {
                           Code = $"{batch.Id}X",
                           Name = $"{batch.Id}-未认证商品",
                           GoodsTypeId = managedGoodsTypeId,
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

            // 操作员创建目标商品（含默认供应商/仓库与多供应商关系）
            GoodsDto targetGoods;
            using (var createTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/goods",
                       new CreateGoodsDto
                       {
                           Code = targetGoodsCode,
                           Name = targetGoodsName,
                           GoodsTypeId = managedGoodsTypeId,
                           DefaultSupplierId = managedSupplierId,
                           DefaultWareId = managedWareId,
                           Spec = "500g/份",
                           Brand = "华东鲜蔬",
                           Origin = "山东寿光",
                           Description = "T4自动测试商品档案：青椒，用于单位换算与上下架校验",
                           TaxRate = 0.09m,
                           IsOnSale = true,
                           SupplierIds = [managedSupplierId, managedSecondarySupplierId],
                           Remark = "T4商品CRUD切片目标商品",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createTargetResponse.StatusCode);
                targetGoods = await ReadApiDataAsync<GoodsDto>(createTargetResponse);
                Assert.Equal(targetGoodsCode, targetGoods.Code);
                Assert.Equal(targetGoodsName, targetGoods.Name);
                Assert.Equal(managedGoodsTypeId, targetGoods.GoodsTypeId);
                Assert.Equal(managedSupplierId, targetGoods.DefaultSupplierId);
                Assert.Equal(managedWareId, targetGoods.DefaultWareId);
                Assert.True(targetGoods.IsOnSale);
                Assert.Equal(Status.Enable, targetGoods.Status);
                targetGoodsId = targetGoods.Id;
                registry.Register<GoodsEntity>(targetGoods.Id, nameof(GoodsEntity.Code), targetGoodsCode);
            }

            // 批量删除目标商品
            GoodsDto batchGoods;
            using (var createBatchTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/goods",
                       new CreateGoodsDto
                       {
                           Code = batchGoodsCode,
                           Name = batchGoodsName,
                           GoodsTypeId = managedGoodsTypeId,
                           DefaultSupplierId = managedSupplierId,
                           DefaultWareId = managedWareId,
                           Spec = "1kg/份",
                           Brand = "华东鲜蔬",
                           Origin = "山东寿光",
                           Description = "T4自动测试批量删除商品",
                           TaxRate = 0.09m,
                           IsOnSale = true,
                           SupplierIds = [managedSupplierId],
                           Remark = "T4商品CRUD切片批量删除商品",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBatchTargetResponse.StatusCode);
                batchGoods = await ReadApiDataAsync<GoodsDto>(createBatchTargetResponse);
                Assert.Equal(batchGoodsCode, batchGoods.Code);
                batchGoodsId = batchGoods.Id;
                registry.Register<GoodsEntity>(batchGoods.Id, nameof(GoodsEntity.Code), batchGoodsCode);
            }

            // 创建基础单位与箱单位（换算率 10）
            GoodsUnitDto baseUnit;
            using (var createBaseUnitResponse = await adminClient.PostAsJsonAsync(
                       "/api/goods-units",
                       new CreateGoodsUnitDto
                       {
                           GoodsId = targetGoods.Id,
                           Name = baseUnitName,
                           Code = baseUnitCode,
                           ConversionRate = 1m,
                           IsBaseUnit = true,
                           Sort = 1,
                           Remark = "T4基础单位斤",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBaseUnitResponse.StatusCode);
                baseUnit = await ReadApiDataAsync<GoodsUnitDto>(createBaseUnitResponse);
                Assert.True(baseUnit.IsBaseUnit);
                Assert.Equal(1m, baseUnit.ConversionRate);
                baseUnitId = baseUnit.Id;
                registry.Register<GoodsUnit>(baseUnit.Id, nameof(GoodsUnit.Code), baseUnitCode);
            }

            GoodsUnitDto packageUnit;
            using (var createPackageUnitResponse = await adminClient.PostAsJsonAsync(
                       "/api/goods-units",
                       new CreateGoodsUnitDto
                       {
                           GoodsId = targetGoods.Id,
                           Name = packageUnitName,
                           Code = packageUnitCode,
                           ConversionRate = 10m,
                           IsBaseUnit = false,
                           Sort = 2,
                           Remark = "T4包装单位箱，1箱=10斤",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createPackageUnitResponse.StatusCode);
                packageUnit = await ReadApiDataAsync<GoodsUnitDto>(createPackageUnitResponse);
                Assert.False(packageUnit.IsBaseUnit);
                Assert.Equal(10m, packageUnit.ConversionRate);
                packageUnitId = packageUnit.Id;
                registry.Register<GoodsUnit>(packageUnit.Id, nameof(GoodsUnit.Code), packageUnitCode);
            }

            // 商品详情应回写基础单位，并返回单位列表与供应商关系
            using (var detailAfterUnits = await adminClient.GetAsync($"/api/goods/{targetGoods.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterUnits.StatusCode);
                var detail = await ReadApiDataAsync<GoodsDto>(detailAfterUnits);
                Assert.Equal(baseUnit.Id, detail.BaseUnitId);
                Assert.NotNull(detail.SupplierIds);
                Assert.Contains(managedSupplierId, detail.SupplierIds!);
                Assert.Contains(managedSecondarySupplierId, detail.SupplierIds!);
            }

            using (var unitsByGoods = await adminClient.GetAsync($"/api/goods-units/by-goods/{targetGoods.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, unitsByGoods.StatusCode);
                var units = await ReadApiDataAsync<List<GoodsUnitDto>>(unitsByGoods);
                Assert.Equal(2, units.Count);
                Assert.Contains(units, unit => unit.Id == baseUnit.Id && unit.IsBaseUnit && unit.ConversionRate == 1m);
                Assert.Contains(units, unit => unit.Id == packageUnit.Id && !unit.IsBaseUnit && unit.ConversionRate == 10m);
            }

            await using (var relationContext = fixture.CreateDbContext())
            {
                var relations = await relationContext.GoodsSupplierRelations.AsNoTracking()
                    .Where(relation => relation.GoodsId == targetGoods.Id)
                    .ToListAsync();
                Assert.Equal(2, relations.Count);
                Assert.Contains(relations, relation => relation.SupplierId == managedSupplierId && relation.IsDefault);
                Assert.Contains(relations, relation => relation.SupplierId == managedSecondarySupplierId && !relation.IsDefault);

                var goodsEntity = await relationContext.Goods.AsNoTracking()
                    .SingleAsync(item => item.Id == targetGoods.Id);
                Assert.Equal(baseUnit.Id, goodsEntity.BaseUnitId);
            }

            // 分页/全量/详情
            using (var listResponse = await adminClient.GetAsync(
                       $"/api/goods/list?current=1&size=20&code={Uri.EscapeDataString(targetGoodsCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<GoodsDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == targetGoods.Id || item.Code == targetGoodsCode);
            }

            using (var allResponse = await adminClient.GetAsync("/api/goods"))
            {
                Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            }

            // 更新商品业务字段
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/goods",
                       new UpdateGoodsDto
                       {
                           Id = targetGoods.Id,
                           Code = targetGoodsCode,
                           Name = targetGoodsName,
                           GoodsTypeId = managedGoodsTypeId,
                           DefaultSupplierId = managedSupplierId,
                           DefaultWareId = managedWareId,
                           Spec = "600g/份",
                           Brand = "华东鲜蔬精选",
                           Origin = "山东寿光",
                           Description = "T4自动测试商品档案：青椒-已更新规格",
                           TaxRate = 0.13m,
                           IsOnSale = true,
                           SupplierIds = [managedSupplierId, managedSecondarySupplierId],
                           Remark = "T4商品CRUD切片目标商品-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var updated = await ReadApiDataAsync<GoodsDto>(updateResponse);
                Assert.Equal("600g/份", updated.Spec);
                Assert.Equal(0.13m, updated.TaxRate);
                Assert.Equal("T4商品CRUD切片目标商品-已更新", updated.Remark);
            }

            // 下架与停用
            using (var saleOffResponse = await adminClient.PatchAsync(
                       $"/api/goods/{targetGoods.Id}/sale-status?isOnSale=false",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, saleOffResponse.StatusCode);
                var saleOff = await ReadApiDataAsync<GoodsDto>(saleOffResponse);
                Assert.False(saleOff.IsOnSale);
            }

            using (var disableResponse = await adminClient.PatchAsync(
                       $"/api/goods/{targetGoods.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
                var disabled = await ReadApiDataAsync<GoodsDto>(disableResponse);
                Assert.Equal(Status.Disable, disabled.Status);
            }

            using (var saleOnResponse = await adminClient.PatchAsync(
                       $"/api/goods/{targetGoods.Id}/sale-status?isOnSale=true",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, saleOnResponse.StatusCode);
                var saleOn = await ReadApiDataAsync<GoodsDto>(saleOnResponse);
                Assert.True(saleOn.IsOnSale);
            }

            using (var enableResponse = await adminClient.PatchAsync(
                       $"/api/goods/{targetGoods.Id}/status?status={(int)Status.Enable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
                var enabled = await ReadApiDataAsync<GoodsDto>(enableResponse);
                Assert.Equal(Status.Enable, enabled.Status);
            }

            // 操作员批量删除
            using (var batchDeleteResponse = await adminClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/goods/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { batchGoods.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, batchDeleteResponse.StatusCode);
            }

            await using (var afterBatchDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterBatchDelete.Goods.AnyAsync(item =>
                    item.Id == batchGoods.Id || item.Code == batchGoodsCode));
                batchGoodsId = null;
            }

            // 最小权限用户登录：仅商品读
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

            using (var allowedAll = await limitedClient.GetAsync("/api/goods"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/goods/{targetGoods.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var allowedUnits = await limitedClient.GetAsync($"/api/goods-units/by-goods/{targetGoods.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedUnits.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/goods",
                       new CreateGoodsDto
                       {
                           Code = deniedGoodsCode,
                           Name = deniedGoodsName,
                           GoodsTypeId = managedGoodsTypeId,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/goods",
                       new UpdateGoodsDto
                       {
                           Id = targetGoods.Id,
                           Code = targetGoodsCode,
                           Name = targetGoodsName,
                           GoodsTypeId = managedGoodsTypeId,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdate, ResponseCode.Forbidden);
            }

            using (var deniedSale = await limitedClient.PatchAsync(
                       $"/api/goods/{targetGoods.Id}/sale-status?isOnSale=false",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedSale, ResponseCode.Forbidden);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/goods/{targetGoods.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDelete, ResponseCode.Forbidden);
            }

            using (var deniedUnitCreate = await limitedClient.PostAsJsonAsync(
                       "/api/goods-units",
                       new CreateGoodsUnitDto
                       {
                           GoodsId = targetGoods.Id,
                           Name = $"{batch.Id}袋",
                           Code = $"{batch.Id}U3",
                           ConversionRate = 2m,
                           IsBaseUnit = false,
                           Sort = 3,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUnitCreate, ResponseCode.Forbidden);
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

            GoodsDto expandedGoods;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/goods",
                       new CreateGoodsDto
                       {
                           Code = expandedGoodsCode,
                           Name = expandedGoodsName,
                           GoodsTypeId = managedGoodsTypeId,
                           DefaultSupplierId = managedSupplierId,
                           DefaultWareId = managedWareId,
                           Spec = "300g/份",
                           Brand = "华东鲜蔬",
                           Origin = "山东寿光",
                           Description = "T4扩权后创建的商品",
                           TaxRate = 0.09m,
                           IsOnSale = true,
                           SupplierIds = [managedSupplierId],
                           Remark = "T4扩权商品",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedGoods = await ReadApiDataAsync<GoodsDto>(createExpanded);
                Assert.Equal(expandedGoodsCode, expandedGoods.Code);
                expandedGoodsId = expandedGoods.Id;
                registry.Register<GoodsEntity>(expandedGoods.Id, nameof(GoodsEntity.Code), expandedGoodsCode);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/goods",
                       new UpdateGoodsDto
                       {
                           Id = expandedGoods.Id,
                           Code = expandedGoodsCode,
                           Name = expandedGoodsName,
                           GoodsTypeId = managedGoodsTypeId,
                           DefaultSupplierId = managedSupplierId,
                           DefaultWareId = managedWareId,
                           Spec = "350g/份",
                           Brand = "华东鲜蔬",
                           Origin = "山东寿光",
                           Description = "T4扩权后创建的商品-已更新",
                           TaxRate = 0.09m,
                           IsOnSale = true,
                           SupplierIds = [managedSupplierId],
                           Remark = "T4扩权商品-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/goods/{expandedGoods.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.Goods.AnyAsync(item =>
                    item.Id == expandedGoods.Id || item.Code == expandedGoodsCode));
                expandedGoodsId = null;
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
                       "/api/goods",
                       new CreateGoodsDto
                       {
                           Code = $"{batch.Id}F",
                           Name = $"{batch.Id}-缩权拒绝商品",
                           GoodsTypeId = managedGoodsTypeId,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateAfterShrink, ResponseCode.Forbidden);
            }

            // 先删包装单位，再删基础单位（商品 BaseUnitId 置空），最后删商品
            using (var deletePackageUnit = await adminClient.DeleteAsync($"/api/goods-units/{packageUnit.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deletePackageUnit.StatusCode);
            }

            packageUnitId = null;

            using (var deleteBaseUnit = await adminClient.DeleteAsync($"/api/goods-units/{baseUnit.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteBaseUnit.StatusCode);
            }

            baseUnitId = null;

            using (var deleteTarget = await adminClient.DeleteAsync($"/api/goods/{targetGoods.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteTarget.StatusCode);
            }

            await using (var afterDeleteTarget = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteTarget.Goods.AnyAsync(item =>
                    item.Id == targetGoods.Id || item.Code == targetGoodsCode));
                Assert.False(await afterDeleteTarget.GoodsUnits.AnyAsync(unit =>
                    unit.Code == baseUnitCode || unit.Code == packageUnitCode));
                Assert.False(await afterDeleteTarget.GoodsSupplierRelations.AnyAsync(relation =>
                    relation.GoodsId == targetGoods.Id));
                targetGoodsId = null;
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

            // 先清本轮批次登记实体；UserRole 随用户级联，RoleMenu 随角色/菜单级联；单位与供货关系随商品级联
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

                // 兜底清理可能残留的商品单位与供货关系
                var residualGoodsCodes = new List<string>
                {
                    targetGoodsCode,
                    batchGoodsCode,
                    expandedGoodsCode,
                    deniedGoodsCode,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualGoodsIds = new List<Guid>();
                if (targetGoodsId.HasValue)
                    residualGoodsIds.Add(targetGoodsId.Value);
                if (batchGoodsId.HasValue)
                    residualGoodsIds.Add(batchGoodsId.Value);
                if (expandedGoodsId.HasValue)
                    residualGoodsIds.Add(expandedGoodsId.Value);

                var residualGoods = await cleanupContext.Goods
                    .Where(item => residualGoodsIds.Contains(item.Id)
                                   || residualGoodsCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualGoods.Count > 0)
                {
                    residualGoodsIds = residualGoods.Select(item => item.Id).Distinct().ToList();
                    var residualUnits = await cleanupContext.GoodsUnits
                        .Where(unit => residualGoodsIds.Contains(unit.GoodsId)
                                       || unit.Code == baseUnitCode
                                       || unit.Code == packageUnitCode
                                       || (unit.Code != null && unit.Code.StartsWith(batch.Id)))
                        .ToListAsync();
                    if (residualUnits.Count > 0)
                    {
                        // 先清空商品基础单位引用，避免删除单位时外键冲突
                        foreach (var goods in residualGoods.Where(item => item.BaseUnitId.HasValue))
                            goods.BaseUnitId = null;
                        await cleanupContext.SaveChangesAsync();

                        cleanupContext.GoodsUnits.RemoveRange(residualUnits);
                        await cleanupContext.SaveChangesAsync();
                    }

                    var residualRelations = await cleanupContext.GoodsSupplierRelations
                        .Where(relation => residualGoodsIds.Contains(relation.GoodsId))
                        .ToListAsync();
                    if (residualRelations.Count > 0)
                    {
                        cleanupContext.GoodsSupplierRelations.RemoveRange(residualRelations);
                        await cleanupContext.SaveChangesAsync();
                    }

                    cleanupContext.Goods.RemoveRange(residualGoods);
                    await cleanupContext.SaveChangesAsync();
                }
                else if (baseUnitId.HasValue || packageUnitId.HasValue)
                {
                    var residualUnits = await cleanupContext.GoodsUnits
                        .Where(unit => (baseUnitId.HasValue && unit.Id == baseUnitId.Value)
                                       || (packageUnitId.HasValue && unit.Id == packageUnitId.Value)
                                       || unit.Code == baseUnitCode
                                       || unit.Code == packageUnitCode
                                       || (unit.Code != null && unit.Code.StartsWith(batch.Id)))
                        .ToListAsync();
                    if (residualUnits.Count > 0)
                    {
                        cleanupContext.GoodsUnits.RemoveRange(residualUnits);
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
                    seedGoodsReadButtonId,
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
                button.Id == seedGoodsReadButtonId
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
            Assert.False(await residualContext.Goods.AnyAsync(item =>
                item.Code == targetGoodsCode
                || item.Code == batchGoodsCode
                || item.Code == expandedGoodsCode
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.GoodsUnits.AnyAsync(unit =>
                unit.Code == baseUnitCode
                || unit.Code == packageUnitCode
                || (unit.Code != null && unit.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            // 既有 Admin 角色与受管主数据必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.GoodsTypes.AnyAsync(item => item.Id == managedGoodsTypeId));
            Assert.True(await residualContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await residualContext.Suppliers.AnyAsync(item => item.Id == managedSecondarySupplierId));
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
