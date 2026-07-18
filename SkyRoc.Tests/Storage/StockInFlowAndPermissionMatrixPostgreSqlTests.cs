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
using Shared.Utils;
using SkyRoc.Tests.Common;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Storage;

/// <summary>
///     T7 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证其他入库审核建批次、移动加权成本、只追加台账、
///     反审核回滚（含账面归零不出现负库存）、非法状态与幂等拒绝，以及库存读写权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class StockInFlowAndPermissionMatrixPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员创建其他入库草稿→审核建批次并写正向台账→再次入库同批次按移动加权重算成本→反审核回滚账面到零；
    ///     非法状态（重复审核/审核后编辑/未审核反审核）与最小权限（仅读允许、写/审核 403）、扩权/缩权收口；
    ///     库存总览/批次/台账读校验；受管商品/单位/仓库不被改动；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task StockIn_OtherInboundBatchCostLedgerReverseAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedReadButtonId = Guid.NewGuid();
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
        var batchNo = $"{batch.Id}-BATCH";
        var costRemark = $"{batch.Id}-成本入库";
        var reverseRemark = $"{batch.Id}-反审核";
        var deniedRemark = $"{batch.Id}-拒绝入库";
        var password = "SkyRocStockInPerm!2026";
        var userAgent = $"SkyRoc-T7-StockIn/{batch.Id}";
        var createName = "T7-StockInFlow";

        var storageReadPermission = PermissionCodes.Business.Storage.Read;
        var storageCreatePermission = PermissionCodes.Business.Storage.Create;
        var storageUpdatePermission = PermissionCodes.Business.Storage.Update;
        var storageDeletePermission = PermissionCodes.Business.Storage.Delete;

        // 移动加权成本样本：基础单位换算率 1，两次入库同一批次
        // 第一次 10 @ 4.0 -> 批次 10、单位成本 4.0
        // 第二次 30 @ 8.0 -> 批次 40、单位成本 (10*4 + 30*8)/40 = 7.0
        var firstQuantity = NumericPrecision.RoundQuantity(10m);
        var firstUnitPrice = NumericPrecision.RoundMoney(4m);
        var secondQuantity = NumericPrecision.RoundQuantity(30m);
        var secondUnitPrice = NumericPrecision.RoundMoney(8m);
        var expectedQuantityAfterSecond = NumericPrecision.RoundQuantity(40m);
        var expectedUnitCostAfterSecond = NumericPrecision.RoundMoney(7m);

        Guid adminRoleId;
        Guid managedGoodsId;
        string managedGoodsCode;
        Guid managedGoodsUnitId;
        decimal managedUnitConversion;
        Guid managedWareId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var goodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var goodsUnitCode = DemoDataStableKeyCatalog.Create("GOODS-UNIT", 1);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);

            var goods = await seedContext.Set<GoodsEntity>().AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsCode);
            Assert.NotNull(goods);
            managedGoodsId = goods.Id;
            managedGoodsCode = goods.Code;

            var goodsUnit = await seedContext.GoodsUnits.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsUnitCode);
            Assert.NotNull(goodsUnit);
            Assert.Equal(goods.Id, goodsUnit.GoodsId);
            managedGoodsUnitId = goodsUnit.Id;
            managedUnitConversion = goodsUnit.ConversionRate;
            // 受管基础单位换算率必须为 1，否则移动加权成本样本假设不成立
            Assert.Equal(1m, managedUnitConversion);

            var ware = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == wareCode);
            Assert.NotNull(ware);
            managedWareId = ware.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T7 其他入库最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T7其他入库操作员",
                    Gender = GenderType.Male,
                    Phone = "13900009801",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T7其他入库只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900009802",
                    Email = $"{batch.Id}-l@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.UserRoles.AddRangeAsync(
                new UserRole { UserId = adminUserId, RoleId = adminRoleId },
                new UserRole { UserId = limitedUserId, RoleId = limitedRoleId });

            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = seedMenuId,
                    Name = seedMenuName,
                    Path = $"/{batch.Id}s",
                    Title = "T7库存只读菜单",
                    Component = "page.t7.stockin.seed",
                    MenuType = MenuType.Menu,
                    Order = 9731,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T7库存写权限菜单",
                    Component = "page.t7.stockin.write",
                    MenuType = MenuType.Menu,
                    Order = 9732,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedReadButtonId,
                    Code = storageReadPermission,
                    Desc = "T7 库存读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = storageCreatePermission,
                    Desc = "T7 库存创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = storageUpdatePermission,
                    Desc = "T7 库存更新权限按钮（含审核/反审核）",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = storageDeletePermission,
                    Desc = "T7 库存删除权限按钮",
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

        Guid? costOrderId = null;
        Guid? deniedOrderId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问其他入库列表与创建
            using (var anonymousList = await anonymousClient.GetAsync("/api/stock-in/other/list?current=1&size=20"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherPayload(managedWareId, managedGoodsId, managedGoodsUnitId, batchNo,
                           firstQuantity, firstUnitPrice, deniedRemark)))
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

            // 创建其他入库草稿（首行 10 @ 4.0）
            StockInOrderDto costOrder;
            using (var createResponse = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherPayload(managedWareId, managedGoodsId, managedGoodsUnitId, batchNo,
                           firstQuantity, firstUnitPrice, costRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
                costOrder = await ReadApiDataAsync<StockInOrderDto>(createResponse);
                Assert.Equal(StockInOrderType.Other, costOrder.OrderType);
                Assert.Equal(StockDocumentStatus.Draft, costOrder.BusinessStatus);
                Assert.Equal(managedWareId, costOrder.WareId);
                var detail = Assert.Single(costOrder.Details);
                Assert.Equal(managedGoodsId, detail.GoodsId);
                Assert.Equal(managedGoodsCode, detail.GoodsCode);
                Assert.Equal(firstQuantity, detail.Quantity);
                Assert.Equal(firstQuantity, detail.BaseQuantity);
                Assert.Equal(firstUnitPrice, detail.UnitPrice);
                Assert.Null(detail.StockBatchId);
                costOrderId = costOrder.Id;
                registry.Register<StockInOrder>(costOrder.Id, nameof(StockInOrder.Remark), costRemark);
            }

            // 草稿状态尚未产生批次或台账
            await using (var beforeAudit = fixture.CreateDbContext())
            {
                Assert.False(await beforeAudit.StockBatches.AnyAsync(item =>
                    item.WareId == managedWareId && item.GoodsId == managedGoodsId && item.BatchNo == batchNo));
            }

            // 审核入库：建批次、写正向台账、账面 10、单位成本 4.0
            using (var auditResponse = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{costOrder.Id}/audit",
                       new StockInAuditDto { Remark = $"{costRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
                var audited = await ReadApiDataAsync<StockInOrderDto>(auditResponse);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
                Assert.NotNull(audited.AuditTime);
                Assert.NotNull(Assert.Single(audited.Details).StockBatchId);
            }

            await using (var afterFirstAudit = fixture.CreateDbContext())
            {
                var stockBatch = await afterFirstAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == batchNo);
                createdBatchId = stockBatch.Id;
                Assert.Equal(firstQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(firstQuantity, stockBatch.AvailableQuantity);
                Assert.Equal(firstUnitPrice, stockBatch.UnitCost);

                var ledgers = await afterFirstAudit.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == costOrder.Id)
                    .ToListAsync();
                var inbound = Assert.Single(ledgers);
                Assert.Equal(StockLedgerDirection.Increase, inbound.Direction);
                Assert.Equal(StockLedgerSourceType.OtherInbound, inbound.SourceType);
                Assert.Equal(firstQuantity, inbound.ChangeQuantity);
                Assert.Equal(firstQuantity, inbound.BalanceQuantity);
                Assert.Equal(firstUnitPrice, inbound.UnitCost);
                Assert.Null(inbound.ReversedFromLedgerId);
            }

            // 已审核单据禁止重复审核与编辑（非法状态）
            using (var reAudit = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{costOrder.Id}/audit",
                       new StockInAuditDto { Remark = "重复审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reAudit, ResponseCode.DatabaseError);
            }

            using (var editAudited = await adminClient.PutAsJsonAsync(
                       "/api/stock-in/other",
                       BuildUpdateOtherPayload(costOrder.Id, managedWareId, managedGoodsId, managedGoodsUnitId,
                           batchNo, firstQuantity, firstUnitPrice, "审核后不应编辑")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(editAudited, ResponseCode.DatabaseError);
            }

            // 第二张其他入库进入同一批次（30 @ 8.0），审核后移动加权成本重算为 7.0、账面 40
            StockInOrderDto secondOrder;
            var secondRemark = $"{batch.Id}-第二次入库";
            using (var createSecond = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherPayload(managedWareId, managedGoodsId, managedGoodsUnitId, batchNo,
                           secondQuantity, secondUnitPrice, secondRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createSecond.StatusCode);
                secondOrder = await ReadApiDataAsync<StockInOrderDto>(createSecond);
                registry.Register<StockInOrder>(secondOrder.Id, nameof(StockInOrder.Remark), secondRemark);
            }

            using (var auditSecond = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{secondOrder.Id}/audit",
                       new StockInAuditDto { Remark = $"{secondRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditSecond.StatusCode);
                var audited = await ReadApiDataAsync<StockInOrderDto>(auditSecond);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
            }

            await using (var afterSecondAudit = fixture.CreateDbContext())
            {
                var stockBatch = await afterSecondAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(expectedQuantityAfterSecond, stockBatch.CurrentQuantity);
                Assert.Equal(expectedQuantityAfterSecond, stockBatch.AvailableQuantity);
                Assert.Equal(expectedUnitCostAfterSecond, stockBatch.UnitCost);

                // 台账只追加：两张来源单各一条正向流水，批次现有两条增加流水
                var batchLedgers = await afterSecondAudit.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.StockBatchId == createdBatchId!.Value)
                    .ToListAsync();
                Assert.Equal(2, batchLedgers.Count);
                Assert.All(batchLedgers, ledger =>
                    Assert.Equal(StockLedgerDirection.Increase, ledger.Direction));
                var secondInbound = Assert.Single(batchLedgers, ledger => ledger.SourceOrderId == secondOrder.Id);
                Assert.Equal(secondQuantity, secondInbound.ChangeQuantity);
                Assert.Equal(expectedQuantityAfterSecond, secondInbound.BalanceQuantity);
                Assert.Equal(secondUnitPrice, secondInbound.UnitCost);
            }

            // 库存总览/批次/台账读接口核对（独立断言，不用被测查询反推）
            using (var overview = await adminClient.GetAsync(
                       $"/api/stock/overview?current=1&size=20&wareId={managedWareId}&goodsId={managedGoodsId}"))
            {
                Assert.Equal(HttpStatusCode.OK, overview.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockOverviewDto>>(overview);
                // 总览按仓库+商品聚合所有批次，可能叠加既有受管库存；此处只核对本轮批次已被计入总量
                var row = Assert.Single(page.Records!, item =>
                    item.WareId == managedWareId && item.GoodsId == managedGoodsId);
                Assert.True(row.CurrentQuantity >= expectedQuantityAfterSecond,
                    "总览账面数量应至少包含本轮入库的 40 基础单位");
                Assert.True(row.AvailableQuantity >= expectedQuantityAfterSecond,
                    "总览可用数量应至少包含本轮入库的 40 基础单位");
                Assert.Equal(row.CurrentQuantity - row.AvailableQuantity, row.OccupiedQuantity);
            }

            using (var batches = await adminClient.GetAsync(
                       $"/api/stock/batches?current=1&size=20&goodsId={managedGoodsId}&batchNo={Uri.EscapeDataString(batchNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, batches.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockBatchDto>>(batches);
                var row = Assert.Single(page.Records!, item => item.Id == createdBatchId!.Value);
                Assert.Equal(expectedQuantityAfterSecond, row.CurrentQuantity);
                Assert.Equal(expectedUnitCostAfterSecond, row.UnitCost);
            }

            using (var ledgersResponse = await adminClient.GetAsync(
                       $"/api/stock/ledgers?current=1&size=50&stockBatchId={createdBatchId!.Value}"))
            {
                Assert.Equal(HttpStatusCode.OK, ledgersResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockLedgerDto>>(ledgersResponse);
                var rows = page.Records!.Where(item => item.StockBatchId == createdBatchId!.Value).ToList();
                Assert.Equal(2, rows.Count);
                Assert.All(rows, row => Assert.True(row.SignedChangeQuantity > 0m));
            }

            // 最小权限用户登录：仅库存读
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

            using (var limitedInfo = await limitedClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, limitedInfo.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(limitedInfo);
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

            using (var allowedOverview = await limitedClient.GetAsync(
                       $"/api/stock/overview?current=1&size=20&goodsId={managedGoodsId}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedOverview.StatusCode);
            }

            using (var allowedList = await limitedClient.GetAsync("/api/stock-in/other/list?current=1&size=20"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherPayload(managedWareId, managedGoodsId, managedGoodsUnitId, batchNo,
                           firstQuantity, firstUnitPrice, deniedRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedAudit = await limitedClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{costOrder.Id}/audit",
                       new StockInAuditDto { Remark = "无权限审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAudit, ResponseCode.Forbidden);
            }

            using (var deniedReverse = await limitedClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{costOrder.Id}/reverse-audit",
                       new StockInAuditDto { Remark = "无权限反审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedReverse, ResponseCode.Forbidden);
            }

            // 未授权用户创建/审核被拒后不得残留业务数据
            await using (var afterDenied = fixture.CreateDbContext())
            {
                Assert.False(await afterDenied.StockInOrders.AnyAsync(item => item.Remark == deniedRemark));
            }

            // 扩权：分配写权限菜单后重新登录，可创建其他入库草稿
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
            using (var expandLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, expandLogin.StatusCode);
                limitedExpandedLogin = await ReadApiDataAsync<LoginResDto>(expandLogin);
            }

            using var limitedWriteClient = factory.CreateClient();
            limitedWriteClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedExpandedLogin.Token);
            limitedWriteClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterExpand = await limitedWriteClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterExpand.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterExpand);
                Assert.Contains(storageCreatePermission, info.Permissions);
                Assert.Contains(storageUpdatePermission, info.Permissions);
                Assert.Contains(storageDeletePermission, info.Permissions);
            }

            StockInOrderDto deniedOrder;
            using (var createDenied = await limitedWriteClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherPayload(managedWareId, managedGoodsId, managedGoodsUnitId, batchNo,
                           firstQuantity, firstUnitPrice, deniedRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createDenied.StatusCode);
                deniedOrder = await ReadApiDataAsync<StockInOrderDto>(createDenied);
                deniedOrderId = deniedOrder.Id;
                registry.Register<StockInOrder>(deniedOrder.Id, nameof(StockInOrder.Remark), deniedRemark);
            }

            // 扩权用户删除自己创建的草稿
            using (var deleteDraft = await limitedWriteClient.DeleteAsync($"/api/stock-in/other/{deniedOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteDraft.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteDraft));
            }

            await using (var afterDeleteDraft = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteDraft.StockInOrders.AnyAsync(item => item.Id == deniedOrder.Id));
                deniedOrderId = null;
            }

            // 缩权后写权限与写菜单路由收口并再次 403
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

            using (var deniedAfterShrink = await limitedReloginClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherPayload(managedWareId, managedGoodsId, managedGoodsUnitId, batchNo,
                           firstQuantity, firstUnitPrice, $"{batch.Id}-缩权拒绝")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAfterShrink, ResponseCode.Forbidden);
            }

            // 反审核第二张入库单：账面回滚到 10、单位成本回到 4.0，追加反向减少流水
            using (var reverseSecond = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{secondOrder.Id}/reverse-audit",
                       new StockInAuditDto { Remark = $"{secondRemark}-反审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, reverseSecond.StatusCode);
                var reversed = await ReadApiDataAsync<StockInOrderDto>(reverseSecond);
                Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);
                Assert.NotNull(reversed.ReverseTime);
            }

            await using (var afterReverseSecond = fixture.CreateDbContext())
            {
                var stockBatch = await afterReverseSecond.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(firstQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(firstQuantity, stockBatch.AvailableQuantity);
                Assert.Equal(firstUnitPrice, stockBatch.UnitCost);

                var reversalLedger = Assert.Single(await afterReverseSecond.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == secondOrder.Id
                                     && ledger.Direction == StockLedgerDirection.Decrease)
                    .ToListAsync());
                Assert.NotNull(reversalLedger.ReversedFromLedgerId);
                Assert.Equal(secondQuantity, reversalLedger.ChangeQuantity);
                Assert.Equal(firstQuantity, reversalLedger.BalanceQuantity);
            }

            // 未审核（已反审核）单据不能再次反审核
            using (var reReverse = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{secondOrder.Id}/reverse-audit",
                       new StockInAuditDto { Remark = "重复反审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reReverse, ResponseCode.DatabaseError);
            }

            // 反审核第一张入库单：账面归零但不出现负库存，批次成本清零
            using (var reverseFirst = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{costOrder.Id}/reverse-audit",
                       new StockInAuditDto { Remark = reverseRemark }))
            {
                Assert.Equal(HttpStatusCode.OK, reverseFirst.StatusCode);
                var reversed = await ReadApiDataAsync<StockInOrderDto>(reverseFirst);
                Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);
            }

            await using (var afterReverseFirst = fixture.CreateDbContext())
            {
                var stockBatch = await afterReverseFirst.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(0m, stockBatch.CurrentQuantity);
                Assert.Equal(0m, stockBatch.AvailableQuantity);
                Assert.True(stockBatch.CurrentQuantity >= 0m, "反审核后账面数量不得为负");
                Assert.True(stockBatch.AvailableQuantity >= 0m, "反审核后可用数量不得为负");
                Assert.Equal(0m, stockBatch.UnitCost);

                // 全库范围内不存在负库存批次
                Assert.False(await afterReverseFirst.StockBatches.AnyAsync(item =>
                    item.CurrentQuantity < 0m || item.AvailableQuantity < 0m));

                // 四条流水：两条正向 + 两条反向，均只追加
                var ledgers = await afterReverseFirst.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.StockBatchId == createdBatchId!.Value)
                    .ToListAsync();
                Assert.Equal(4, ledgers.Count);
                Assert.Equal(2, ledgers.Count(ledger => ledger.Direction == StockLedgerDirection.Increase));
                Assert.Equal(2, ledgers.Count(ledger => ledger.Direction == StockLedgerDirection.Decrease));
            }

            await using var auditContext = fixture.CreateDbContext();
            var loginLogs = await auditContext.LoginLogs.AsNoTracking()
                .Where(log => log.Username == adminUsername || log.Username == limitedUsername)
                .ToListAsync();
            Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
            Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
            Assert.All(loginLogs, log =>
                Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal));
            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            Assert.True(await auditContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await auditContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await auditContext.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));
            Assert.True(await auditContext.Wares.AnyAsync(item => item.Id == managedWareId));
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            // 库存台账（Restrict→批次）与批次由本轮审核派生，无归属备注，需在批次登记项之前显式清理；
            // 先按本轮来源单据删除全部台账，再删入库单（明细级联），最后删空批次。
            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualOrderRemarks = new[]
                {
                    costRemark,
                    $"{batch.Id}-第二次入库",
                    deniedRemark,
                    $"{batch.Id}-缩权拒绝"
                };
                var residualOrderIds = new List<Guid>();
                if (costOrderId.HasValue)
                    residualOrderIds.Add(costOrderId.Value);
                if (deniedOrderId.HasValue)
                    residualOrderIds.Add(deniedOrderId.Value);

                var residualOrders = await cleanupContext.StockInOrders
                    .Where(item => residualOrderIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (residualOrderRemarks.Contains(item.Remark)
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();
                var residualOrderIdSet = residualOrders.Select(item => item.Id).ToHashSet();

                var residualLedgers = await cleanupContext.StockLedgers
                    .Where(ledger => residualOrderIdSet.Contains(ledger.SourceOrderId)
                                     || (createdBatchId.HasValue && ledger.StockBatchId == createdBatchId.Value))
                    .ToListAsync();
                if (residualLedgers.Count > 0)
                {
                    cleanupContext.StockLedgers.RemoveRange(residualLedgers);
                    await cleanupContext.SaveChangesAsync();
                }

                if (residualOrders.Count > 0)
                {
                    // 明细随入库单级联删除
                    cleanupContext.StockInOrders.RemoveRange(residualOrders);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualBatches = await cleanupContext.StockBatches
                    .Where(item => item.BatchNo == batchNo
                                   || (createdBatchId.HasValue && item.Id == createdBatchId.Value))
                    .ToListAsync();
                if (residualBatches.Count > 0)
                {
                    cleanupContext.StockBatches.RemoveRange(residualBatches);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            // 再清本轮批次登记实体；UserRole 随用户级联，RoleMenu 随角色/菜单级联
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };
                var residualUsernames = new[] { adminUsername, limitedUsername };

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

                var residualRoles = await cleanupContext.Roles
                    .Where(role => role.Id == limitedRoleId
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
                    seedReadButtonId,
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
            Assert.False(await residualContext.StockInOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockBatches.AnyAsync(item => item.BatchNo == batchNo));
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
                button.Id == seedReadButtonId
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
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            // 既有 Admin 角色与受管商品/单位/仓库必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
        }
    }

    private static object BuildCreateOtherPayload(
        Guid wareId,
        Guid goodsId,
        Guid goodsUnitId,
        string batchNo,
        decimal quantity,
        decimal unitPrice,
        string remark)
    {
        return new
        {
            wareId,
            inTime = "2026-07-18T09:00:00Z",
            remark,
            details = new[]
            {
                new
                {
                    goodsId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    batchNo,
                    remark = "T7其他入库明细"
                }
            }
        };
    }

    private static object BuildUpdateOtherPayload(
        Guid id,
        Guid wareId,
        Guid goodsId,
        Guid goodsUnitId,
        string batchNo,
        decimal quantity,
        decimal unitPrice,
        string remark)
    {
        return new
        {
            id,
            wareId,
            inTime = "2026-07-18T09:30:00Z",
            remark,
            details = new[]
            {
                new
                {
                    goodsId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    batchNo,
                    remark = "T7其他入库明细-编辑"
                }
            }
        };
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

