using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Pricing;
using Application.DTOs.Role;
using Domain.Entities;
using Domain.Entities.Pricing;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Pricing;

/// <summary>
///     T4 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证报价审核权限矩阵、受管报价/协议价差与协议价优先语义。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class QuotationAuditAndProtocolPricePriorityPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建临时报价与协议价明细、审核/反审核；受管报价与协议价保持协议价更低；最小权限仅读；无审核权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task QuotationAudit_AndProtocolPricePriority_AuditPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedPricingReadButtonId = Guid.NewGuid();
        var writeMenuId = Guid.NewGuid();
        var writeCreateButtonId = Guid.NewGuid();
        var writeUpdateButtonId = Guid.NewGuid();
        var writeDeleteButtonId = Guid.NewGuid();
        var auditMenuId = Guid.NewGuid();
        var auditButtonId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username/role/code ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var writeMenuName = $"{batch.Id}W";
        var auditMenuName = $"{batch.Id}U";
        var targetQuotationCode = $"{batch.Id}Q";
        var targetQuotationName = $"{batch.Id}-联调报价方案";
        var targetProtocolCode = $"{batch.Id}P";
        var targetProtocolName = $"{batch.Id}-联调协议价";
        var expandedQuotationCode = $"{batch.Id}E";
        var expandedQuotationName = $"{batch.Id}-扩权报价";
        var deniedQuotationCode = $"{batch.Id}D";
        var deniedQuotationName = $"{batch.Id}-拒绝报价";
        var quotationGoodsRemark = $"{batch.Id}-联调报价明细";
        var protocolGoodsRemark = $"{batch.Id}-联调协议明细";
        var password = "SkyRocPricingAudit!2026";
        var userAgent = $"SkyRoc-T4-Pricing/{batch.Id}";
        var createName = "T4-QuotationAudit";

        var pricingReadPermission = PermissionCodes.Business.Pricing.Read;
        var pricingCreatePermission = PermissionCodes.Business.Pricing.Create;
        var pricingUpdatePermission = PermissionCodes.Business.Pricing.Update;
        var pricingDeletePermission = PermissionCodes.Business.Pricing.Delete;
        var pricingAuditPermission = PermissionCodes.Business.Pricing.Audit;

        var expectedManagedUnitPrice = 9.5m;
        var expectedManagedProtocolPrice = NumericPrecision.RoundMoney(8.25m);
        var tempUnitPrice = NumericPrecision.RoundMoney(15.5m);
        var tempProtocolPrice = NumericPrecision.RoundMoney(12.25m);

        Guid adminRoleId;
        Guid managedQuotationId;
        bool managedQuotationAudited;
        Guid managedUnauditedQuotationId;
        Guid managedProtocolId;
        Guid managedGoodsId;
        Guid managedGoodsUnitId;
        Guid managedQuotationGoodsId;
        Guid managedProtocolGoodsId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var managedQuotationCode = DemoDataStableKeyCatalog.Create("QUOTATION", 1);
            var managedUnauditedQuotationCode = DemoDataStableKeyCatalog.Create("QUOTATION", 5);
            var managedProtocolCode = DemoDataStableKeyCatalog.Create("CUSTOMER-PROTOCOL", 1);
            var managedGoodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var managedGoodsUnitCode = DemoDataStableKeyCatalog.Create("GOODS-UNIT", 1);

            var managedQuotation = await seedContext.Quotations.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedQuotationCode);
            Assert.NotNull(managedQuotation);
            managedQuotationId = managedQuotation.Id;
            managedQuotationAudited = managedQuotation.IsAudited;
            Assert.True(managedQuotationAudited);

            var managedUnauditedQuotation = await seedContext.Quotations.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedUnauditedQuotationCode);
            Assert.NotNull(managedUnauditedQuotation);
            managedUnauditedQuotationId = managedUnauditedQuotation.Id;
            Assert.False(managedUnauditedQuotation.IsAudited);

            var managedProtocol = await seedContext.CustomerProtocols.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedProtocolCode);
            Assert.NotNull(managedProtocol);
            managedProtocolId = managedProtocol.Id;

            var managedGoods = await seedContext.Goods.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedGoodsCode);
            Assert.NotNull(managedGoods);
            managedGoodsId = managedGoods.Id;

            var managedGoodsUnit = await seedContext.GoodsUnits.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == managedGoodsUnitCode);
            Assert.NotNull(managedGoodsUnit);
            managedGoodsUnitId = managedGoodsUnit.Id;

            var managedQuotationGoods = await seedContext.QuotationGoods.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.QuotationId == managedQuotationId
                    && item.GoodsId == managedGoodsId
                    && item.GoodsUnitId == managedGoodsUnitId);
            Assert.NotNull(managedQuotationGoods);
            managedQuotationGoodsId = managedQuotationGoods.Id;
            Assert.Equal(expectedManagedUnitPrice, managedQuotationGoods.UnitPrice);

            var managedProtocolGoods = await seedContext.CustomerProtocolGoods.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.CustomerProtocolId == managedProtocolId
                    && item.GoodsId == managedGoodsId
                    && item.GoodsUnitId == managedGoodsUnitId);
            Assert.NotNull(managedProtocolGoods);
            managedProtocolGoodsId = managedProtocolGoods.Id;
            Assert.Equal(expectedManagedProtocolPrice, managedProtocolGoods.ProtocolPrice);
            Assert.True(managedProtocolGoods.ProtocolPrice < managedQuotationGoods.UnitPrice);

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T4 报价审核权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T4报价审核操作员",
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
                    NickName = "T4报价只读用户",
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
                    Title = "T4定价只读菜单",
                    Component = "page.t4.pricing.seed",
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
                    Title = "T4定价写权限菜单",
                    Component = "page.t4.pricing.write",
                    MenuType = MenuType.Menu,
                    Order = 9652,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = auditMenuId,
                    Name = auditMenuName,
                    Path = $"/{batch.Id}u",
                    Title = "T4定价审核菜单",
                    Component = "page.t4.pricing.audit",
                    MenuType = MenuType.Menu,
                    Order = 9653,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedPricingReadButtonId,
                    Code = pricingReadPermission,
                    Desc = "T4 定价读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = pricingCreatePermission,
                    Desc = "T4 定价创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = pricingUpdatePermission,
                    Desc = "T4 定价更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = pricingDeletePermission,
                    Desc = "T4 定价删除权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = auditButtonId,
                    Code = pricingAuditPermission,
                    Desc = "T4 定价审核权限按钮",
                    MenuId = auditMenuId,
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
        registry.Register<Menu>(auditMenuId, nameof(Menu.Name), auditMenuName);

        Guid? targetQuotationId = null;
        Guid? targetQuotationGoodsId = null;
        Guid? targetProtocolId = null;
        Guid? targetProtocolGoodsId = null;
        Guid? expandedQuotationId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问报价/协议价/审核接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/quotations"))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousList.StatusCode);
            }

            using (var anonymousProtocolList = await anonymousClient.GetAsync("/api/customer-protocols"))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousProtocolList.StatusCode);
            }

            using (var anonymousAudit = await anonymousClient.PatchAsync(
                       $"/api/quotations/{managedQuotationId}/audit?isAudited=true",
                       null))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousAudit.StatusCode);
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

            // 受管报价/协议价只读核对：同商品同单位协议价低于报价价（下单优先协议价的数据前提）
            using (var managedQuotationGoodsList = await adminClient.GetAsync(
                       $"/api/quotation-goods/list?current=1&size=20&quotationId={managedQuotationId}"))
            {
                Assert.Equal(HttpStatusCode.OK, managedQuotationGoodsList.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<QuotationGoodsDto>>(managedQuotationGoodsList);
                var detail = Assert.Single(page.Records!, item => item.Id == managedQuotationGoodsId);
                Assert.Equal(expectedManagedUnitPrice, detail.UnitPrice);
                Assert.True(detail.IsOnSale);
            }

            using (var managedProtocolGoodsList = await adminClient.GetAsync(
                       $"/api/customer-protocol-goods/list?current=1&size=20&customerProtocolId={managedProtocolId}"))
            {
                Assert.Equal(HttpStatusCode.OK, managedProtocolGoodsList.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<CustomerProtocolGoodsDto>>(managedProtocolGoodsList);
                var detail = Assert.Single(page.Records!, item => item.Id == managedProtocolGoodsId);
                Assert.Equal(expectedManagedProtocolPrice, detail.ProtocolPrice);
                Assert.True(detail.ProtocolPrice < expectedManagedUnitPrice);
            }

            using (var managedUnauditedDetail = await adminClient.GetAsync(
                       $"/api/quotations/{managedUnauditedQuotationId}"))
            {
                Assert.Equal(HttpStatusCode.OK, managedUnauditedDetail.StatusCode);
                var detail = await ReadApiDataAsync<QuotationDto>(managedUnauditedDetail);
                Assert.False(detail.IsAudited);
            }

            // 操作员创建未审核临时报价（不绑定受管客户，避免改写长期联调默认报价）
            QuotationDto targetQuotation;
            using (var createQuotationResponse = await adminClient.PostAsJsonAsync(
                       "/api/quotations",
                       new CreateQuotationDto
                       {
                           Code = targetQuotationCode,
                           Name = targetQuotationName,
                           Description = "T4报价审核切片临时报价，覆盖审核与协议价优先级",
                           IsAudited = false,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createQuotationResponse.StatusCode);
                targetQuotation = await ReadApiDataAsync<QuotationDto>(createQuotationResponse);
                Assert.Equal(targetQuotationCode, targetQuotation.Code);
                Assert.False(targetQuotation.IsAudited);
                targetQuotationId = targetQuotation.Id;
                registry.Register<Quotation>(targetQuotation.Id, nameof(Quotation.Code), targetQuotationCode);
            }

            QuotationGoodsDto targetQuotationGoods;
            using (var createGoodsResponse = await adminClient.PostAsJsonAsync(
                       "/api/quotation-goods",
                       new CreateQuotationGoodsDto
                       {
                           QuotationId = targetQuotation.Id,
                           GoodsId = managedGoodsId,
                           GoodsUnitId = managedGoodsUnitId,
                           UnitPrice = tempUnitPrice,
                           MinOrderQuantity = NumericPrecision.RoundQuantity(4m),
                           IsOnSale = true,
                           Remark = quotationGoodsRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createGoodsResponse.StatusCode);
                targetQuotationGoods = await ReadApiDataAsync<QuotationGoodsDto>(createGoodsResponse);
                Assert.Equal(tempUnitPrice, targetQuotationGoods.UnitPrice);
                targetQuotationGoodsId = targetQuotationGoods.Id;
                registry.Register<QuotationGoods>(
                    targetQuotationGoods.Id,
                    nameof(QuotationGoods.Remark),
                    quotationGoodsRemark);
            }

            // 操作员创建临时协议价并挂接同商品更低单价
            CustomerProtocolDto targetProtocol;
            using (var createProtocolResponse = await adminClient.PostAsJsonAsync(
                       "/api/customer-protocols",
                       new
                       {
                           code = targetProtocolCode,
                           name = targetProtocolName,
                           quotationId = targetQuotation.Id,
                           // HTTP JSON 需带 Z 后缀，避免反序列化为 Unspecified 导致 PostgreSQL timestamptz 写入失败
                           effectiveStart = "2026-07-01T00:00:00Z",
                           effectiveEnd = "2027-12-31T23:59:59Z",
                           remark = "T4报价审核切片临时协议价，协议单价低于同商品报价单价",
                           status = (int)Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createProtocolResponse.StatusCode);
                targetProtocol = await ReadApiDataAsync<CustomerProtocolDto>(createProtocolResponse);
                Assert.Equal(targetProtocolCode, targetProtocol.Code);
                Assert.Equal(targetQuotation.Id, targetProtocol.QuotationId);
                targetProtocolId = targetProtocol.Id;
                registry.Register<CustomerProtocol>(
                    targetProtocol.Id,
                    nameof(CustomerProtocol.Code),
                    targetProtocolCode);
            }

            CustomerProtocolGoodsDto targetProtocolGoods;
            using (var createProtocolGoodsResponse = await adminClient.PostAsJsonAsync(
                       "/api/customer-protocol-goods",
                       new CreateCustomerProtocolGoodsDto
                       {
                           CustomerProtocolId = targetProtocol.Id,
                           GoodsId = managedGoodsId,
                           GoodsUnitId = managedGoodsUnitId,
                           ProtocolPrice = tempProtocolPrice,
                           MinOrderQuantity = NumericPrecision.RoundQuantity(2m),
                           Remark = protocolGoodsRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createProtocolGoodsResponse.StatusCode);
                targetProtocolGoods = await ReadApiDataAsync<CustomerProtocolGoodsDto>(createProtocolGoodsResponse);
                Assert.Equal(tempProtocolPrice, targetProtocolGoods.ProtocolPrice);
                Assert.True(targetProtocolGoods.ProtocolPrice < tempUnitPrice);
                targetProtocolGoodsId = targetProtocolGoods.Id;
                registry.Register<CustomerProtocolGoods>(
                    targetProtocolGoods.Id,
                    nameof(CustomerProtocolGoods.Remark),
                    protocolGoodsRemark);
            }

            // 审核 / 反审核临时报价
            using (var auditTrue = await adminClient.PatchAsync(
                       $"/api/quotations/{targetQuotation.Id}/audit?isAudited=true",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, auditTrue.StatusCode);
                var audited = await ReadApiDataAsync<QuotationDto>(auditTrue);
                Assert.True(audited.IsAudited);
            }

            using (var auditFalse = await adminClient.PatchAsync(
                       $"/api/quotations/{targetQuotation.Id}/audit?isAudited=false",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, auditFalse.StatusCode);
                var unaudited = await ReadApiDataAsync<QuotationDto>(auditFalse);
                Assert.False(unaudited.IsAudited);
            }

            await using (var afterAudit = fixture.CreateDbContext())
            {
                var quotationEntity = await afterAudit.Quotations.AsNoTracking()
                    .SingleAsync(item => item.Id == targetQuotation.Id);
                Assert.False(quotationEntity.IsAudited);
                Assert.Equal(adminUserId, quotationEntity.UpdateBy);

                var managedQuotationEntity = await afterAudit.Quotations.AsNoTracking()
                    .SingleAsync(item => item.Id == managedQuotationId);
                Assert.Equal(managedQuotationAudited, managedQuotationEntity.IsAudited);

                var managedUnauditedEntity = await afterAudit.Quotations.AsNoTracking()
                    .SingleAsync(item => item.Id == managedUnauditedQuotationId);
                Assert.False(managedUnauditedEntity.IsAudited);

                var managedGoodsPrice = await afterAudit.QuotationGoods.AsNoTracking()
                    .SingleAsync(item => item.Id == managedQuotationGoodsId);
                Assert.Equal(expectedManagedUnitPrice, managedGoodsPrice.UnitPrice);

                var managedProtocolPrice = await afterAudit.CustomerProtocolGoods.AsNoTracking()
                    .SingleAsync(item => item.Id == managedProtocolGoodsId);
                Assert.Equal(expectedManagedProtocolPrice, managedProtocolPrice.ProtocolPrice);
            }

            // 最小权限用户：仅定价读
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
                Assert.Contains(pricingReadPermission, info.Permissions);
                Assert.DoesNotContain(pricingCreatePermission, info.Permissions);
                Assert.DoesNotContain(pricingUpdatePermission, info.Permissions);
                Assert.DoesNotContain(pricingDeletePermission, info.Permissions);
                Assert.DoesNotContain(pricingAuditPermission, info.Permissions);
            }

            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
                Assert.DoesNotContain($"/{batch.Id}u", paths);
            }

            using (var allowedAll = await limitedClient.GetAsync("/api/quotations"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/quotations/{targetQuotation.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var allowedProtocolGoods = await limitedClient.GetAsync(
                       $"/api/customer-protocol-goods/list?current=1&size=5&customerProtocolId={targetProtocol.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedProtocolGoods.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<CustomerProtocolGoodsDto>>(allowedProtocolGoods);
                var detail = Assert.Single(page.Records!, item => item.Id == targetProtocolGoods.Id);
                Assert.True(detail.ProtocolPrice < tempUnitPrice);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/quotations",
                       new CreateQuotationDto
                       {
                           Code = deniedQuotationCode,
                           Name = deniedQuotationName,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedCreate.StatusCode);
            }

            using (var deniedAudit = await limitedClient.PatchAsync(
                       $"/api/quotations/{targetQuotation.Id}/audit?isAudited=true",
                       null))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedAudit.StatusCode);
            }

            // 扩权：分配写权限菜单后可创建，但仍无审核权限
            using (var expandWriteMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId, writeMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, expandWriteMenus.StatusCode);
            }

            LoginResDto limitedWriteLogin;
            using (var expandLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, expandLoginResponse.StatusCode);
                limitedWriteLogin = await ReadApiDataAsync<LoginResDto>(expandLoginResponse);
            }

            using var limitedWriteClient = factory.CreateClient();
            limitedWriteClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedWriteLogin.Token);
            limitedWriteClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterWriteExpand = await limitedWriteClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterWriteExpand.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterWriteExpand);
                Assert.Contains(pricingReadPermission, info.Permissions);
                Assert.Contains(pricingCreatePermission, info.Permissions);
                Assert.Contains(pricingUpdatePermission, info.Permissions);
                Assert.Contains(pricingDeletePermission, info.Permissions);
                Assert.DoesNotContain(pricingAuditPermission, info.Permissions);
            }

            QuotationDto expandedQuotation;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/quotations",
                       new CreateQuotationDto
                       {
                           Code = expandedQuotationCode,
                           Name = expandedQuotationName,
                           Description = "T4扩权报价",
                           IsAudited = false,
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedQuotation = await ReadApiDataAsync<QuotationDto>(createExpanded);
                expandedQuotationId = expandedQuotation.Id;
                registry.Register<Quotation>(expandedQuotation.Id, nameof(Quotation.Code), expandedQuotationCode);
            }

            using (var deniedAuditWithoutPermission = await limitedWriteClient.PatchAsync(
                       $"/api/quotations/{expandedQuotation.Id}/audit?isAudited=true",
                       null))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedAuditWithoutPermission.StatusCode);
            }

            // 再扩权：分配审核菜单后可审核
            using (var expandAuditMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId, writeMenuId, auditMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, expandAuditMenus.StatusCode);
            }

            LoginResDto limitedAuditLogin;
            using (var auditLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, auditLoginResponse.StatusCode);
                limitedAuditLogin = await ReadApiDataAsync<LoginResDto>(auditLoginResponse);
            }

            using var limitedAuditClient = factory.CreateClient();
            limitedAuditClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedAuditLogin.Token);
            limitedAuditClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterAuditExpand = await limitedAuditClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterAuditExpand.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterAuditExpand);
                Assert.Contains(pricingAuditPermission, info.Permissions);
            }

            using (var routesAfterAuditExpand = await limitedAuditClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesAfterAuditExpand.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesAfterAuditExpand);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}u", paths);
            }

            using (var allowedAudit = await limitedAuditClient.PatchAsync(
                       $"/api/quotations/{expandedQuotation.Id}/audit?isAudited=true",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAudit.StatusCode);
                var audited = await ReadApiDataAsync<QuotationDto>(allowedAudit);
                Assert.True(audited.IsAudited);
            }

            using (var deleteExpanded = await limitedAuditClient.DeleteAsync(
                       $"/api/quotations/{expandedQuotation.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.Quotations.AnyAsync(item =>
                    item.Id == expandedQuotation.Id || item.Code == expandedQuotationCode));
                expandedQuotationId = null;
            }

            // 缩权后写/审核权限与菜单路径收口
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
                Assert.Contains(pricingReadPermission, info.Permissions);
                Assert.DoesNotContain(pricingCreatePermission, info.Permissions);
                Assert.DoesNotContain(pricingUpdatePermission, info.Permissions);
                Assert.DoesNotContain(pricingDeletePermission, info.Permissions);
                Assert.DoesNotContain(pricingAuditPermission, info.Permissions);
            }

            using (var routesAfterShrink = await limitedReloginClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesAfterShrink.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesAfterShrink);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
                Assert.DoesNotContain($"/{batch.Id}u", paths);
            }

            using (var deniedAuditAfterShrink = await limitedReloginClient.PatchAsync(
                       $"/api/quotations/{targetQuotation.Id}/audit?isAudited=true",
                       null))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedAuditAfterShrink.StatusCode);
            }

            // 操作员按依赖逆序删除临时协议价与报价
            using (var deleteProtocolGoods = await adminClient.DeleteAsync(
                       $"/api/customer-protocol-goods/{targetProtocolGoods.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteProtocolGoods.StatusCode);
            }

            using (var deleteProtocol = await adminClient.DeleteAsync(
                       $"/api/customer-protocols/{targetProtocol.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteProtocol.StatusCode);
            }

            using (var deleteQuotationGoods = await adminClient.DeleteAsync(
                       $"/api/quotation-goods/{targetQuotationGoods.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteQuotationGoods.StatusCode);
            }

            using (var deleteQuotation = await adminClient.DeleteAsync(
                       $"/api/quotations/{targetQuotation.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteQuotation.StatusCode);
            }

            await using (var afterDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterDelete.Quotations.AnyAsync(item =>
                    item.Id == targetQuotation.Id || item.Code == targetQuotationCode));
                Assert.False(await afterDelete.CustomerProtocols.AnyAsync(item =>
                    item.Id == targetProtocol.Id || item.Code == targetProtocolCode));
                Assert.False(await afterDelete.QuotationGoods.AnyAsync(item =>
                    item.Id == targetQuotationGoods.Id));
                Assert.False(await afterDelete.CustomerProtocolGoods.AnyAsync(item =>
                    item.Id == targetProtocolGoods.Id));
                targetQuotationId = null;
                targetQuotationGoodsId = null;
                targetProtocolId = null;
                targetProtocolGoodsId = null;
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

                var residualProtocolGoodsRemarks = new List<string> { protocolGoodsRemark };
                var residualProtocolGoodsIds = new List<Guid>();
                if (targetProtocolGoodsId.HasValue)
                    residualProtocolGoodsIds.Add(targetProtocolGoodsId.Value);

                var residualProtocolGoods = await cleanupContext.CustomerProtocolGoods
                    .Where(item => residualProtocolGoodsIds.Contains(item.Id)
                                   || (item.Remark != null && residualProtocolGoodsRemarks.Contains(item.Remark))
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualProtocolGoods.Count > 0)
                {
                    cleanupContext.CustomerProtocolGoods.RemoveRange(residualProtocolGoods);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualProtocolCodes = new List<string> { targetProtocolCode };
                var residualProtocolIds = new List<Guid>();
                if (targetProtocolId.HasValue)
                    residualProtocolIds.Add(targetProtocolId.Value);

                var residualProtocols = await cleanupContext.CustomerProtocols
                    .Where(item => residualProtocolIds.Contains(item.Id)
                                   || residualProtocolCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualProtocols.Count > 0)
                {
                    cleanupContext.CustomerProtocols.RemoveRange(residualProtocols);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualQuotationGoodsRemarks = new List<string> { quotationGoodsRemark };
                var residualQuotationGoodsIds = new List<Guid>();
                if (targetQuotationGoodsId.HasValue)
                    residualQuotationGoodsIds.Add(targetQuotationGoodsId.Value);

                var residualQuotationGoods = await cleanupContext.QuotationGoods
                    .Where(item => residualQuotationGoodsIds.Contains(item.Id)
                                   || (item.Remark != null && residualQuotationGoodsRemarks.Contains(item.Remark))
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualQuotationGoods.Count > 0)
                {
                    cleanupContext.QuotationGoods.RemoveRange(residualQuotationGoods);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualQuotationCodes = new List<string>
                {
                    targetQuotationCode,
                    expandedQuotationCode,
                    deniedQuotationCode,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualQuotationIds = new List<Guid>();
                if (targetQuotationId.HasValue)
                    residualQuotationIds.Add(targetQuotationId.Value);
                if (expandedQuotationId.HasValue)
                    residualQuotationIds.Add(expandedQuotationId.Value);

                var residualQuotations = await cleanupContext.Quotations
                    .Where(item => residualQuotationIds.Contains(item.Id)
                                   || residualQuotationCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualQuotations.Count > 0)
                {
                    cleanupContext.Quotations.RemoveRange(residualQuotations);
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

                var residualRoleIds = new List<Guid> { limitedRoleId };
                var residualRoleMenus = await cleanupContext.RoleMenus
                    .Where(relation => residualRoleIds.Contains(relation.RoleId)
                                       || relation.MenuId == seedMenuId
                                       || relation.MenuId == writeMenuId
                                       || relation.MenuId == auditMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoles = await cleanupContext.Roles
                    .Where(role => residualRoleIds.Contains(role.Id)
                                   || role.Code == limitedRoleCode
                                   || (role.Code != null && role.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualRoles.Count > 0)
                {
                    cleanupContext.Roles.RemoveRange(residualRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtonIds = new List<Guid>
                {
                    seedPricingReadButtonId,
                    writeCreateButtonId,
                    writeUpdateButtonId,
                    writeDeleteButtonId,
                    auditButtonId
                };
                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => residualButtonIds.Contains(button.Id)
                                     || button.MenuId == seedMenuId
                                     || button.MenuId == writeMenuId
                                     || button.MenuId == auditMenuId
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
                                   || menu.Id == auditMenuId
                                   || menu.Name == seedMenuName
                                   || menu.Name == writeMenuName
                                   || menu.Name == auditMenuName)
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
                || menu.Id == auditMenuId
                || menu.Name == seedMenuName
                || menu.Name == writeMenuName
                || menu.Name == auditMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedPricingReadButtonId
                || button.Id == writeCreateButtonId
                || button.Id == writeUpdateButtonId
                || button.Id == writeDeleteButtonId
                || button.Id == auditButtonId
                || button.CreateName == createName));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == adminUserId || relation.UserId == limitedUserId));
            Assert.False(await residualContext.RoleMenus.AnyAsync(relation =>
                relation.RoleId == limitedRoleId
                || relation.MenuId == seedMenuId
                || relation.MenuId == writeMenuId
                || relation.MenuId == auditMenuId));
            Assert.False(await residualContext.Quotations.AnyAsync(item =>
                item.Code == targetQuotationCode
                || item.Code == expandedQuotationCode
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.CustomerProtocols.AnyAsync(item =>
                item.Code == targetProtocolCode
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.QuotationGoods.AnyAsync(item =>
                item.Remark == quotationGoodsRemark
                || (item.Remark != null && item.Remark.StartsWith(batch.Id))));
            Assert.False(await residualContext.CustomerProtocolGoods.AnyAsync(item =>
                item.Remark == protocolGoodsRemark
                || (item.Remark != null && item.Remark.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));

            // 既有 Admin 角色与受管报价/协议价/商品主数据必须保留且价格未改动
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Quotations.AnyAsync(item =>
                item.Id == managedQuotationId && item.IsAudited == managedQuotationAudited));
            Assert.True(await residualContext.Quotations.AnyAsync(item =>
                item.Id == managedUnauditedQuotationId && !item.IsAudited));
            Assert.True(await residualContext.CustomerProtocols.AnyAsync(item => item.Id == managedProtocolId));
            Assert.True(await residualContext.Goods.AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));

            var residualManagedQuotationGoods = await residualContext.QuotationGoods.AsNoTracking()
                .SingleAsync(item => item.Id == managedQuotationGoodsId);
            Assert.Equal(expectedManagedUnitPrice, residualManagedQuotationGoods.UnitPrice);
            var residualManagedProtocolGoods = await residualContext.CustomerProtocolGoods.AsNoTracking()
                .SingleAsync(item => item.Id == managedProtocolGoodsId);
            Assert.Equal(expectedManagedProtocolPrice, residualManagedProtocolGoods.ProtocolPrice);
            Assert.True(residualManagedProtocolGoods.ProtocolPrice < residualManagedQuotationGoods.UnitPrice);
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
