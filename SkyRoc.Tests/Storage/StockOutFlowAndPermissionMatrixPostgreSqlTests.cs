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
///     T7 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证其他出库审核锁定并扣减批次可用库存、只追加负向台账、
///     可用库存不足拒绝审核、反审核恢复库存（含无负库存）、非法状态与幂等拒绝，以及库存读写权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class StockOutFlowAndPermissionMatrixPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库审核建批次（50 @ 5.0）→操作员创建其他出库草稿（草稿态无台账、批次不变）→审核锁定并扣减批次可用库存（40）
    ///     并写一条负向台账→重复审核/审核后编辑非法拒绝→第二张出库单请求超过可用库存审核被拒且批次与台账不变（可用库存扣减守恒）
    ///     →反审核恢复批次到 50 并追加带来源的反向增加流水→重复反审核 502→全库无负库存；
    ///     最小权限（仅读允许、创建/审核/反审核 403）、扩权创建并删除草稿、缩权收口再次 403；
    ///     库存总览/批次/台账读校验；受管商品/单位/仓库不被改动；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task StockOut_OtherOutboundAuditBatchDeductionReverseAndPermissionMatrix_OnPostgreSql()
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
        var inboundRemark = $"{batch.Id}-入库建批次";
        var outboundRemark = $"{batch.Id}-其他出库";
        var overRemark = $"{batch.Id}-超额出库";
        var reverseRemark = $"{batch.Id}-反审核";
        var deniedRemark = $"{batch.Id}-拒绝出库";
        var password = "SkyRocStockOutPerm!2026";
        var userAgent = $"SkyRoc-T7-StockOut/{batch.Id}";
        var createName = "T7-StockOutFlow";

        var storageReadPermission = PermissionCodes.Business.Storage.Read;
        var storageCreatePermission = PermissionCodes.Business.Storage.Create;
        var storageUpdatePermission = PermissionCodes.Business.Storage.Update;
        var storageDeletePermission = PermissionCodes.Business.Storage.Delete;

        // 库存样本：基础单位换算率 1
        // 入库 50 @ 5.0 -> 批次 50、单位成本 5.0
        // 出库 30 -> 批次 20、单位成本仍 5.0（出库不改成本）
        // 第二张出库请求 25 > 剩余 20 -> 审核拒绝，批次守恒
        var inboundQuantity = NumericPrecision.RoundQuantity(50m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(5m);
        var outboundQuantity = NumericPrecision.RoundQuantity(30m);
        var outboundUnitPrice = NumericPrecision.RoundMoney(6m);
        var overQuantity = NumericPrecision.RoundQuantity(25m);
        var expectedAfterOutbound = NumericPrecision.RoundQuantity(20m);

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
            // 受管基础单位换算率必须为 1，否则可用库存扣减样本假设不成立
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
                Desc = "T7 其他出库最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T7其他出库操作员",
                    Gender = GenderType.Male,
                    Phone = "13900009811",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T7其他出库只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900009812",
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
                    Component = "page.t7.stockout.seed",
                    MenuType = MenuType.Menu,
                    Order = 9741,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T7库存写权限菜单",
                    Component = "page.t7.stockout.write",
                    MenuType = MenuType.Menu,
                    Order = 9742,
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

        Guid? inboundOrderId = null;
        Guid? outboundOrderId = null;
        Guid? overOrderId = null;
        Guid? deniedOrderId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问其他出库列表与创建
            using (var anonymousList = await anonymousClient.GetAsync("/api/stock-out/other/list?current=1&size=20"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/stock-out/other",
                       BuildCreateOtherOutPayload(managedWareId, Guid.NewGuid(), managedGoodsUnitId,
                           outboundQuantity, outboundUnitPrice, deniedRemark)))
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

            // 先经其他入库审核建批次（50 @ 5.0），为出库准备可用库存
            StockInOrderDto inboundOrder;
            using (var createInbound = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherInPayload(managedWareId, managedGoodsId, managedGoodsUnitId, batchNo,
                           inboundQuantity, inboundUnitPrice, inboundRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createInbound.StatusCode);
                inboundOrder = await ReadApiDataAsync<StockInOrderDto>(createInbound);
                inboundOrderId = inboundOrder.Id;
                registry.Register<StockInOrder>(inboundOrder.Id, nameof(StockInOrder.Remark), inboundRemark);
            }

            using (var auditInbound = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{inboundOrder.Id}/audit",
                       new StockInAuditDto { Remark = $"{inboundRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditInbound.StatusCode);
                var audited = await ReadApiDataAsync<StockInOrderDto>(auditInbound);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
            }

            await using (var afterInbound = fixture.CreateDbContext())
            {
                var stockBatch = await afterInbound.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == batchNo);
                createdBatchId = stockBatch.Id;
                Assert.Equal(inboundQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);
                Assert.Equal(inboundUnitPrice, stockBatch.UnitCost);
            }

            // 创建其他出库草稿（30，从上面批次扣减）
            StockOutOrderDto outboundOrder;
            using (var createOut = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/other",
                       BuildCreateOtherOutPayload(managedWareId, createdBatchId!.Value, managedGoodsUnitId,
                           outboundQuantity, outboundUnitPrice, outboundRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createOut.StatusCode);
                outboundOrder = await ReadApiDataAsync<StockOutOrderDto>(createOut);
                Assert.Equal(StockOutOrderType.Other, outboundOrder.OrderType);
                Assert.Equal(StockDocumentStatus.Draft, outboundOrder.BusinessStatus);
                Assert.Equal(managedWareId, outboundOrder.WareId);
                var detail = Assert.Single(outboundOrder.Details);
                Assert.Equal(managedGoodsId, detail.GoodsId);
                Assert.Equal(managedGoodsCode, detail.GoodsCode);
                Assert.Equal(outboundQuantity, detail.Quantity);
                Assert.Equal(outboundQuantity, detail.BaseQuantity);
                Assert.Equal(createdBatchId!.Value, detail.StockBatchId);
                outboundOrderId = outboundOrder.Id;
                registry.Register<StockOutOrder>(outboundOrder.Id, nameof(StockOutOrder.Remark), outboundRemark);
            }

            // 草稿状态尚未扣减批次，也没有出库台账
            await using (var beforeAudit = fixture.CreateDbContext())
            {
                var stockBatch = await beforeAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(inboundQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);
                Assert.False(await beforeAudit.StockLedgers.AnyAsync(ledger =>
                    ledger.SourceOrderId == outboundOrder.Id));
            }

            // 审核出库：锁定批次、可用库存扣减到 20、写一条负向台账
            using (var auditOut = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/other/{outboundOrder.Id}/audit",
                       new StockOutAuditDto { Remark = $"{outboundRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditOut.StatusCode);
                var audited = await ReadApiDataAsync<StockOutOrderDto>(auditOut);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
                Assert.NotNull(audited.AuditTime);
            }

            await using (var afterOutbound = fixture.CreateDbContext())
            {
                var stockBatch = await afterOutbound.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(expectedAfterOutbound, stockBatch.CurrentQuantity);
                Assert.Equal(expectedAfterOutbound, stockBatch.AvailableQuantity);
                // 出库不改变移动加权成本
                Assert.Equal(inboundUnitPrice, stockBatch.UnitCost);

                var ledgers = await afterOutbound.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == outboundOrder.Id)
                    .ToListAsync();
                var outbound = Assert.Single(ledgers);
                Assert.Equal(StockLedgerDirection.Decrease, outbound.Direction);
                Assert.Equal(StockLedgerSourceType.OtherOutbound, outbound.SourceType);
                Assert.Equal(outboundQuantity, outbound.ChangeQuantity);
                Assert.Equal(expectedAfterOutbound, outbound.BalanceQuantity);
                Assert.Equal(inboundUnitPrice, outbound.UnitCost);
                Assert.Null(outbound.ReversedFromLedgerId);
            }

            // 已审核单据禁止重复审核与编辑（非法状态）
            using (var reAudit = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/other/{outboundOrder.Id}/audit",
                       new StockOutAuditDto { Remark = "重复审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reAudit, ResponseCode.DatabaseError);
            }

            using (var editAudited = await adminClient.PutAsJsonAsync(
                       "/api/stock-out/other",
                       BuildUpdateOtherOutPayload(outboundOrder.Id, managedWareId, createdBatchId!.Value,
                           managedGoodsUnitId, outboundQuantity, outboundUnitPrice, "审核后不应编辑")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(editAudited, ResponseCode.DatabaseError);
            }

            // 第二张其他出库请求 25 > 剩余可用 20，审核被拒且批次/台账守恒
            StockOutOrderDto overOrder;
            using (var createOver = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/other",
                       BuildCreateOtherOutPayload(managedWareId, createdBatchId!.Value, managedGoodsUnitId,
                           overQuantity, outboundUnitPrice, overRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createOver.StatusCode);
                overOrder = await ReadApiDataAsync<StockOutOrderDto>(createOver);
                overOrderId = overOrder.Id;
                registry.Register<StockOutOrder>(overOrder.Id, nameof(StockOutOrder.Remark), overRemark);
            }

            using (var auditOver = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/other/{overOrder.Id}/audit",
                       new StockOutAuditDto { Remark = "可用库存不足审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(auditOver, ResponseCode.DatabaseError);
            }

            await using (var afterOver = fixture.CreateDbContext())
            {
                // 审核失败回滚：批次可用库存不变、无新增台账、单据仍为草稿
                var stockBatch = await afterOver.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(expectedAfterOutbound, stockBatch.CurrentQuantity);
                Assert.Equal(expectedAfterOutbound, stockBatch.AvailableQuantity);
                Assert.False(await afterOver.StockLedgers.AnyAsync(ledger =>
                    ledger.SourceOrderId == overOrder.Id));
                var reloaded = await afterOver.StockOutOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == overOrder.Id);
                Assert.Equal(StockDocumentStatus.Draft, reloaded.BusinessStatus);
            }

            // 删除超额草稿，避免长期残留
            using (var deleteOver = await adminClient.DeleteAsync($"/api/stock-out/other/{overOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteOver.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteOver));
                overOrderId = null;
            }

            // 库存总览/批次/台账读接口核对
            using (var overview = await adminClient.GetAsync(
                       $"/api/stock/overview?current=1&size=20&wareId={managedWareId}&goodsId={managedGoodsId}"))
            {
                Assert.Equal(HttpStatusCode.OK, overview.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockOverviewDto>>(overview);
                var row = Assert.Single(page.Records!, item =>
                    item.WareId == managedWareId && item.GoodsId == managedGoodsId);
                // 总览按仓库+商品聚合所有批次，可能叠加既有受管库存，只核对不低于本轮出库后余额
                Assert.True(row.CurrentQuantity >= expectedAfterOutbound,
                    "总览账面数量应至少包含本轮出库后剩余的 20 基础单位");
                Assert.True(row.AvailableQuantity >= expectedAfterOutbound,
                    "总览可用数量应至少包含本轮出库后剩余的 20 基础单位");
                Assert.Equal(row.CurrentQuantity - row.AvailableQuantity, row.OccupiedQuantity);
            }

            using (var batches = await adminClient.GetAsync(
                       $"/api/stock/batches?current=1&size=20&goodsId={managedGoodsId}&batchNo={Uri.EscapeDataString(batchNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, batches.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockBatchDto>>(batches);
                var row = Assert.Single(page.Records!, item => item.Id == createdBatchId!.Value);
                Assert.Equal(expectedAfterOutbound, row.CurrentQuantity);
                Assert.Equal(inboundUnitPrice, row.UnitCost);
            }

            using (var ledgersResponse = await adminClient.GetAsync(
                       $"/api/stock/ledgers?current=1&size=50&stockBatchId={createdBatchId!.Value}"))
            {
                Assert.Equal(HttpStatusCode.OK, ledgersResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockLedgerDto>>(ledgersResponse);
                var outboundRow = Assert.Single(page.Records!,
                    item => item.SourceOrderId == outboundOrder.Id);
                Assert.True(outboundRow.SignedChangeQuantity < 0m, "出库台账带方向数量应为负");
                Assert.Equal(StockLedgerDirection.Decrease, outboundRow.Direction);
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

            using (var allowedList = await limitedClient.GetAsync("/api/stock-out/other/list?current=1&size=20"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/stock-out/other",
                       BuildCreateOtherOutPayload(managedWareId, createdBatchId!.Value, managedGoodsUnitId,
                           NumericPrecision.RoundQuantity(1m), outboundUnitPrice, deniedRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedAudit = await limitedClient.PostAsJsonAsync(
                       $"/api/stock-out/other/{outboundOrder.Id}/audit",
                       new StockOutAuditDto { Remark = "无权限审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAudit, ResponseCode.Forbidden);
            }

            using (var deniedReverse = await limitedClient.PostAsJsonAsync(
                       $"/api/stock-out/other/{outboundOrder.Id}/reverse-audit",
                       new StockOutAuditDto { Remark = "无权限反审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedReverse, ResponseCode.Forbidden);
            }

            // 未授权用户创建/审核被拒后不得残留业务数据
            await using (var afterDenied = fixture.CreateDbContext())
            {
                Assert.False(await afterDenied.StockOutOrders.AnyAsync(item => item.Remark == deniedRemark));
            }

            // 扩权：分配写权限菜单后重新登录，可创建其他出库草稿
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

            StockOutOrderDto deniedOrder;
            using (var createDenied = await limitedWriteClient.PostAsJsonAsync(
                       "/api/stock-out/other",
                       BuildCreateOtherOutPayload(managedWareId, createdBatchId!.Value, managedGoodsUnitId,
                           NumericPrecision.RoundQuantity(1m), outboundUnitPrice, deniedRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createDenied.StatusCode);
                deniedOrder = await ReadApiDataAsync<StockOutOrderDto>(createDenied);
                deniedOrderId = deniedOrder.Id;
                registry.Register<StockOutOrder>(deniedOrder.Id, nameof(StockOutOrder.Remark), deniedRemark);
            }

            using (var deleteDraft = await limitedWriteClient.DeleteAsync($"/api/stock-out/other/{deniedOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteDraft.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteDraft));
            }

            await using (var afterDeleteDraft = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteDraft.StockOutOrders.AnyAsync(item => item.Id == deniedOrder.Id));
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
                       "/api/stock-out/other",
                       BuildCreateOtherOutPayload(managedWareId, createdBatchId!.Value, managedGoodsUnitId,
                           NumericPrecision.RoundQuantity(1m), outboundUnitPrice, $"{batch.Id}-缩权拒绝")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAfterShrink, ResponseCode.Forbidden);
            }

            // 反审核出库单：批次恢复到 50，追加带来源的反向增加流水
            using (var reverseOut = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/other/{outboundOrder.Id}/reverse-audit",
                       new StockOutAuditDto { Remark = reverseRemark }))
            {
                Assert.Equal(HttpStatusCode.OK, reverseOut.StatusCode);
                var reversed = await ReadApiDataAsync<StockOutOrderDto>(reverseOut);
                Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);
                Assert.NotNull(reversed.ReverseTime);
            }

            await using (var afterReverse = fixture.CreateDbContext())
            {
                var stockBatch = await afterReverse.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(inboundQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);
                Assert.Equal(inboundUnitPrice, stockBatch.UnitCost);
                Assert.True(stockBatch.CurrentQuantity >= 0m, "反审核后账面数量不得为负");
                Assert.True(stockBatch.AvailableQuantity >= 0m, "反审核后可用数量不得为负");

                var reversalLedger = Assert.Single(await afterReverse.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == outboundOrder.Id
                                     && ledger.Direction == StockLedgerDirection.Increase)
                    .ToListAsync());
                Assert.NotNull(reversalLedger.ReversedFromLedgerId);
                Assert.Equal(outboundQuantity, reversalLedger.ChangeQuantity);
                Assert.Equal(inboundQuantity, reversalLedger.BalanceQuantity);

                // 出库单两条流水：一条负向 + 一条反向正流水，均只追加
                var outboundLedgers = await afterReverse.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == outboundOrder.Id)
                    .ToListAsync();
                Assert.Equal(2, outboundLedgers.Count);
                Assert.Equal(1, outboundLedgers.Count(ledger => ledger.Direction == StockLedgerDirection.Decrease));
                Assert.Equal(1, outboundLedgers.Count(ledger => ledger.Direction == StockLedgerDirection.Increase));

                // 全库范围内不存在负库存批次
                Assert.False(await afterReverse.StockBatches.AnyAsync(item =>
                    item.CurrentQuantity < 0m || item.AvailableQuantity < 0m));
            }

            // 已反审核单据不能再次反审核
            using (var reReverse = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/other/{outboundOrder.Id}/reverse-audit",
                       new StockOutAuditDto { Remark = "重复反审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reReverse, ResponseCode.DatabaseError);
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

            // 库存台账（Restrict→批次）与批次由本轮入库/出库审核派生、无归属备注，需在批次登记项之前显式清理；
            // 先按本轮来源单据与批次删台账，再删出库单与入库单（明细级联），最后删空批次。
            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualStockOutRemarks = new[]
                {
                    outboundRemark,
                    overRemark,
                    deniedRemark,
                    $"{batch.Id}-缩权拒绝"
                };
                var residualStockOutIds = new List<Guid>();
                if (outboundOrderId.HasValue)
                    residualStockOutIds.Add(outboundOrderId.Value);
                if (overOrderId.HasValue)
                    residualStockOutIds.Add(overOrderId.Value);
                if (deniedOrderId.HasValue)
                    residualStockOutIds.Add(deniedOrderId.Value);

                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => residualStockOutIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (residualStockOutRemarks.Contains(item.Remark)
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualStockInIds = new List<Guid>();
                if (inboundOrderId.HasValue)
                    residualStockInIds.Add(inboundOrderId.Value);
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => residualStockInIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == inboundRemark || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualOrderIdSet = residualStockOutOrders.Select(item => item.Id)
                    .Concat(residualStockInOrders.Select(item => item.Id))
                    .ToHashSet();

                var residualLedgers = await cleanupContext.StockLedgers
                    .Where(ledger => residualOrderIdSet.Contains(ledger.SourceOrderId)
                                     || (createdBatchId.HasValue && ledger.StockBatchId == createdBatchId.Value))
                    .ToListAsync();
                if (residualLedgers.Count > 0)
                {
                    cleanupContext.StockLedgers.RemoveRange(residualLedgers);
                    await cleanupContext.SaveChangesAsync();
                }

                if (residualStockOutOrders.Count > 0)
                {
                    cleanupContext.StockOutOrders.RemoveRange(residualStockOutOrders);
                    await cleanupContext.SaveChangesAsync();
                }

                if (residualStockInOrders.Count > 0)
                {
                    cleanupContext.StockInOrders.RemoveRange(residualStockInOrders);
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
            Assert.False(await residualContext.StockOutOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
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

    private static object BuildCreateOtherInPayload(
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

    private static object BuildCreateOtherOutPayload(
        Guid wareId,
        Guid stockBatchId,
        Guid goodsUnitId,
        decimal quantity,
        decimal unitPrice,
        string remark)
    {
        return new
        {
            wareId,
            outTime = "2026-07-18T10:00:00Z",
            remark,
            details = new[]
            {
                new
                {
                    stockBatchId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    remark = "T7其他出库明细"
                }
            }
        };
    }

    private static object BuildUpdateOtherOutPayload(
        Guid id,
        Guid wareId,
        Guid stockBatchId,
        Guid goodsUnitId,
        decimal quantity,
        decimal unitPrice,
        string remark)
    {
        return new
        {
            id,
            wareId,
            outTime = "2026-07-18T10:30:00Z",
            remark,
            details = new[]
            {
                new
                {
                    stockBatchId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    remark = "T7其他出库明细-编辑"
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
