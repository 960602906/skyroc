using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Role;
using Application.DTOs.Storage;
using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Purchases;
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
///     T7 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证采购退货出库审核锁定并扣减批次、只追加负向台账、
///     反审核恢复库存；以及销售退货入库（无 AfterSaleId 手工退货）审核增加批次数量、移动加权成本更新、
///     反审核回滚；同时覆盖库存读写权限矩阵与全库无负库存守恒。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class PurchaseReturnAndSalesReturnFlowPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库审核建批次（60 @ 6.0）→采购退货出库草稿（20，草稿态无台账、批次不变）→审核扣减批次到 40
    ///     并写一条负向 PurchaseReturn 台账→重复审核 502→反审核恢复批次到 60 并追加带来源的反向增加流水→
    ///     销售退货入库草稿（25 @ 8.0，无 AfterSaleId）→审核后定位批次写正向 SalesReturn 台账、移动加权成本
    ///     更新为 (40*6+25*8)/(40+25)=6.769...→反审核批次与成本回滚→全库无负库存；
    ///     最小权限（仅读允许、创建/审核/反审核 403）、扩权创建并删除草稿、缩权收口再次 403；
    ///     库存总览/批次/台账读校验；受管商品/单位/仓库/供应商/客户不被改动；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task PurchaseReturn_SalesReturn_FlowBatchSourceConservationAndPermissionMatrix_OnPostgreSql()
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
        var batchNo = $"{batch.Id}-B";
        var inboundRemark = $"{batch.Id}-入库建批次";
        var purchaseReturnRemark = $"{batch.Id}-采购退货出库";
        var salesReturnRemark = $"{batch.Id}-销售退货入库";
        var deniedRemark = $"{batch.Id}-拒绝创建";
        var expandRemark = $"{batch.Id}-扩权草稿";
        var password = "SkyRocReturnFlow!2026";
        var userAgent = $"SkyRoc-T7-Return/{batch.Id}";
        var createName = "T7-ReturnFlow";

        var storageReadPermission = PermissionCodes.Business.Storage.Read;
        var storageCreatePermission = PermissionCodes.Business.Storage.Create;
        var storageUpdatePermission = PermissionCodes.Business.Storage.Update;
        var storageDeletePermission = PermissionCodes.Business.Storage.Delete;

        // 库存样本：基础单位换算率 1
        // 入库 60 @ 6.0  -> 批次 60、单位成本 6.0
        // 采购退货出库 20  -> 批次 40、单位成本仍 6.0（出库不改成本）
        // 反审核采购退货   -> 批次 60、成本 6.0
        // 销售退货入库 25 @ 8.0 -> 批次 65, 移动加权成本 = (60*6 + 25*8)/85 ≈ 6.5882
        // 反审核销售退货   -> 批次 60，成本 6.0
        var inboundQuantity = NumericPrecision.RoundQuantity(60m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(6m);
        var purchaseReturnQuantity = NumericPrecision.RoundQuantity(20m);
        var salesReturnQuantity = NumericPrecision.RoundQuantity(25m);
        var salesReturnUnitPrice = NumericPrecision.RoundMoney(8m);

        var expectedAfterPurchaseReturn = NumericPrecision.RoundQuantity(40m);
        var expectedAfterSalesReturn = NumericPrecision.RoundQuantity(85m); // 60+25（采购退货已反审核后批次回到60）
        // 移动加权：(40*6 + 25*8) / 65 = (240+200)/65 = 440/65
        var expectedCostAfterSalesReturn =
            NumericPrecision.RoundMoney((inboundQuantity * inboundUnitPrice
                                         + salesReturnQuantity * salesReturnUnitPrice)
                                        / expectedAfterSalesReturn);

        Guid adminRoleId;
        Guid managedGoodsId;
        string managedGoodsCode;
        Guid managedGoodsUnitId;
        decimal managedUnitConversion;
        Guid managedWareId;
        Guid managedSupplierId;
        Guid managedCustomerId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var goodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var goodsUnitCode = DemoDataStableKeyCatalog.Create("GOODS-UNIT", 1);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);
            var supplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);
            var customerCode = DemoDataStableKeyCatalog.Create("CUSTOMER", 1);

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

            var supplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == supplierCode);
            Assert.NotNull(supplier);
            managedSupplierId = supplier.Id;

            var customer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == customerCode);
            Assert.NotNull(customer);
            managedCustomerId = customer.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T7 采购退货/销售退货最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T7退货流程操作员",
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
                    NickName = "T7退货流程只读用户",
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
                    Component = "page.t7.return.seed",
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
                    Component = "page.t7.return.write",
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
        Guid? purchaseReturnOrderId = null;
        Guid? salesReturnOrderId = null;
        Guid? createdBatchId = null;
        Guid? expandOrderId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问采购退货出库列表与销售退货入库列表
            using (var anonList1 = await anonymousClient.GetAsync("/api/stock-out/purchase-return/list?current=1&size=20"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonList1, ResponseCode.Unauthorized);
            }

            using (var anonList2 = await anonymousClient.GetAsync("/api/stock-in/sales-return/list?current=1&size=20"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonList2, ResponseCode.Unauthorized);
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

            // 先经其他入库审核建批次（60 @ 6.0），为退货测试准备库存
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

            // ===== 采购退货出库流程 =====
            // 创建采购退货出库草稿（20，从批次扣减）
            StockOutOrderDto purchaseReturnOrder;
            using (var createPrOut = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/purchase-return",
                       BuildPurchaseReturnPayload(managedWareId, managedSupplierId, createdBatchId!.Value,
                           managedGoodsUnitId, purchaseReturnQuantity, inboundUnitPrice, purchaseReturnRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createPrOut.StatusCode);
                purchaseReturnOrder = await ReadApiDataAsync<StockOutOrderDto>(createPrOut);
                Assert.Equal(StockOutOrderType.PurchaseReturn, purchaseReturnOrder.OrderType);
                Assert.Equal(StockDocumentStatus.Draft, purchaseReturnOrder.BusinessStatus);
                Assert.Equal(managedWareId, purchaseReturnOrder.WareId);
                var detail = Assert.Single(purchaseReturnOrder.Details);
                Assert.Equal(managedGoodsId, detail.GoodsId);
                Assert.Equal(managedGoodsCode, detail.GoodsCode);
                Assert.Equal(purchaseReturnQuantity, detail.Quantity);
                Assert.Equal(createdBatchId!.Value, detail.StockBatchId);
                purchaseReturnOrderId = purchaseReturnOrder.Id;
                registry.Register<StockOutOrder>(purchaseReturnOrder.Id, nameof(StockOutOrder.Remark), purchaseReturnRemark);
            }

            // 草稿态不扣批次、无台账
            await using (var beforePrAudit = fixture.CreateDbContext())
            {
                var stockBatch = await beforePrAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(inboundQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);
                Assert.False(await beforePrAudit.StockLedgers.AnyAsync(ledger =>
                    ledger.SourceOrderId == purchaseReturnOrder.Id));
            }

            // 审核采购退货出库：扣减批次到 40，写一条负向 PurchaseReturn 台账
            using (var auditPrOut = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/purchase-return/{purchaseReturnOrder.Id}/audit",
                       new StockOutAuditDto { Remark = $"{purchaseReturnRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditPrOut.StatusCode);
                var audited = await ReadApiDataAsync<StockOutOrderDto>(auditPrOut);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
                Assert.NotNull(audited.AuditTime);
            }

            await using (var afterPrAudit = fixture.CreateDbContext())
            {
                var stockBatch = await afterPrAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(expectedAfterPurchaseReturn, stockBatch.CurrentQuantity);
                Assert.Equal(expectedAfterPurchaseReturn, stockBatch.AvailableQuantity);
                // 采购退货出库不改变移动加权成本
                Assert.Equal(inboundUnitPrice, stockBatch.UnitCost);

                var ledgers = await afterPrAudit.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == purchaseReturnOrder.Id)
                    .ToListAsync();
                var prLedger = Assert.Single(ledgers);
                Assert.Equal(StockLedgerDirection.Decrease, prLedger.Direction);
                Assert.Equal(StockLedgerSourceType.PurchaseReturnOutbound, prLedger.SourceType);
                Assert.Equal(purchaseReturnQuantity, prLedger.ChangeQuantity);
                Assert.Equal(expectedAfterPurchaseReturn, prLedger.BalanceQuantity);
                Assert.Equal(inboundUnitPrice, prLedger.UnitCost);
                Assert.Null(prLedger.ReversedFromLedgerId);
            }

            // 已审核采购退货出库禁止重复审核
            using (var reAuditPr = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/purchase-return/{purchaseReturnOrder.Id}/audit",
                       new StockOutAuditDto { Remark = "重复审核采购退货" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reAuditPr, ResponseCode.DatabaseError);
            }

            // 反审核采购退货出库：批次恢复到 60，追加带来源的反向增加流水
            using (var reversePrOut = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/purchase-return/{purchaseReturnOrder.Id}/reverse-audit",
                       new StockOutAuditDto { Remark = $"{purchaseReturnRemark}-反审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, reversePrOut.StatusCode);
                var reversed = await ReadApiDataAsync<StockOutOrderDto>(reversePrOut);
                Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);
                Assert.NotNull(reversed.ReverseTime);
            }

            await using (var afterPrReverse = fixture.CreateDbContext())
            {
                var stockBatch = await afterPrReverse.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(inboundQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);
                Assert.Equal(inboundUnitPrice, stockBatch.UnitCost);
                Assert.True(stockBatch.CurrentQuantity >= 0m, "反审核后账面数量不得为负");
                Assert.True(stockBatch.AvailableQuantity >= 0m, "反审核后可用数量不得为负");

                var reversalLedger = Assert.Single(await afterPrReverse.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == purchaseReturnOrder.Id
                                     && ledger.Direction == StockLedgerDirection.Increase)
                    .ToListAsync());
                Assert.NotNull(reversalLedger.ReversedFromLedgerId);
                Assert.Equal(purchaseReturnQuantity, reversalLedger.ChangeQuantity);
                Assert.Equal(inboundQuantity, reversalLedger.BalanceQuantity);

                // 采购退货出库单两条流水（一负一反正，只追加）
                var prLedgers = await afterPrReverse.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == purchaseReturnOrder.Id)
                    .ToListAsync();
                Assert.Equal(2, prLedgers.Count);
                Assert.Equal(1, prLedgers.Count(ledger => ledger.Direction == StockLedgerDirection.Decrease));
                Assert.Equal(1, prLedgers.Count(ledger => ledger.Direction == StockLedgerDirection.Increase));
            }

            // 已反审核不能再次反审核
            using (var reReversePr = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/purchase-return/{purchaseReturnOrder.Id}/reverse-audit",
                       new StockOutAuditDto { Remark = "重复反审核采购退货" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reReversePr, ResponseCode.DatabaseError);
            }

            // ===== 销售退货入库流程（无 AfterSaleId，手工退货）=====
            // 创建销售退货入库草稿（25 @ 8.0，不关联售后单）
            StockInOrderDto salesReturnOrder;
            using (var createSrIn = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/sales-return",
                       BuildSalesReturnPayload(managedWareId, managedCustomerId, managedGoodsId, managedGoodsUnitId,
                           batchNo, salesReturnQuantity, salesReturnUnitPrice, salesReturnRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createSrIn.StatusCode);
                salesReturnOrder = await ReadApiDataAsync<StockInOrderDto>(createSrIn);
                Assert.Equal(StockInOrderType.SalesReturn, salesReturnOrder.OrderType);
                Assert.Equal(StockDocumentStatus.Draft, salesReturnOrder.BusinessStatus);
                Assert.Equal(managedWareId, salesReturnOrder.WareId);
                var detail = Assert.Single(salesReturnOrder.Details);
                Assert.Equal(managedGoodsId, detail.GoodsId);
                Assert.Equal(salesReturnQuantity, detail.Quantity);
                salesReturnOrderId = salesReturnOrder.Id;
                registry.Register<StockInOrder>(salesReturnOrder.Id, nameof(StockInOrder.Remark), salesReturnRemark);
            }

            // 草稿态无台账、批次不变（仍为入库后的 60）
            await using (var beforeSrAudit = fixture.CreateDbContext())
            {
                var stockBatch = await beforeSrAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(inboundQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);
                Assert.False(await beforeSrAudit.StockLedgers.AnyAsync(ledger =>
                    ledger.SourceOrderId == salesReturnOrder.Id));
            }

            // 审核销售退货入库：批次增加到 65，移动加权成本更新
            using (var auditSrIn = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}/audit",
                       new StockInAuditDto { Remark = $"{salesReturnRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditSrIn.StatusCode);
                var audited = await ReadApiDataAsync<StockInOrderDto>(auditSrIn);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
                Assert.NotNull(audited.AuditTime);
            }

            await using (var afterSrAudit = fixture.CreateDbContext())
            {
                var stockBatch = await afterSrAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                Assert.Equal(expectedAfterSalesReturn, stockBatch.CurrentQuantity);
                Assert.Equal(expectedAfterSalesReturn, stockBatch.AvailableQuantity);
                // 移动加权：(60*6 + 25*8) / 85 = 440/65
                Assert.Equal(expectedCostAfterSalesReturn, stockBatch.UnitCost);

                var srLedgers = await afterSrAudit.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == salesReturnOrder.Id)
                    .ToListAsync();
                var srLedger = Assert.Single(srLedgers);
                Assert.Equal(StockLedgerDirection.Increase, srLedger.Direction);
                Assert.Equal(StockLedgerSourceType.SalesReturnInbound, srLedger.SourceType);
                Assert.Equal(salesReturnQuantity, srLedger.ChangeQuantity);
                Assert.Equal(expectedAfterSalesReturn, srLedger.BalanceQuantity);
                Assert.Equal(salesReturnUnitPrice, srLedger.UnitCost);
                Assert.Null(srLedger.ReversedFromLedgerId);
            }

            // 已审核销售退货入库禁止重复审核
            using (var reAuditSr = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}/audit",
                       new StockInAuditDto { Remark = "重复审核销售退货" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reAuditSr, ResponseCode.DatabaseError);
            }

            // 反审核销售退货入库：批次回滚到入库后的 60，成本回滚到 6.0
            using (var reverseSrIn = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}/reverse-audit",
                       new StockInAuditDto { Remark = $"{salesReturnRemark}-反审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, reverseSrIn.StatusCode);
                var reversed = await ReadApiDataAsync<StockInOrderDto>(reverseSrIn);
                Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);
                Assert.NotNull(reversed.ReverseTime);
            }

            await using (var afterSrReverse = fixture.CreateDbContext())
            {
                var stockBatch = await afterSrReverse.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId!.Value);
                // 反审核后批次回到入库基准（60），成本恢复 6.0
                Assert.Equal(inboundQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);
                Assert.Equal(inboundUnitPrice, stockBatch.UnitCost);
                Assert.True(stockBatch.CurrentQuantity >= 0m, "销售退货反审核后账面数量不得为负");
                Assert.True(stockBatch.AvailableQuantity >= 0m, "销售退货反审核后可用数量不得为负");

                // 销售退货入库两条流水（一正一反负，只追加）
                var srAllLedgers = await afterSrReverse.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == salesReturnOrder.Id)
                    .ToListAsync();
                Assert.Equal(2, srAllLedgers.Count);
                Assert.Equal(1, srAllLedgers.Count(ledger => ledger.Direction == StockLedgerDirection.Increase
                                                              && ledger.ReversedFromLedgerId == null));
                var reversalRow = Assert.Single(srAllLedgers,
                    ledger => ledger.ReversedFromLedgerId != null);
                Assert.Equal(StockLedgerDirection.Decrease, reversalRow.Direction);
                Assert.Equal(inboundQuantity, reversalRow.BalanceQuantity);
            }

            // 已反审核销售退货不能再次反审核
            using (var reReverseSr = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}/reverse-audit",
                       new StockInAuditDto { Remark = "重复反审核销售退货" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reReverseSr, ResponseCode.DatabaseError);
            }

            // ===== 库存总览/批次/台账读接口核对 =====
            using (var overview = await adminClient.GetAsync(
                       $"/api/stock/overview?current=1&size=20&wareId={managedWareId}&goodsId={managedGoodsId}"))
            {
                Assert.Equal(HttpStatusCode.OK, overview.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockOverviewDto>>(overview);
                // 总览按仓库+商品+基础单位聚合所有批次（含既有受管数据），仅核对守恒关系
                Assert.True(page.Records!.Any(item =>
                    item.WareId == managedWareId && item.GoodsId == managedGoodsId && item.BaseUnitId == managedGoodsUnitId),
                    "总览应包含本轮创建批次对应的仓库/商品/基础单位行");
                var row = page.Records!.First(item =>
                    item.WareId == managedWareId && item.GoodsId == managedGoodsId && item.BaseUnitId == managedGoodsUnitId);
                Assert.True(row.CurrentQuantity >= 0m, "总览账面数量不得为负");
                Assert.True(row.AvailableQuantity >= 0m, "总览可用数量不得为负");
                Assert.Equal(row.CurrentQuantity - row.AvailableQuantity, row.OccupiedQuantity);
            }

            using (var batches = await adminClient.GetAsync(
                       $"/api/stock/batches?current=1&size=20&goodsId={managedGoodsId}&batchNo={Uri.EscapeDataString(batchNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, batches.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockBatchDto>>(batches);
                var row = Assert.Single(page.Records!, item => item.Id == createdBatchId!.Value);
                // 经过采购退货+反审核和销售退货+反审核，最终批次应回到入库值
                Assert.Equal(inboundQuantity, row.CurrentQuantity);
                Assert.Equal(inboundUnitPrice, row.UnitCost);
            }

            using (var ledgersResponse = await adminClient.GetAsync(
                       $"/api/stock/ledgers?current=1&size=50&stockBatchId={createdBatchId!.Value}"))
            {
                Assert.Equal(HttpStatusCode.OK, ledgersResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<StockLedgerDto>>(ledgersResponse);
                // 本批次应有 4 条台账（采购入库 1 + 采购退货出库 2 + 销售退货入库 2 = 5，但入库 1 属于入库单来源）
                // 实际：入库 1 正、采购退货 1 负+1 正、销售退货 1 正+1 负 = 5 条
                Assert.True(page.Records!.Count >= 5,
                    "本批次台账应至少 5 条（入库+采购退货两条+销售退货两条）");
                Assert.True(page.Records!.All(item => item.SignedChangeQuantity != 0m),
                    "所有台账带方向数量不应为零");
            }

            // ===== 权限矩阵 =====
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
                Assert.Contains(storageReadPermission, info.Permissions);
                Assert.DoesNotContain(storageCreatePermission, info.Permissions);
                Assert.DoesNotContain(storageUpdatePermission, info.Permissions);
            }

            // 最小权限：采购退货出库列表与销售退货入库列表允许（读）
            using (var allowedPrList = await limitedClient.GetAsync("/api/stock-out/purchase-return/list?current=1&size=20"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedPrList.StatusCode);
            }

            using (var allowedSrList = await limitedClient.GetAsync("/api/stock-in/sales-return/list?current=1&size=20"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedSrList.StatusCode);
            }

            // 最小权限：创建采购退货出库拒绝（403）
            using (var deniedPrCreate = await limitedClient.PostAsJsonAsync(
                       "/api/stock-out/purchase-return",
                       BuildPurchaseReturnPayload(managedWareId, managedSupplierId, createdBatchId!.Value,
                           managedGoodsUnitId, purchaseReturnQuantity, inboundUnitPrice, deniedRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedPrCreate, ResponseCode.Forbidden);
            }

            // 最小权限：创建销售退货入库拒绝（403）
            using (var deniedSrCreate = await limitedClient.PostAsJsonAsync(
                       "/api/stock-in/sales-return",
                       BuildSalesReturnPayload(managedWareId, managedCustomerId, managedGoodsId, managedGoodsUnitId,
                           batchNo, salesReturnQuantity, salesReturnUnitPrice, deniedRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedSrCreate, ResponseCode.Forbidden);
            }

            // 被拒后无残留业务数据
            await using (var afterDenied = fixture.CreateDbContext())
            {
                Assert.False(await afterDenied.StockOutOrders.AnyAsync(item => item.Remark == deniedRemark));
                Assert.False(await afterDenied.StockInOrders.AnyAsync(item => item.Remark == deniedRemark));
            }

            // 扩权：分配写权限菜单后重新登录，可创建退货草稿并删除
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

            StockOutOrderDto expandPrOrder;
            using (var createExpand = await limitedWriteClient.PostAsJsonAsync(
                       "/api/stock-out/purchase-return",
                       BuildPurchaseReturnPayload(managedWareId, managedSupplierId, createdBatchId!.Value,
                           managedGoodsUnitId, NumericPrecision.RoundQuantity(1m), inboundUnitPrice, expandRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createExpand.StatusCode);
                expandPrOrder = await ReadApiDataAsync<StockOutOrderDto>(createExpand);
                expandOrderId = expandPrOrder.Id;
                registry.Register<StockOutOrder>(expandPrOrder.Id, nameof(StockOutOrder.Remark), expandRemark);
            }

            using (var deleteExpand = await limitedWriteClient.DeleteAsync(
                       $"/api/stock-out/purchase-return/{expandPrOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpand.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteExpand));
            }

            await using (var afterDeleteExpand = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteExpand.StockOutOrders.AnyAsync(item => item.Id == expandPrOrder.Id));
                expandOrderId = null;
            }

            // 缩权后写权限与写菜单路由收口
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
                       "/api/stock-out/purchase-return",
                       BuildPurchaseReturnPayload(managedWareId, managedSupplierId, createdBatchId!.Value,
                           managedGoodsUnitId, NumericPrecision.RoundQuantity(1m), inboundUnitPrice,
                           $"{batch.Id}-缩权拒绝")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAfterShrink, ResponseCode.Forbidden);
            }

            // 全库无负库存批次
            await using (var finalCheck = fixture.CreateDbContext())
            {
                Assert.False(await finalCheck.StockBatches.AnyAsync(item =>
                    item.CurrentQuantity < 0m || item.AvailableQuantity < 0m));
            }

            await using var auditCtx = fixture.CreateDbContext();
            var loginLogs = await auditCtx.LoginLogs.AsNoTracking()
                .Where(log => log.Username == adminUsername || log.Username == limitedUsername)
                .ToListAsync();
            Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
            Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
            Assert.All(loginLogs, log =>
                Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal));
            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            Assert.True(await auditCtx.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await auditCtx.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await auditCtx.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));
            Assert.True(await auditCtx.Wares.AnyAsync(item => item.Id == managedWareId));
            Assert.True(await auditCtx.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await auditCtx.Customers.AnyAsync(item => item.Id == managedCustomerId));
        }


        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualStockOutRemarks = new[]
                {
                    purchaseReturnRemark,
                    expandRemark,
                    deniedRemark
                };
                var residualStockOutIds = new List<Guid>();
                if (purchaseReturnOrderId.HasValue) residualStockOutIds.Add(purchaseReturnOrderId.Value);
                if (expandOrderId.HasValue) residualStockOutIds.Add(expandOrderId.Value);

                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => residualStockOutIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (residualStockOutRemarks.Contains(item.Remark)
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualStockInRemarks = new[]
                {
                    inboundRemark,
                    salesReturnRemark,
                    deniedRemark
                };
                var residualStockInIds = new List<Guid>();
                if (inboundOrderId.HasValue) residualStockInIds.Add(inboundOrderId.Value);
                if (salesReturnOrderId.HasValue) residualStockInIds.Add(salesReturnOrderId.Value);

                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => residualStockInIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (residualStockInRemarks.Contains(item.Remark)
                                           || item.Remark.StartsWith(batch.Id))))
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
                    seedReadButtonId, writeCreateButtonId, writeUpdateButtonId, writeDeleteButtonId
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
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
            Assert.True(await residualContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await residualContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
        }
    }


    private static object BuildCreateOtherInPayload(
        Guid wareId, Guid goodsId, Guid goodsUnitId, string batchNo,
        decimal quantity, decimal unitPrice, string remark)
    {
        return new
        {
            wareId,
            inTime = "2026-07-20T09:00:00Z",
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
                    remark = "T7退货流程-其他入库明细"
                }
            }
        };
    }

    private static object BuildPurchaseReturnPayload(
        Guid wareId, Guid supplierId, Guid stockBatchId, Guid goodsUnitId,
        decimal quantity, decimal unitPrice, string remark)
    {
        return new
        {
            wareId,
            supplierId,
            outTime = "2026-07-20T10:00:00Z",
            remark,
            details = new[]
            {
                new
                {
                    stockBatchId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    remark = "T7采购退货出库明细"
                }
            }
        };
    }

    private static object BuildSalesReturnPayload(
        Guid wareId, Guid customerId, Guid goodsId, Guid goodsUnitId,
        string batchNo, decimal quantity, decimal unitPrice, string remark)
    {
        return new
        {
            wareId,
            customerId,
            inTime = "2026-07-20T11:00:00Z",
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
                    remark = "T7销售退货入库明细"
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
        PostgreSqlTestFixture fixture, BatchCleanupRegistry registry, params string[] usernames)
    {
        await using var context = fixture.CreateDbContext();
        var nameSet = usernames.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
        if (nameSet.Length == 0) return;

        var residualLogs = await context.LoginLogs.AsNoTracking()
            .Where(log => log.Username != null && nameSet.Contains(log.Username))
            .ToListAsync();
        RegisterLoginLogs(registry, residualLogs);
    }

    private static async Task RegisterBatchOperationLogsAsync(
        PostgreSqlTestFixture fixture, BatchCleanupRegistry registry, params string[] usernames)
    {
        await using var context = fixture.CreateDbContext();
        var nameSet = usernames.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
        if (nameSet.Length == 0) return;

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
