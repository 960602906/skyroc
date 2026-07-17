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
///     T4 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证采购规则 CRUD、适用商品/客户关系、启停与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class PurchaseRuleCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/查询/更新/删除/批量删除采购规则并维护适用商品与客户关系、切换启停；
    ///     最小权限仅读；未认证与无权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task PurchaseRule_CrudRelationsStatusAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedPurchaseReadButtonId = Guid.NewGuid();
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
        var targetRuleCode = $"{batch.Id}P";
        var targetRuleName = $"{batch.Id}-联调蔬菜采购规则";
        var batchRuleCode = $"{batch.Id}B";
        var batchRuleName = $"{batch.Id}-批量删除采购规则";
        var expandedRuleCode = $"{batch.Id}E";
        var expandedRuleName = $"{batch.Id}-扩权采购规则";
        var deniedRuleCode = $"{batch.Id}D";
        var deniedRuleName = $"{batch.Id}-拒绝采购规则";
        var password = "SkyRocPurchaseRulePerm!2026";
        var userAgent = $"SkyRoc-T4-PurchaseRule/{batch.Id}";
        var createName = "T4-PurchaseRuleCrud";

        var purchaseReadPermission = PermissionCodes.Business.Purchases.Read;
        var purchaseCreatePermission = PermissionCodes.Business.Purchases.Create;
        var purchaseUpdatePermission = PermissionCodes.Business.Purchases.Update;
        var purchaseDeletePermission = PermissionCodes.Business.Purchases.Delete;

        Guid adminRoleId;
        Guid managedRuleId;
        Guid managedSupplierId;
        string managedSupplierName = null!;
        Guid managedSecondarySupplierId;
        string managedSecondarySupplierName = null!;
        Guid managedPurchaserId;
        string managedPurchaserName = null!;
        Guid managedSecondaryPurchaserId;
        string managedSecondaryPurchaserName = null!;
        Guid managedWareId;
        string managedWareName = null!;
        Guid managedGoodsTypeId;
        string managedGoodsTypeName = null!;
        Guid managedGoodsId;
        Guid managedSecondaryGoodsId;
        Guid managedCustomerId;
        Guid managedSecondaryCustomerId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            // 复用 T2 受管采购规则与关联主数据，仅用于断言不被本轮改动/删除
            var managedRuleCode = DemoDataStableKeyCatalog.Create("PURCHASE-RULE", 1);
            var managedRule = await seedContext.PurchaseRules.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedRuleCode);
            Assert.NotNull(managedRule);
            managedRuleId = managedRule.Id;

            var managedSupplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);
            var managedSecondarySupplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 2);
            var managedPurchaserCode = DemoDataStableKeyCatalog.Create("PURCHASER", 1);
            var managedSecondaryPurchaserCode = DemoDataStableKeyCatalog.Create("PURCHASER", 2);
            var managedWareCode = DemoDataStableKeyCatalog.Create("WARE", 1);
            var managedGoodsTypeCode = DemoDataStableKeyCatalog.Create("GOODS-TYPE", 1);
            var managedGoodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var managedSecondaryGoodsCode = DemoDataStableKeyCatalog.Create("GOODS", 2);
            var managedCustomerCode = DemoDataStableKeyCatalog.Create("CUSTOMER", 1);
            var managedSecondaryCustomerCode = DemoDataStableKeyCatalog.Create("CUSTOMER", 2);

            var managedSupplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedSupplierCode);
            Assert.NotNull(managedSupplier);
            managedSupplierId = managedSupplier.Id;
            managedSupplierName = managedSupplier.Name;

            var managedSecondarySupplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedSecondarySupplierCode);
            Assert.NotNull(managedSecondarySupplier);
            managedSecondarySupplierId = managedSecondarySupplier.Id;
            managedSecondarySupplierName = managedSecondarySupplier.Name;

            var managedPurchaser = await seedContext.Purchasers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedPurchaserCode);
            Assert.NotNull(managedPurchaser);
            managedPurchaserId = managedPurchaser.Id;
            managedPurchaserName = managedPurchaser.Name;

            var managedSecondaryPurchaser = await seedContext.Purchasers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedSecondaryPurchaserCode);
            Assert.NotNull(managedSecondaryPurchaser);
            managedSecondaryPurchaserId = managedSecondaryPurchaser.Id;
            managedSecondaryPurchaserName = managedSecondaryPurchaser.Name;

            var managedWare = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedWareCode);
            Assert.NotNull(managedWare);
            managedWareId = managedWare.Id;
            managedWareName = managedWare.Name;

            var managedGoodsType = await seedContext.GoodsTypes.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedGoodsTypeCode);
            Assert.NotNull(managedGoodsType);
            managedGoodsTypeId = managedGoodsType.Id;
            managedGoodsTypeName = managedGoodsType.Name;

            var managedGoods = await seedContext.Goods.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedGoodsCode);
            Assert.NotNull(managedGoods);
            managedGoodsId = managedGoods.Id;

            var managedSecondaryGoods = await seedContext.Goods.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedSecondaryGoodsCode);
            Assert.NotNull(managedSecondaryGoods);
            managedSecondaryGoodsId = managedSecondaryGoods.Id;

            var managedCustomer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedCustomerCode);
            Assert.NotNull(managedCustomer);
            managedCustomerId = managedCustomer.Id;

            var managedSecondaryCustomer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedSecondaryCustomerCode);
            Assert.NotNull(managedSecondaryCustomer);
            managedSecondaryCustomerId = managedSecondaryCustomer.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T4 采购规则权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T4采购规则操作员",
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
                    NickName = "T4采购规则只读用户",
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
                    Title = "T4采购规则只读菜单",
                    Component = "page.t4.purchase-rule.seed",
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
                    Title = "T4采购规则写权限菜单",
                    Component = "page.t4.purchase-rule.write",
                    MenuType = MenuType.Menu,
                    Order = 9632,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedPurchaseReadButtonId,
                    Code = purchaseReadPermission,
                    Desc = "T4 采购/采购规则读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = purchaseCreatePermission,
                    Desc = "T4 采购/采购规则创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = purchaseUpdatePermission,
                    Desc = "T4 采购/采购规则更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = purchaseDeletePermission,
                    Desc = "T4 采购/采购规则删除权限按钮",
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

        Guid? targetRuleId = null;
        Guid? batchRuleId = null;
        Guid? expandedRuleId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问采购规则接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/purchase-rules"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/purchase-rules",
                       new CreatePurchaseRuleDto
                       {
                           Code = $"{batch.Id}X",
                           Name = $"{batch.Id}-未认证采购规则",
                           PurchasePattern = 1,
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

            // 操作员创建目标采购规则（含供应商/采购员/仓库/分类与多商品、多客户关系）
            PurchaseRuleDto targetRule;
            using (var createTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/purchase-rules",
                       new CreatePurchaseRuleDto
                       {
                           Code = targetRuleCode,
                           Name = targetRuleName,
                           SupplierId = managedSupplierId,
                           PurchaserId = managedPurchaserId,
                           WareId = managedWareId,
                           GoodsTypeId = managedGoodsTypeId,
                           PurchasePattern = 1,
                           GoodsIds = [managedGoodsId, managedSecondaryGoodsId],
                           CustomerIds = [managedCustomerId, managedSecondaryCustomerId],
                           Remark = "T4采购规则CRUD切片目标规则",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createTargetResponse.StatusCode);
                targetRule = await ReadApiDataAsync<PurchaseRuleDto>(createTargetResponse);
                Assert.Equal(targetRuleCode, targetRule.Code);
                Assert.Equal(targetRuleName, targetRule.Name);
                Assert.Equal(managedSupplierId, targetRule.SupplierId);
                Assert.Equal(managedPurchaserId, targetRule.PurchaserId);
                Assert.Equal(managedWareId, targetRule.WareId);
                Assert.Equal(managedGoodsTypeId, targetRule.GoodsTypeId);
                Assert.Equal(1, targetRule.PurchasePattern);
                Assert.Equal(Status.Enable, targetRule.Status);
                Assert.NotNull(targetRule.GoodsIds);
                Assert.Contains(managedGoodsId, targetRule.GoodsIds!);
                Assert.Contains(managedSecondaryGoodsId, targetRule.GoodsIds!);
                Assert.NotNull(targetRule.CustomerIds);
                Assert.Contains(managedCustomerId, targetRule.CustomerIds!);
                Assert.Contains(managedSecondaryCustomerId, targetRule.CustomerIds!);
                targetRuleId = targetRule.Id;
                registry.Register<PurchaseRule>(targetRule.Id, nameof(PurchaseRule.Code), targetRuleCode);
            }

            // 批量删除目标规则
            PurchaseRuleDto batchRule;
            using (var createBatchTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/purchase-rules",
                       new CreatePurchaseRuleDto
                       {
                           Code = batchRuleCode,
                           Name = batchRuleName,
                           SupplierId = managedSupplierId,
                           PurchaserId = managedPurchaserId,
                           WareId = managedWareId,
                           GoodsTypeId = managedGoodsTypeId,
                           PurchasePattern = 1,
                           GoodsIds = [managedGoodsId],
                           CustomerIds = [managedCustomerId],
                           Remark = "T4采购规则CRUD切片批量删除规则",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBatchTargetResponse.StatusCode);
                batchRule = await ReadApiDataAsync<PurchaseRuleDto>(createBatchTargetResponse);
                Assert.Equal(batchRuleCode, batchRule.Code);
                batchRuleId = batchRule.Id;
                registry.Register<PurchaseRule>(batchRule.Id, nameof(PurchaseRule.Code), batchRuleCode);
            }

            // 详情应返回关联名称与商品/客户关系
            using (var detailAfterCreate = await adminClient.GetAsync($"/api/purchase-rules/{targetRule.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterCreate.StatusCode);
                var detail = await ReadApiDataAsync<PurchaseRuleDto>(detailAfterCreate);
                Assert.Equal(managedSupplierId, detail.SupplierId);
                Assert.Equal(managedSupplierName, detail.SupplierName);
                Assert.Equal(managedPurchaserId, detail.PurchaserId);
                Assert.Equal(managedPurchaserName, detail.PurchaserName);
                Assert.Equal(managedWareId, detail.WareId);
                Assert.Equal(managedWareName, detail.WareName);
                Assert.Equal(managedGoodsTypeId, detail.GoodsTypeId);
                Assert.Equal(managedGoodsTypeName, detail.GoodsTypeName);
                Assert.NotNull(detail.GoodsIds);
                Assert.Equal(2, detail.GoodsIds!.Count);
                Assert.Contains(managedGoodsId, detail.GoodsIds);
                Assert.Contains(managedSecondaryGoodsId, detail.GoodsIds);
                Assert.NotNull(detail.CustomerIds);
                Assert.Equal(2, detail.CustomerIds!.Count);
                Assert.Contains(managedCustomerId, detail.CustomerIds);
                Assert.Contains(managedSecondaryCustomerId, detail.CustomerIds);
            }

            await using (var relationContext = fixture.CreateDbContext())
            {
                var goodsRelations = await relationContext.PurchaseRuleGoods.AsNoTracking()
                    .Where(relation => relation.PurchaseRuleId == targetRule.Id)
                    .ToListAsync();
                Assert.Equal(2, goodsRelations.Count);
                Assert.Contains(goodsRelations, relation => relation.GoodsId == managedGoodsId);
                Assert.Contains(goodsRelations, relation => relation.GoodsId == managedSecondaryGoodsId);

                var customerRelations = await relationContext.PurchaseRuleCustomers.AsNoTracking()
                    .Where(relation => relation.PurchaseRuleId == targetRule.Id)
                    .ToListAsync();
                Assert.Equal(2, customerRelations.Count);
                Assert.Contains(customerRelations, relation => relation.CustomerId == managedCustomerId);
                Assert.Contains(customerRelations, relation => relation.CustomerId == managedSecondaryCustomerId);

                var ruleEntity = await relationContext.PurchaseRules.AsNoTracking()
                    .SingleAsync(item => item.Id == targetRule.Id);
                Assert.Equal(managedSupplierId, ruleEntity.SupplierId);
                Assert.Equal(managedPurchaserId, ruleEntity.PurchaserId);
                Assert.Equal(managedWareId, ruleEntity.WareId);
                Assert.Equal(managedGoodsTypeId, ruleEntity.GoodsTypeId);
                Assert.Equal(1, ruleEntity.PurchasePattern);
                Assert.Equal("T4采购规则CRUD切片目标规则", ruleEntity.Remark);
            }

            // 分页/全量/详情
            using (var listResponse = await adminClient.GetAsync(
                       $"/api/purchase-rules/list?current=1&size=20&code={Uri.EscapeDataString(targetRuleCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<PurchaseRuleDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == targetRule.Id || item.Code == targetRuleCode);
            }

            using (var allResponse = await adminClient.GetAsync("/api/purchase-rules"))
            {
                Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            }

            // 更新业务字段与关系（换绑供应商/采购员，采购模式改为市场自采，缩为单商品/单客户）
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/purchase-rules",
                       new UpdatePurchaseRuleDto
                       {
                           Id = targetRule.Id,
                           Code = targetRuleCode,
                           Name = targetRuleName,
                           SupplierId = managedSecondarySupplierId,
                           PurchaserId = managedSecondaryPurchaserId,
                           WareId = managedWareId,
                           GoodsTypeId = managedGoodsTypeId,
                           PurchasePattern = 2,
                           GoodsIds = [managedGoodsId],
                           CustomerIds = [managedCustomerId],
                           Remark = "T4采购规则CRUD切片目标规则-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var updated = await ReadApiDataAsync<PurchaseRuleDto>(updateResponse);
                Assert.Equal(managedSecondarySupplierId, updated.SupplierId);
                Assert.Equal(managedSecondaryPurchaserId, updated.PurchaserId);
                Assert.Equal(2, updated.PurchasePattern);
                Assert.Equal("T4采购规则CRUD切片目标规则-已更新", updated.Remark);
                Assert.NotNull(updated.GoodsIds);
                Assert.Single(updated.GoodsIds!);
                Assert.Contains(managedGoodsId, updated.GoodsIds!);
                Assert.NotNull(updated.CustomerIds);
                Assert.Single(updated.CustomerIds!);
                Assert.Contains(managedCustomerId, updated.CustomerIds!);
            }

            await using (var afterUpdate = fixture.CreateDbContext())
            {
                var goodsRelations = await afterUpdate.PurchaseRuleGoods.AsNoTracking()
                    .Where(relation => relation.PurchaseRuleId == targetRule.Id)
                    .ToListAsync();
                Assert.Single(goodsRelations);
                Assert.Equal(managedGoodsId, goodsRelations[0].GoodsId);

                var customerRelations = await afterUpdate.PurchaseRuleCustomers.AsNoTracking()
                    .Where(relation => relation.PurchaseRuleId == targetRule.Id)
                    .ToListAsync();
                Assert.Single(customerRelations);
                Assert.Equal(managedCustomerId, customerRelations[0].CustomerId);

                var ruleEntity = await afterUpdate.PurchaseRules.AsNoTracking()
                    .SingleAsync(item => item.Id == targetRule.Id);
                Assert.Equal(managedSecondarySupplierId, ruleEntity.SupplierId);
                Assert.Equal(managedSecondaryPurchaserId, ruleEntity.PurchaserId);
                Assert.Equal(2, ruleEntity.PurchasePattern);
            }

            using (var detailAfterUpdate = await adminClient.GetAsync($"/api/purchase-rules/{targetRule.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterUpdate.StatusCode);
                var detail = await ReadApiDataAsync<PurchaseRuleDto>(detailAfterUpdate);
                Assert.Equal(managedSecondarySupplierName, detail.SupplierName);
                Assert.Equal(managedSecondaryPurchaserName, detail.PurchaserName);
            }

            // 停用与启用
            using (var disableResponse = await adminClient.PatchAsync(
                       $"/api/purchase-rules/{targetRule.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
                var disabled = await ReadApiDataAsync<PurchaseRuleDto>(disableResponse);
                Assert.Equal(Status.Disable, disabled.Status);
            }

            using (var enableResponse = await adminClient.PatchAsync(
                       $"/api/purchase-rules/{targetRule.Id}/status?status={(int)Status.Enable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
                var enabled = await ReadApiDataAsync<PurchaseRuleDto>(enableResponse);
                Assert.Equal(Status.Enable, enabled.Status);
            }

            // 操作员批量删除
            using (var batchDeleteResponse = await adminClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/purchase-rules/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { batchRule.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, batchDeleteResponse.StatusCode);
            }

            await using (var afterBatchDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterBatchDelete.PurchaseRules.AnyAsync(item =>
                    item.Id == batchRule.Id || item.Code == batchRuleCode));
                Assert.False(await afterBatchDelete.PurchaseRuleGoods.AnyAsync(relation =>
                    relation.PurchaseRuleId == batchRule.Id));
                Assert.False(await afterBatchDelete.PurchaseRuleCustomers.AnyAsync(relation =>
                    relation.PurchaseRuleId == batchRule.Id));
                batchRuleId = null;
            }

            // 最小权限用户登录：仅采购读
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

            using (var allowedAll = await limitedClient.GetAsync("/api/purchase-rules"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/purchase-rules/{targetRule.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/purchase-rules",
                       new CreatePurchaseRuleDto
                       {
                           Code = deniedRuleCode,
                           Name = deniedRuleName,
                           PurchasePattern = 1,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/purchase-rules",
                       new UpdatePurchaseRuleDto
                       {
                           Id = targetRule.Id,
                           Code = targetRuleCode,
                           Name = targetRuleName,
                           PurchasePattern = 1,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdate, ResponseCode.Forbidden);
            }

            using (var deniedStatus = await limitedClient.PatchAsync(
                       $"/api/purchase-rules/{targetRule.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedStatus, ResponseCode.Forbidden);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/purchase-rules/{targetRule.Id}"))
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

            LoginResDto limitedWriteLogin;
            using (var writeLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, writeLoginResponse.StatusCode);
                limitedWriteLogin = await ReadApiDataAsync<LoginResDto>(writeLoginResponse);
            }

            using var limitedWriteClient = factory.CreateClient();
            limitedWriteClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedWriteLogin.Token);
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

            using (var routesAfterExpand = await limitedWriteClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesAfterExpand.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesAfterExpand);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.Contains($"/{batch.Id}w", paths);
            }

            PurchaseRuleDto expandedRule;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/purchase-rules",
                       new CreatePurchaseRuleDto
                       {
                           Code = expandedRuleCode,
                           Name = expandedRuleName,
                           SupplierId = managedSupplierId,
                           PurchaserId = managedPurchaserId,
                           WareId = managedWareId,
                           GoodsTypeId = managedGoodsTypeId,
                           PurchasePattern = 1,
                           GoodsIds = [managedGoodsId],
                           CustomerIds = [managedCustomerId],
                           Remark = "T4采购规则CRUD切片扩权规则",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedRule = await ReadApiDataAsync<PurchaseRuleDto>(createExpanded);
                Assert.Equal(expandedRuleCode, expandedRule.Code);
                expandedRuleId = expandedRule.Id;
                registry.Register<PurchaseRule>(expandedRule.Id, nameof(PurchaseRule.Code), expandedRuleCode);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/purchase-rules",
                       new UpdatePurchaseRuleDto
                       {
                           Id = expandedRule.Id,
                           Code = expandedRuleCode,
                           Name = expandedRuleName,
                           SupplierId = managedSupplierId,
                           PurchaserId = managedPurchaserId,
                           WareId = managedWareId,
                           GoodsTypeId = managedGoodsTypeId,
                           PurchasePattern = 1,
                           GoodsIds = [managedGoodsId],
                           CustomerIds = [managedCustomerId],
                           Remark = "T4采购规则CRUD切片扩权规则-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/purchase-rules/{expandedRule.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.PurchaseRules.AnyAsync(item =>
                    item.Id == expandedRule.Id || item.Code == expandedRuleCode));
                Assert.False(await afterExpandedDelete.PurchaseRuleGoods.AnyAsync(relation =>
                    relation.PurchaseRuleId == expandedRule.Id));
                Assert.False(await afterExpandedDelete.PurchaseRuleCustomers.AnyAsync(relation =>
                    relation.PurchaseRuleId == expandedRule.Id));
                expandedRuleId = null;
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
                       "/api/purchase-rules",
                       new CreatePurchaseRuleDto
                       {
                           Code = $"{batch.Id}F",
                           Name = $"{batch.Id}-缩权拒绝采购规则",
                           PurchasePattern = 1,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateAfterShrink, ResponseCode.Forbidden);
            }

            // 删除目标采购规则
            using (var deleteTarget = await adminClient.DeleteAsync($"/api/purchase-rules/{targetRule.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteTarget.StatusCode);
            }

            await using (var afterDeleteTarget = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteTarget.PurchaseRules.AnyAsync(item =>
                    item.Id == targetRule.Id || item.Code == targetRuleCode));
                Assert.False(await afterDeleteTarget.PurchaseRuleGoods.AnyAsync(relation =>
                    relation.PurchaseRuleId == targetRule.Id));
                Assert.False(await afterDeleteTarget.PurchaseRuleCustomers.AnyAsync(relation =>
                    relation.PurchaseRuleId == targetRule.Id));
                targetRuleId = null;
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

                // 兜底清理可能残留的采购规则及其关系
                var residualRuleCodes = new List<string>
                {
                    targetRuleCode,
                    batchRuleCode,
                    expandedRuleCode,
                    deniedRuleCode,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualRuleIds = new List<Guid>();
                if (targetRuleId.HasValue)
                    residualRuleIds.Add(targetRuleId.Value);
                if (batchRuleId.HasValue)
                    residualRuleIds.Add(batchRuleId.Value);
                if (expandedRuleId.HasValue)
                    residualRuleIds.Add(expandedRuleId.Value);

                var residualRules = await cleanupContext.PurchaseRules
                    .Where(item => residualRuleIds.Contains(item.Id)
                                   || residualRuleCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualRules.Count > 0)
                {
                    var residualIds = residualRules.Select(item => item.Id).ToList();
                    var residualGoods = await cleanupContext.PurchaseRuleGoods
                        .Where(relation => residualIds.Contains(relation.PurchaseRuleId))
                        .ToListAsync();
                    if (residualGoods.Count > 0)
                        cleanupContext.PurchaseRuleGoods.RemoveRange(residualGoods);

                    var residualCustomers = await cleanupContext.PurchaseRuleCustomers
                        .Where(relation => residualIds.Contains(relation.PurchaseRuleId))
                        .ToListAsync();
                    if (residualCustomers.Count > 0)
                        cleanupContext.PurchaseRuleCustomers.RemoveRange(residualCustomers);

                    cleanupContext.PurchaseRules.RemoveRange(residualRules);
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
                    seedPurchaseReadButtonId,
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
                button.Id == seedPurchaseReadButtonId
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
            Assert.False(await residualContext.PurchaseRules.AnyAsync(item =>
                item.Code == targetRuleCode
                || item.Code == batchRuleCode
                || item.Code == expandedRuleCode
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            // 既有 Admin 角色与受管采购规则及关联主数据必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.PurchaseRules.AnyAsync(item => item.Id == managedRuleId));
            Assert.True(await residualContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await residualContext.Purchasers.AnyAsync(item => item.Id == managedPurchaserId));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
            Assert.True(await residualContext.GoodsTypes.AnyAsync(item => item.Id == managedGoodsTypeId));
            Assert.True(await residualContext.Goods.AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
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
