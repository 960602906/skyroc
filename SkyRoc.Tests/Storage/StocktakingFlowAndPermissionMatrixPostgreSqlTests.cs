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
///     T7 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证库存盘点创建快照、盘盈盘亏审核只追加调整台账、
///     零差异跳过流水、快照后库存变更拒绝审核且回滚、重复审核拒绝，以及库存读写权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class StocktakingFlowAndPermissionMatrixPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库审核建两个批次（GAIN 40@5.0、LOSS 30@5.0，另 MATCH 10@5.0）→操作员创建盘点草稿
    ///     （盘盈+5 / 盘亏-8 / 账实相符 0，草稿态无台账、批次不变）→审核后批次变为 45/22/10 并只追加两条调整台账
    ///     （Increase+Decrease，零差异无流水）→重复审核 502→第二张盘点在创建后对批次做出库导致快照失效，审核 502 且批次/台账守恒
    ///     →库存总览/批次/台账读接口核对→最小权限仅读允许、创建/审核 403→扩权可创建草稿→缩权收口再次 403
    ///     →全库无负库存；登录审计脱敏与临时盘点/出入库/台账/批次/用户角色菜单按钮/登录操作日志精确清理；
    ///     既有 Admin 与受管商品单位仓库不被修改或删除。
    /// </summary>
    [Fact]
    public async Task Stocktaking_GainLossAdjustmentLedgerSnapshotAndPermissionMatrix_OnPostgreSql()
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
        var gainBatchNo = $"{batch.Id}-G";
        var lossBatchNo = $"{batch.Id}-L";
        var matchBatchNo = $"{batch.Id}-M";
        var inboundRemark = $"{batch.Id}-入库建批次";
        var stocktakingRemark = $"{batch.Id}-盘点主单";
        var staleRemark = $"{batch.Id}-快照失效盘点";
        var deniedRemark = $"{batch.Id}-拒绝盘点";
        var expandRemark = $"{batch.Id}-扩权草稿";
        var outboundRemark = $"{batch.Id}-快照扰动出库";
        var password = "SkyRocStocktaking!2026";
        var userAgent = $"SkyRoc-T7-Stocktaking/{batch.Id}";
        var createName = "T7-StocktakingFlow";

        var storageReadPermission = PermissionCodes.Business.Storage.Read;
        var storageCreatePermission = PermissionCodes.Business.Storage.Create;
        var storageUpdatePermission = PermissionCodes.Business.Storage.Update;
        var storageDeletePermission = PermissionCodes.Business.Storage.Delete;

        // 库存样本：基础单位换算率 1
        // GAIN 批次 40 @ 5.0 → 实盘 45（盘盈 +5）→ 审核后 45
        // LOSS 批次 30 @ 5.0 → 实盘 22（盘亏 -8）→ 审核后 22
        // MATCH 批次 10 @ 5.0 → 实盘 10（零差异）→ 审核后 10 且不写台账
        var gainBook = NumericPrecision.RoundQuantity(40m);
        var lossBook = NumericPrecision.RoundQuantity(30m);
        var matchBook = NumericPrecision.RoundQuantity(10m);
        var unitCost = NumericPrecision.RoundMoney(5m);
        var gainActual = NumericPrecision.RoundQuantity(45m);
        var lossActual = NumericPrecision.RoundQuantity(22m);
        var matchActual = NumericPrecision.RoundQuantity(10m);
        var gainDiff = NumericPrecision.RoundQuantity(5m);
        var lossDiff = NumericPrecision.RoundQuantity(-8m);
        var expectedGainAfter = gainActual;
        var expectedLossAfter = lossActual;
        var expectedMatchAfter = matchActual;
        var expectedTotalBook = NumericPrecision.RoundQuantity(gainBook + lossBook + matchBook);
        var expectedTotalActual = NumericPrecision.RoundQuantity(gainActual + lossActual + matchActual);
        var expectedTotalDiff = NumericPrecision.RoundQuantity(gainDiff + lossDiff);

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
                Desc = "T7 库存盘点最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T7盘点操作员",
                    Gender = GenderType.Male,
                    Phone = "13900009821",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T7盘点只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900009822",
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
                    Component = "page.t7.stocktaking.seed",
                    MenuType = MenuType.Menu,
                    Order = 9751,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T7库存写权限菜单",
                    Component = "page.t7.stocktaking.write",
                    MenuType = MenuType.Menu,
                    Order = 9752,
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
                    Desc = "T7 库存更新权限按钮（含盘点审核）",
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
        Guid? stocktakingOrderId = null;
        Guid? staleOrderId = null;
        Guid? expandOrderId = null;
        Guid? outboundOrderId = null;
        Guid? gainBatchId = null;
        Guid? lossBatchId = null;
        Guid? matchBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问盘点列表与创建
            using (var anonymousList = await anonymousClient.GetAsync("/api/stocktaking/list?current=1&size=20"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/stocktaking",
                       BuildCreateStocktakingPayload(managedWareId, Guid.NewGuid(), matchActual, deniedRemark)))
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

            // 其他入库审核一次写入三个批次，供盘盈/盘亏/账实相符样本
            StockInOrderDto inboundOrder;
            using (var createInbound = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherInPayload(
                           managedWareId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           unitCost,
                           inboundRemark,
                           (gainBatchNo, gainBook),
                           (lossBatchNo, lossBook),
                           (matchBatchNo, matchBook))))
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
                var gainBatch = await afterInbound.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == gainBatchNo);
                var lossBatch = await afterInbound.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == lossBatchNo);
                var matchBatch = await afterInbound.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == matchBatchNo);
                gainBatchId = gainBatch.Id;
                lossBatchId = lossBatch.Id;
                matchBatchId = matchBatch.Id;
                Assert.Equal(gainBook, gainBatch.CurrentQuantity);
                Assert.Equal(lossBook, lossBatch.CurrentQuantity);
                Assert.Equal(matchBook, matchBatch.CurrentQuantity);
                Assert.Equal(unitCost, gainBatch.UnitCost);
                Assert.Equal(unitCost, lossBatch.UnitCost);
                Assert.Equal(unitCost, matchBatch.UnitCost);
            }

            // 创建盘点草稿：盘盈 + 盘亏 + 账实相符
            StocktakingOrderDto stocktakingOrder;
            using (var createStocktaking = await adminClient.PostAsJsonAsync(
                       "/api/stocktaking",
                       BuildCreateStocktakingMultiPayload(
                           managedWareId,
                           stocktakingRemark,
                           (gainBatchId!.Value, gainActual, "盘盈复核"),
                           (lossBatchId!.Value, lossActual, "盘亏复核"),
                           (matchBatchId!.Value, matchActual, "账实相符"))))
            {
                Assert.Equal(HttpStatusCode.OK, createStocktaking.StatusCode);
                stocktakingOrder = await ReadApiDataAsync<StocktakingOrderDto>(createStocktaking);
                Assert.StartsWith("STK", stocktakingOrder.StocktakingNo);
                Assert.Equal(StockDocumentStatus.Draft, stocktakingOrder.BusinessStatus);
                Assert.Equal(managedWareId, stocktakingOrder.WareId);
                Assert.False(stocktakingOrder.IsAdjustmentApplied);
                Assert.Equal(expectedTotalBook, stocktakingOrder.TotalBookQuantity);
                Assert.Equal(expectedTotalActual, stocktakingOrder.TotalActualQuantity);
                Assert.Equal(expectedTotalDiff, stocktakingOrder.TotalDifferenceQuantity);
                Assert.Equal(3, stocktakingOrder.Details.Count);

                var gainDetail = Assert.Single(stocktakingOrder.Details, d => d.StockBatchId == gainBatchId);
                Assert.Equal(managedGoodsId, gainDetail.GoodsId);
                Assert.Equal(managedGoodsCode, gainDetail.GoodsCode);
                Assert.Equal(gainBatchNo, gainDetail.BatchNo);
                Assert.Equal(gainBook, gainDetail.BookQuantity);
                Assert.Equal(gainActual, gainDetail.ActualQuantity);
                Assert.Equal(gainDiff, gainDetail.DifferenceQuantity);
                Assert.Equal(unitCost, gainDetail.UnitCost);
                Assert.Equal(NumericPrecision.RoundMoney(gainDiff * unitCost), gainDetail.DifferenceAmount);

                var lossDetail = Assert.Single(stocktakingOrder.Details, d => d.StockBatchId == lossBatchId);
                Assert.Equal(lossBook, lossDetail.BookQuantity);
                Assert.Equal(lossActual, lossDetail.ActualQuantity);
                Assert.Equal(lossDiff, lossDetail.DifferenceQuantity);
                Assert.Equal(NumericPrecision.RoundMoney(lossDiff * unitCost), lossDetail.DifferenceAmount);

                var matchDetail = Assert.Single(stocktakingOrder.Details, d => d.StockBatchId == matchBatchId);
                Assert.Equal(matchBook, matchDetail.BookQuantity);
                Assert.Equal(0m, matchDetail.DifferenceQuantity);
                Assert.Equal(0m, matchDetail.DifferenceAmount);

                stocktakingOrderId = stocktakingOrder.Id;
                registry.Register<StocktakingOrder>(stocktakingOrder.Id, nameof(StocktakingOrder.Remark),
                    stocktakingRemark);
            }

            // 草稿态：批次不变、无盘点调整台账
            await using (var beforeAudit = fixture.CreateDbContext())
            {
                Assert.Equal(gainBook,
                    (await beforeAudit.StockBatches.AsNoTracking().SingleAsync(b => b.Id == gainBatchId)).CurrentQuantity);
                Assert.Equal(lossBook,
                    (await beforeAudit.StockBatches.AsNoTracking().SingleAsync(b => b.Id == lossBatchId)).CurrentQuantity);
                Assert.Equal(matchBook,
                    (await beforeAudit.StockBatches.AsNoTracking().SingleAsync(b => b.Id == matchBatchId)).CurrentQuantity);
                Assert.False(await beforeAudit.StockLedgers.AnyAsync(ledger =>
                    ledger.SourceOrderId == stocktakingOrder.Id));
            }

            // 审核盘点：锁定批次、盘盈/盘亏生效、零差异跳过流水、只追加两条 Stocktaking 台账
            using (var auditStocktaking = await adminClient.PostAsJsonAsync(
                       $"/api/stocktaking/{stocktakingOrder.Id}/audit",
                       new StocktakingAuditDto { Remark = $"{stocktakingRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditStocktaking.StatusCode);
                var audited = await ReadApiDataAsync<StocktakingOrderDto>(auditStocktaking);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
                Assert.True(audited.IsAdjustmentApplied);
                Assert.NotNull(audited.AuditTime);
                Assert.NotNull(audited.AdjustmentTime);
                Assert.Equal(adminUserId, audited.AuditUserId);
                Assert.Equal(expectedTotalDiff, audited.TotalDifferenceQuantity);
            }

            await using (var afterAudit = fixture.CreateDbContext())
            {
                var gainBatch = await afterAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == gainBatchId);
                Assert.Equal(expectedGainAfter, gainBatch.CurrentQuantity);
                Assert.Equal(expectedGainAfter, gainBatch.AvailableQuantity);
                Assert.Equal(unitCost, gainBatch.UnitCost);

                var lossBatch = await afterAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == lossBatchId);
                Assert.Equal(expectedLossAfter, lossBatch.CurrentQuantity);
                Assert.Equal(expectedLossAfter, lossBatch.AvailableQuantity);
                Assert.Equal(unitCost, lossBatch.UnitCost);

                var matchBatch = await afterAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == matchBatchId);
                Assert.Equal(expectedMatchAfter, matchBatch.CurrentQuantity);
                Assert.Equal(expectedMatchAfter, matchBatch.AvailableQuantity);

                var ledgers = await afterAudit.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == stocktakingOrder.Id)
                    .OrderBy(ledger => ledger.Direction)
                    .ToListAsync();
                Assert.Equal(2, ledgers.Count);
                Assert.All(ledgers, ledger =>
                {
                    Assert.Equal(StockLedgerSourceType.Stocktaking, ledger.SourceType);
                    Assert.Equal(unitCost, ledger.UnitCost);
                    Assert.Null(ledger.ReversedFromLedgerId);
                });

                var increase = Assert.Single(ledgers, l => l.Direction == StockLedgerDirection.Increase);
                Assert.Equal(gainBatchId, increase.StockBatchId);
                Assert.Equal(gainDiff, increase.ChangeQuantity);
                Assert.Equal(expectedGainAfter, increase.BalanceQuantity);
                Assert.Equal(NumericPrecision.RoundMoney(gainDiff * unitCost), increase.TotalCost);

                var decrease = Assert.Single(ledgers, l => l.Direction == StockLedgerDirection.Decrease);
                Assert.Equal(lossBatchId, decrease.StockBatchId);
                Assert.Equal(NumericPrecision.RoundQuantity(Math.Abs(lossDiff)), decrease.ChangeQuantity);
                Assert.Equal(expectedLossAfter, decrease.BalanceQuantity);
                Assert.Equal(NumericPrecision.RoundMoney(Math.Abs(lossDiff) * unitCost), decrease.TotalCost);

                // 零差异批次不得写盘点调整流水
                Assert.DoesNotContain(ledgers, l => l.StockBatchId == matchBatchId);

                Assert.False(await afterAudit.StockBatches.AnyAsync(item =>
                    item.CurrentQuantity < 0m || item.AvailableQuantity < 0m));
            }

            // 已审核禁止重复审核（幂等拒绝）
            using (var reAudit = await adminClient.PostAsJsonAsync(
                       $"/api/stocktaking/{stocktakingOrder.Id}/audit",
                       new StocktakingAuditDto { Remark = "重复审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reAudit, ResponseCode.DatabaseError);
            }

            await using (var afterReAudit = fixture.CreateDbContext())
            {
                Assert.Equal(2, await afterReAudit.StockLedgers.CountAsync(ledger =>
                    ledger.SourceOrderId == stocktakingOrder.Id));
                Assert.Equal(expectedGainAfter,
                    (await afterReAudit.StockBatches.AsNoTracking().SingleAsync(b => b.Id == gainBatchId)).CurrentQuantity);
            }

            // 第二张盘点：创建后对 LOSS 批次出库扰动库存，审核应因快照失效被拒且回滚
            StocktakingOrderDto staleOrder;
            using (var createStale = await adminClient.PostAsJsonAsync(
                       "/api/stocktaking",
                       BuildCreateStocktakingPayload(
                           managedWareId,
                           lossBatchId!.Value,
                           NumericPrecision.RoundQuantity(20m),
                           staleRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createStale.StatusCode);
                staleOrder = await ReadApiDataAsync<StocktakingOrderDto>(createStale);
                Assert.Equal(StockDocumentStatus.Draft, staleOrder.BusinessStatus);
                Assert.Equal(expectedLossAfter, Assert.Single(staleOrder.Details).BookQuantity);
                staleOrderId = staleOrder.Id;
                registry.Register<StocktakingOrder>(staleOrder.Id, nameof(StocktakingOrder.Remark), staleRemark);
            }

            // 对 LOSS 批次出库 2，使账面与盘点快照不一致
            var disturbQty = NumericPrecision.RoundQuantity(2m);
            StockOutOrderDto outboundOrder;
            using (var createOut = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/other",
                       BuildCreateOtherOutPayload(
                           managedWareId,
                           lossBatchId!.Value,
                           managedGoodsUnitId,
                           disturbQty,
                           unitCost,
                           outboundRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createOut.StatusCode);
                outboundOrder = await ReadApiDataAsync<StockOutOrderDto>(createOut);
                outboundOrderId = outboundOrder.Id;
                registry.Register<StockOutOrder>(outboundOrder.Id, nameof(StockOutOrder.Remark), outboundRemark);
            }

            using (var auditOut = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/other/{outboundOrder.Id}/audit",
                       new StockOutAuditDto { Remark = $"{outboundRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditOut.StatusCode);
                var auditedOut = await ReadApiDataAsync<StockOutOrderDto>(auditOut);
                Assert.Equal(StockDocumentStatus.Audited, auditedOut.BusinessStatus);
            }

            var lossAfterDisturb = NumericPrecision.RoundQuantity(expectedLossAfter - disturbQty);
            await using (var afterDisturb = fixture.CreateDbContext())
            {
                Assert.Equal(lossAfterDisturb,
                    (await afterDisturb.StockBatches.AsNoTracking().SingleAsync(b => b.Id == lossBatchId)).CurrentQuantity);
            }

            using (var auditStale = await adminClient.PostAsJsonAsync(
                       $"/api/stocktaking/{staleOrder.Id}/audit",
                       new StocktakingAuditDto { Remark = "快照后库存已变" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(auditStale, ResponseCode.DatabaseError);
            }

            await using (var afterStaleReject = fixture.CreateDbContext())
            {
                // 审核失败回滚：批次保持扰动后数量、无本盘点台账、单据仍草稿、未标记已调整
                var lossBatch = await afterStaleReject.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == lossBatchId);
                Assert.Equal(lossAfterDisturb, lossBatch.CurrentQuantity);
                Assert.False(await afterStaleReject.StockLedgers.AnyAsync(ledger =>
                    ledger.SourceOrderId == staleOrder.Id));
                var reloaded = await afterStaleReject.StocktakingOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == staleOrder.Id);
                Assert.Equal(StockDocumentStatus.Draft, reloaded.BusinessStatus);
                Assert.False(reloaded.IsAdjustmentApplied);
            }

            // 库存总览/批次/台账读接口核对
            using (var overview = await adminClient.GetAsync(
                       $"/api/stock/overview?current=1&size=20&wareId={managedWareId}&goodsId={managedGoodsId}"))
            {
                Assert.Equal(HttpStatusCode.OK, overview.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockOverviewDto>>(overview);
                // 总览按仓库+商品+基础单位聚合；历史联调批次可能使用不同基础单位，故可能多行
                var rows = (page.Records ?? [])
                    .Where(item => item.WareId == managedWareId && item.GoodsId == managedGoodsId)
                    .ToList();
                Assert.NotEmpty(rows);
                var minQty = NumericPrecision.RoundQuantity(
                    expectedGainAfter + lossAfterDisturb + expectedMatchAfter);
                Assert.True(rows.Sum(item => item.CurrentQuantity) >= minQty,
                    "总览账面数量合计应至少包含本轮盘点与扰动后的批次合计");
                Assert.All(rows, row =>
                    Assert.Equal(row.CurrentQuantity - row.AvailableQuantity, row.OccupiedQuantity));
            }

            using (var batches = await adminClient.GetAsync(
                       $"/api/stock/batches?current=1&size=20&goodsId={managedGoodsId}&batchNo={Uri.EscapeDataString(gainBatchNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, batches.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockBatchDto>>(batches);
                var row = Assert.Single(page.Records!, item => item.Id == gainBatchId);
                Assert.Equal(expectedGainAfter, row.CurrentQuantity);
                Assert.Equal(unitCost, row.UnitCost);
            }

            using (var ledgersResponse = await adminClient.GetAsync(
                       $"/api/stock/ledgers?current=1&size=50&stockBatchId={gainBatchId}"))
            {
                Assert.Equal(HttpStatusCode.OK, ledgersResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockLedgerDto>>(ledgersResponse);
                var gainLedger = Assert.Single(page.Records!,
                    item => item.SourceOrderId == stocktakingOrder.Id);
                Assert.True(gainLedger.SignedChangeQuantity > 0m, "盘盈台账带方向数量应为正");
                Assert.Equal(StockLedgerDirection.Increase, gainLedger.Direction);
                Assert.Equal(StockLedgerSourceType.Stocktaking, gainLedger.SourceType);
            }

            using (var listResponse = await adminClient.GetAsync(
                       $"/api/stocktaking/list?current=1&size=20&keyword={Uri.EscapeDataString(stocktakingOrder.StocktakingNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StocktakingOrderDto>>(listResponse);
                Assert.Contains(page.Records!, item => item.Id == stocktakingOrder.Id);
            }

            using (var detailResponse = await adminClient.GetAsync($"/api/stocktaking/{stocktakingOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
                var detail = await ReadApiDataAsync<StocktakingOrderDto>(detailResponse);
                Assert.Equal(StockDocumentStatus.Audited, detail.BusinessStatus);
                Assert.True(detail.IsAdjustmentApplied);
                Assert.Equal(3, detail.Details.Count);
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

            using (var allowedList = await limitedClient.GetAsync("/api/stocktaking/list?current=1&size=20"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/stocktaking",
                       BuildCreateStocktakingPayload(
                           managedWareId,
                           matchBatchId!.Value,
                           matchActual,
                           deniedRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedAudit = await limitedClient.PostAsJsonAsync(
                       $"/api/stocktaking/{stocktakingOrder.Id}/audit",
                       new StocktakingAuditDto { Remark = "无权限审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAudit, ResponseCode.Forbidden);
            }

            await using (var afterDenied = fixture.CreateDbContext())
            {
                Assert.False(await afterDenied.StocktakingOrders.AnyAsync(item => item.Remark == deniedRemark));
            }

            // 扩权：分配写权限菜单后重新登录，可创建盘点草稿
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

            StocktakingOrderDto expandOrder;
            using (var createExpand = await limitedWriteClient.PostAsJsonAsync(
                       "/api/stocktaking",
                       BuildCreateStocktakingPayload(
                           managedWareId,
                           matchBatchId!.Value,
                           matchActual,
                           expandRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createExpand.StatusCode);
                expandOrder = await ReadApiDataAsync<StocktakingOrderDto>(createExpand);
                Assert.Equal(StockDocumentStatus.Draft, expandOrder.BusinessStatus);
                expandOrderId = expandOrder.Id;
                registry.Register<StocktakingOrder>(expandOrder.Id, nameof(StocktakingOrder.Remark), expandRemark);
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
                       "/api/stocktaking",
                       BuildCreateStocktakingPayload(
                           managedWareId,
                           matchBatchId!.Value,
                           matchActual,
                           $"{batch.Id}-缩权拒绝")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAfterShrink, ResponseCode.Forbidden);
            }

            using (var deniedAuditAfterShrink = await limitedReloginClient.PostAsJsonAsync(
                       $"/api/stocktaking/{expandOrder.Id}/audit",
                       new StocktakingAuditDto { Remark = "缩权后审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAuditAfterShrink, ResponseCode.Forbidden);
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
            Assert.False(await auditContext.StockBatches.AnyAsync(item =>
                item.CurrentQuantity < 0m || item.AvailableQuantity < 0m));
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            // 台账 Restrict→批次；盘点/出入库由本轮审核派生。先删台账，再删出库/盘点/入库，最后删空批次。
            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualStocktakingRemarks = new[]
                {
                    stocktakingRemark,
                    staleRemark,
                    deniedRemark,
                    expandRemark,
                    $"{batch.Id}-缩权拒绝"
                };
                var residualStocktakingIds = new List<Guid>();
                if (stocktakingOrderId.HasValue)
                    residualStocktakingIds.Add(stocktakingOrderId.Value);
                if (staleOrderId.HasValue)
                    residualStocktakingIds.Add(staleOrderId.Value);
                if (expandOrderId.HasValue)
                    residualStocktakingIds.Add(expandOrderId.Value);

                var residualStocktakingOrders = await cleanupContext.StocktakingOrders
                    .Where(item => residualStocktakingIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (residualStocktakingRemarks.Contains(item.Remark)
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualStockOutIds = new List<Guid>();
                if (outboundOrderId.HasValue)
                    residualStockOutIds.Add(outboundOrderId.Value);
                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => residualStockOutIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == outboundRemark || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualStockInIds = new List<Guid>();
                if (inboundOrderId.HasValue)
                    residualStockInIds.Add(inboundOrderId.Value);
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => residualStockInIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == inboundRemark || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualBatchIds = new List<Guid>();
                if (gainBatchId.HasValue)
                    residualBatchIds.Add(gainBatchId.Value);
                if (lossBatchId.HasValue)
                    residualBatchIds.Add(lossBatchId.Value);
                if (matchBatchId.HasValue)
                    residualBatchIds.Add(matchBatchId.Value);

                var residualOrderIdSet = residualStocktakingOrders.Select(item => item.Id)
                    .Concat(residualStockOutOrders.Select(item => item.Id))
                    .Concat(residualStockInOrders.Select(item => item.Id))
                    .ToHashSet();

                var residualLedgers = await cleanupContext.StockLedgers
                    .Where(ledger => residualOrderIdSet.Contains(ledger.SourceOrderId)
                                     || residualBatchIds.Contains(ledger.StockBatchId))
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

                if (residualStocktakingOrders.Count > 0)
                {
                    cleanupContext.StocktakingOrders.RemoveRange(residualStocktakingOrders);
                    await cleanupContext.SaveChangesAsync();
                }

                if (residualStockInOrders.Count > 0)
                {
                    cleanupContext.StockInOrders.RemoveRange(residualStockInOrders);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualBatches = await cleanupContext.StockBatches
                    .Where(item => residualBatchIds.Contains(item.Id)
                                   || item.BatchNo == gainBatchNo
                                   || item.BatchNo == lossBatchNo
                                   || item.BatchNo == matchBatchNo)
                    .ToListAsync();
                if (residualBatches.Count > 0)
                {
                    cleanupContext.StockBatches.RemoveRange(residualBatches);
                    await cleanupContext.SaveChangesAsync();
                }
            }

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
            Assert.False(await residualContext.StocktakingOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockOutOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockInOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockBatches.AnyAsync(item =>
                item.BatchNo == gainBatchNo
                || item.BatchNo == lossBatchNo
                || item.BatchNo == matchBatchNo));
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
        decimal unitPrice,
        string remark,
        params (string BatchNo, decimal Quantity)[] lines)
    {
        return new
        {
            wareId,
            inTime = "2026-07-18T09:00:00Z",
            remark,
            details = lines.Select(line => new
            {
                goodsId,
                goodsUnitId,
                quantity = line.Quantity,
                unitPrice,
                batchNo = line.BatchNo,
                remark = "T7盘点入库明细"
            }).ToArray()
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
                    remark = "T7盘点扰动出库明细"
                }
            }
        };
    }

    private static object BuildCreateStocktakingPayload(
        Guid wareId,
        Guid stockBatchId,
        decimal actualQuantity,
        string remark)
    {
        return new
        {
            wareId,
            remark,
            details = new[]
            {
                new
                {
                    stockBatchId,
                    actualQuantity,
                    remark = "T7盘点明细"
                }
            }
        };
    }

    private static object BuildCreateStocktakingMultiPayload(
        Guid wareId,
        string remark,
        params (Guid StockBatchId, decimal ActualQuantity, string DetailRemark)[] lines)
    {
        return new
        {
            wareId,
            remark,
            details = lines.Select(line => new
            {
                stockBatchId = line.StockBatchId,
                actualQuantity = line.ActualQuantity,
                remark = line.DetailRemark
            }).ToArray()
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
