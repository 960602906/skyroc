using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.AfterSales;
using Application.DTOs.Auth;
using Application.DTOs.Orders;
using Application.DTOs.Storage;
using Domain.Entities;
using Domain.Entities.AfterSales;
using Domain.Entities.Orders;
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

namespace SkyRoc.Tests.AfterSales;

/// <summary>
///     T9 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证售后取货完成后的
///     销售退货入库、库存回补、创建防重与完成后反审核拒绝。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class SalesReturnInboundReplenishPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库→销售订单审核→退货退款审核生成取货→未完成取货拒绝入库→
    ///     分配/开始/完成取货→未入库拒绝完成售后→创建销售退货入库草稿（幂等复用、分歧请求拒绝）→
    ///     审核回补批次与台账→重复审核拒绝→完成售后→完成后反审核拒绝→401/403 权限矩阵；
    ///     临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task SalesReturn_InboundReplenishIdempotencyAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedReadButtonId = Guid.NewGuid();

        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var inboundBatchNo = $"{batch.Id}-IN";
        var returnBatchNo = $"{batch.Id}-SR";
        var inboundRemark = $"{batch.Id}-入库建批次";
        var salesReturnRemark = $"{batch.Id}-售后退货入库";
        var saleOrderInnerRemark = $"{batch.Id}O";
        var returnRemark = $"{batch.Id}-退货入库回补";
        var source = batch.Id;
        var password = "SkyRocSalesReturnIn!2026";
        var userAgent = $"SkyRoc-T9-SalesReturnIn/{batch.Id}";
        var createName = "T9-SalesReturnInbound";
        var contactName = "退货入库王老师";
        var contactPhone = "13900009951";
        var pickupAddress = $"{batch.Id}-上海市浦东新区退货入库路 51 号食堂后门";
        var deliveryAddress = "上海市浦东新区退货联调路 51 号食堂西门";
        var plannedPickupTime = new DateTime(2026, 7, 20, 11, 0, 0, DateTimeKind.Utc);

        var storageReadPermission = PermissionCodes.Business.Storage.Read;
        var storageCreatePermission = PermissionCodes.Business.Storage.Create;
        var storageUpdatePermission = PermissionCodes.Business.Storage.Update;

        var inboundQuantity = NumericPrecision.RoundQuantity(20m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(5m);
        var saleQuantity = NumericPrecision.RoundQuantity(10m);
        var saleUnitPrice = NumericPrecision.RoundMoney(8m);
        var refundQuantity = NumericPrecision.RoundQuantity(2m);

        Guid adminRoleId;
        Guid managedCustomerId;
        Guid managedGoodsId;
        Guid managedGoodsUnitId;
        Guid managedWareId;
        Guid managedDriverId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var customerCode = DemoDataStableKeyCatalog.Create("CUSTOMER", 1);
            var goodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var goodsUnitCode = DemoDataStableKeyCatalog.Create("GOODS-UNIT", 1);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);
            var driverCode = DemoDataStableKeyCatalog.Create("DRIVER", 1);

            var customer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == customerCode);
            Assert.NotNull(customer);
            managedCustomerId = customer.Id;

            var goods = await seedContext.Set<GoodsEntity>().AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsCode);
            Assert.NotNull(goods);
            managedGoodsId = goods.Id;

            var goodsUnit = await seedContext.GoodsUnits.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsUnitCode);
            Assert.NotNull(goodsUnit);
            Assert.Equal(goods.Id, goodsUnit.GoodsId);
            managedGoodsUnitId = goodsUnit.Id;
            Assert.Equal(1m, goodsUnit.ConversionRate);

            var ware = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == wareCode);
            Assert.NotNull(ware);
            managedWareId = ware.Id;

            var driver = await seedContext.Drivers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == driverCode);
            Assert.NotNull(driver);
            Assert.Equal(Status.Enable, driver.Status);
            managedDriverId = driver.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T9 退货入库最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T9退货入库操作员",
                    Gender = GenderType.Male,
                    Phone = "13900009961",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T9库存只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900009962",
                    Email = $"{batch.Id}-l@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.UserRoles.AddRangeAsync(
                new UserRole { UserId = adminUserId, RoleId = adminRoleId },
                new UserRole { UserId = limitedUserId, RoleId = limitedRoleId });

            await seedContext.Menus.AddAsync(new Menu
            {
                Id = seedMenuId,
                Name = seedMenuName,
                Path = $"/{batch.Id}s",
                Title = "T9退货入库只读菜单",
                Component = "page.t9.sales.return.inbound.seed",
                MenuType = MenuType.Menu,
                Order = 9943,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedReadButtonId,
                Code = storageReadPermission,
                Desc = "T9 库存读取权限按钮",
                MenuId = seedMenuId,
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

        Guid? inboundOrderId = null;
        Guid? saleOrderId = null;
        Guid? returnAfterSaleId = null;
        Guid? pickupTaskId = null;
        Guid? salesReturnOrderId = null;
        Guid? inboundBatchId = null;
        Guid? returnBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousList = await anonymousClient.GetAsync("/api/stock-in/sales-return/list?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

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

            StockInOrderDto inboundOrder;
            using (var createInbound = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherInPayload(
                           managedWareId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           inboundBatchNo,
                           inboundQuantity,
                           inboundUnitPrice,
                           inboundRemark)))
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
                                         && item.BatchNo == inboundBatchNo);
                inboundBatchId = stockBatch.Id;
                Assert.Equal(inboundQuantity, stockBatch.CurrentQuantity);
            }

            SaleOrderDto saleOrder;
            using (var createOrder = await adminClient.PostAsJsonAsync(
                       "/api/orders",
                       BuildCreateSaleOrderPayload(
                           managedCustomerId,
                           managedWareId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           saleQuantity,
                           saleUnitPrice,
                           contactName,
                           contactPhone,
                           deliveryAddress,
                           "T9退货入库销售订单",
                           saleOrderInnerRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createOrder.StatusCode);
                saleOrder = await ReadApiDataAsync<SaleOrderDto>(createOrder);
                saleOrderId = saleOrder.Id;
                registry.Register<SaleOrder>(saleOrder.Id, nameof(SaleOrder.InnerRemark), saleOrderInnerRemark);
            }

            var saleOrderDetail = Assert.Single(saleOrder.Details);

            using (var approveOrder = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{saleOrder.Id}/approve",
                       new SaleOrderAuditDto { Remark = $"{saleOrderInnerRemark}-审核通过" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveOrder.StatusCode);
                var approved = await ReadApiDataAsync<SaleOrderDto>(approveOrder);
                Assert.Equal(SaleOrderStatus.SortingPending, approved.OrderStatus);
            }

            AfterSaleDto returnDraft;
            using (var createReturn = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetail.Id,
                           refundQuantity,
                           AfterSaleType.ReturnAndRefund,
                           AfterSaleHandleType.GoodsDiscount,
                           source,
                           contactName,
                           contactPhone,
                           pickupAddress,
                           returnRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createReturn.StatusCode);
                returnDraft = await ReadApiDataAsync<AfterSaleDto>(createReturn);
                returnAfterSaleId = returnDraft.Id;
                registry.Register<AfterSale>(returnDraft.Id, nameof(AfterSale.Remark), returnRemark);
            }

            var afterSaleGoods = Assert.Single(returnDraft.Goods);
            var refundUnitPrice = NumericPrecision.RoundMoney(afterSaleGoods.UnitPrice);
            Assert.Equal(saleUnitPrice, refundUnitPrice);

            using (var submitReturn = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{returnDraft.Id}/submit",
                       new AfterSaleActionDto { Remark = $"{returnRemark}-提交" }))
            {
                Assert.Equal(HttpStatusCode.OK, submitReturn.StatusCode);
            }

            AfterSaleDto approvedReturn;
            using (var approveReturn = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{returnDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{returnRemark}-同意退货" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveReturn.StatusCode);
                approvedReturn = await ReadApiDataAsync<AfterSaleDto>(approveReturn);
                Assert.Equal(AfterSaleStatus.ReturnPending, approvedReturn.AfterStatus);
                var createdPickup = Assert.Single(approvedReturn.PickupTasks);
                Assert.Equal(PickupTaskStatus.PendingAssign, createdPickup.PickupStatus);
                pickupTaskId = createdPickup.Id;
                registry.Register<PickupTask>(createdPickup.Id, nameof(PickupTask.PickupAddressSnapshot), pickupAddress);
            }

            var taskId = pickupTaskId.Value;

            using (var rejectBeforeComplete = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/sales-return",
                       BuildCreateSalesReturnPayload(
                           returnDraft.Id,
                           managedWareId,
                           managedCustomerId,
                           taskId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           refundQuantity,
                           refundUnitPrice,
                           returnBatchNo,
                           $"{salesReturnRemark}-未完成取货")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectBeforeComplete, ResponseCode.DatabaseError);
            }

            using (var assign = await adminClient.PutAsJsonAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/assign",
                       new AssignPickupTaskDto
                       {
                           DriverId = managedDriverId,
                           PlannedPickupTime = plannedPickupTime,
                           Remark = $"{returnRemark}-分配司机"
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
                var assigned = await ReadApiDataAsync<PickupTaskDto>(assign);
                Assert.Equal(PickupTaskStatus.PendingPickup, assigned.PickupStatus);
            }

            using (var start = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/start",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, start.StatusCode);
                var started = await ReadApiDataAsync<PickupTaskDto>(start);
                Assert.Equal(PickupTaskStatus.PickingUp, started.PickupStatus);
            }

            using (var completePickup = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/complete",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, completePickup.StatusCode);
                var completedPickup = await ReadApiDataAsync<PickupTaskDto>(completePickup);
                Assert.Equal(PickupTaskStatus.Completed, completedPickup.PickupStatus);
                Assert.Null(completedPickup.StockInOrderId);
            }

            using (var rejectCompleteBeforeInbound = await adminClient.PostAsync(
                       $"/api/after-sales/{returnDraft.Id}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectCompleteBeforeInbound, ResponseCode.DatabaseError);
            }

            var salesReturnRequest = BuildCreateSalesReturnPayload(
                returnDraft.Id,
                managedWareId,
                managedCustomerId,
                taskId,
                managedGoodsId,
                managedGoodsUnitId,
                refundQuantity,
                refundUnitPrice,
                returnBatchNo,
                salesReturnRemark);

            StockInOrderDto salesReturnOrder;
            using (var createSalesReturn = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/sales-return",
                       salesReturnRequest))
            {
                Assert.Equal(HttpStatusCode.OK, createSalesReturn.StatusCode);
                salesReturnOrder = await ReadApiDataAsync<StockInOrderDto>(createSalesReturn);
                Assert.Equal(StockInOrderType.SalesReturn, salesReturnOrder.OrderType);
                Assert.Equal(StockDocumentStatus.Draft, salesReturnOrder.BusinessStatus);
                Assert.Equal(returnDraft.Id, salesReturnOrder.AfterSaleId);
                var detail = Assert.Single(salesReturnOrder.Details);
                Assert.Equal(taskId, detail.PickupTaskId);
                Assert.Equal(refundQuantity, detail.Quantity);
                Assert.Equal(refundUnitPrice, detail.UnitPrice);
                Assert.Equal(returnBatchNo, detail.BatchNo);
                salesReturnOrderId = salesReturnOrder.Id;
                registry.Register<StockInOrder>(salesReturnOrder.Id, nameof(StockInOrder.Remark), salesReturnRemark);
            }

            await using (var beforeAudit = fixture.CreateDbContext())
            {
                Assert.False(await beforeAudit.StockBatches.AnyAsync(item => item.BatchNo == returnBatchNo));
                Assert.False(await beforeAudit.StockLedgers.AnyAsync(ledger =>
                    ledger.SourceOrderId == salesReturnOrder.Id));
                Assert.Equal(inboundQuantity, await beforeAudit.StockBatches.AsNoTracking()
                    .Where(item => item.Id == inboundBatchId)
                    .Select(item => item.CurrentQuantity)
                    .SingleAsync());
            }

            using (var idempotentCreate = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/sales-return",
                       salesReturnRequest))
            {
                Assert.Equal(HttpStatusCode.OK, idempotentCreate.StatusCode);
                var repeated = await ReadApiDataAsync<StockInOrderDto>(idempotentCreate);
                Assert.Equal(salesReturnOrder.Id, repeated.Id);
                Assert.Equal(StockDocumentStatus.Draft, repeated.BusinessStatus);
            }

            using (var divergentCreate = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/sales-return",
                       BuildCreateSalesReturnPayload(
                           returnDraft.Id,
                           managedWareId,
                           managedCustomerId,
                           taskId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           refundQuantity,
                           refundUnitPrice,
                           $"{returnBatchNo}-CHANGED",
                           salesReturnRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(divergentCreate, ResponseCode.DatabaseError);
            }

            await using (var afterDivergent = fixture.CreateDbContext())
            {
                Assert.Equal(1, await afterDivergent.StockInOrders.CountAsync(item =>
                    item.AfterSaleId == returnDraft.Id));
                Assert.False(await afterDivergent.StockBatches.AnyAsync(item =>
                    item.BatchNo == $"{returnBatchNo}-CHANGED"));
            }

            using (var auditSalesReturn = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}/audit",
                       new StockInAuditDto { Remark = $"{salesReturnRemark}-审核入库" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditSalesReturn.StatusCode);
                var audited = await ReadApiDataAsync<StockInOrderDto>(auditSalesReturn);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
                Assert.NotNull(audited.AuditTime);
            }

            await using (var afterAudit = fixture.CreateDbContext())
            {
                var returnBatch = await afterAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == returnBatchNo);
                returnBatchId = returnBatch.Id;
                Assert.Equal(refundQuantity, returnBatch.CurrentQuantity);
                Assert.Equal(refundQuantity, returnBatch.AvailableQuantity);
                Assert.Equal(refundUnitPrice, returnBatch.UnitCost);
                Assert.True(returnBatch.CurrentQuantity >= 0m);
                Assert.True(returnBatch.AvailableQuantity >= 0m);

                var inboundBatch = await afterAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == inboundBatchId);
                Assert.Equal(inboundQuantity, inboundBatch.CurrentQuantity);

                var ledger = Assert.Single(await afterAudit.StockLedgers.AsNoTracking()
                    .Where(item => item.SourceOrderId == salesReturnOrder.Id)
                    .ToListAsync());
                Assert.Equal(StockLedgerDirection.Increase, ledger.Direction);
                Assert.Equal(StockLedgerSourceType.SalesReturnInbound, ledger.SourceType);
                Assert.Equal(refundQuantity, ledger.ChangeQuantity);
                Assert.Equal(refundQuantity, ledger.BalanceQuantity);
                Assert.Equal(refundUnitPrice, ledger.UnitCost);
                Assert.Equal(returnBatch.Id, ledger.StockBatchId);
                Assert.Null(ledger.ReversedFromLedgerId);

                var linkedTask = await afterAudit.PickupTasks.AsNoTracking()
                    .Include(item => item.StockInDetail)
                    .SingleAsync(item => item.Id == taskId);
                Assert.NotNull(linkedTask.StockInDetail);
                Assert.Equal(salesReturnOrder.Id, linkedTask.StockInDetail.StockInOrderId);
            }

            using (var reAudit = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}/audit",
                       new StockInAuditDto { Remark = "重复审核销售退货入库" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reAudit, ResponseCode.DatabaseError);
            }

            AfterSaleDto completedAfterSale;
            using (var completeAfterSale = await adminClient.PostAsync(
                       $"/api/after-sales/{returnDraft.Id}/complete",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, completeAfterSale.StatusCode);
                completedAfterSale = await ReadApiDataAsync<AfterSaleDto>(completeAfterSale);
                Assert.Equal(AfterSaleStatus.Completed, completedAfterSale.AfterStatus);
            }

            using (var reverseAfterComplete = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}/reverse-audit",
                       new StockInAuditDto { Remark = "售后已完成仍反审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reverseAfterComplete, ResponseCode.DatabaseError);
            }

            await using (var afterReverseReject = fixture.CreateDbContext())
            {
                var persistedOrder = await afterReverseReject.StockInOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == salesReturnOrder.Id);
                Assert.Equal(StockDocumentStatus.Audited, persistedOrder.BusinessStatus);

                var returnBatch = await afterReverseReject.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == returnBatchId);
                Assert.Equal(refundQuantity, returnBatch.CurrentQuantity);

                Assert.Equal(1, await afterReverseReject.StockLedgers.CountAsync(item =>
                    item.SourceOrderId == salesReturnOrder.Id));

                var persistedAfterSale = await afterReverseReject.AfterSales.AsNoTracking()
                    .SingleAsync(item => item.Id == returnDraft.Id);
                Assert.Equal(AfterSaleStatus.Completed, persistedAfterSale.AfterStatus);
            }

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
                var info = await ReadApiDataAsync<UserInfoDto>(limitedInfo);
                Assert.Contains(storageReadPermission, info.Permissions);
                Assert.DoesNotContain(storageCreatePermission, info.Permissions);
                Assert.DoesNotContain(storageUpdatePermission, info.Permissions);
            }

            using (var allowedList = await limitedClient.GetAsync("/api/stock-in/sales-return/list?current=1&size=20"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
                var detail = await ReadApiDataAsync<StockInOrderDto>(allowedDetail);
                Assert.Equal(StockDocumentStatus.Audited, detail.BusinessStatus);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/stock-in/sales-return",
                       BuildCreateSalesReturnPayload(
                           returnDraft.Id,
                           managedWareId,
                           managedCustomerId,
                           taskId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           refundQuantity,
                           refundUnitPrice,
                           returnBatchNo,
                           $"{salesReturnRemark}-越权创建")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedAudit = await limitedClient.PostAsJsonAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}/audit",
                       new StockInAuditDto { Remark = "越权重复审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAudit, ResponseCode.Forbidden);
            }

            using (var deniedReverse = await limitedClient.PostAsJsonAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}/reverse-audit",
                       new StockInAuditDto { Remark = "越权反审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedReverse, ResponseCode.Forbidden);
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
            Assert.True(await auditContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
            Assert.True(await auditContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await auditContext.Wares.AnyAsync(item => item.Id == managedWareId));
            Assert.True(await auditContext.Drivers.AnyAsync(item =>
                item.Id == managedDriverId && item.Code == DemoDataStableKeyCatalog.Create("DRIVER", 1)));
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualStockInIds = new List<Guid>();
                if (inboundOrderId.HasValue)
                    residualStockInIds.Add(inboundOrderId.Value);
                if (salesReturnOrderId.HasValue)
                    residualStockInIds.Add(salesReturnOrderId.Value);

                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => residualStockInIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == inboundRemark
                                           || item.Remark == salesReturnRemark
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();
                var residualOrderIdSet = residualStockInOrders.Select(item => item.Id).ToHashSet();

                var residualLedgers = await cleanupContext.StockLedgers
                    .Where(ledger => residualOrderIdSet.Contains(ledger.SourceOrderId)
                                     || (inboundBatchId.HasValue && ledger.StockBatchId == inboundBatchId.Value)
                                     || (returnBatchId.HasValue && ledger.StockBatchId == returnBatchId.Value)
                                     || ledger.BatchNoSnapshot == inboundBatchNo
                                     || ledger.BatchNoSnapshot == returnBatchNo)
                    .ToListAsync();
                if (residualLedgers.Count > 0)
                {
                    cleanupContext.StockLedgers.RemoveRange(residualLedgers);
                    await cleanupContext.SaveChangesAsync();
                }

                if (residualStockInOrders.Count > 0)
                {
                    cleanupContext.StockInOrders.RemoveRange(residualStockInOrders);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualAfterSaleIds = new List<Guid>();
                if (returnAfterSaleId.HasValue)
                    residualAfterSaleIds.Add(returnAfterSaleId.Value);

                var residualPickupTaskIds = new List<Guid>();
                if (pickupTaskId.HasValue)
                    residualPickupTaskIds.Add(pickupTaskId.Value);

                var residualPickupTasks = await cleanupContext.PickupTasks
                    .Where(item => residualPickupTaskIds.Contains(item.Id)
                                   || residualAfterSaleIds.Contains(item.AfterSaleId)
                                   || item.PickupAddressSnapshot == pickupAddress
                                   || item.PickupAddressSnapshot.StartsWith(batch.Id))
                    .ToListAsync();
                if (residualPickupTasks.Count > 0)
                {
                    cleanupContext.PickupTasks.RemoveRange(residualPickupTasks);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualAfterSales = await cleanupContext.AfterSales
                    .Where(item => residualAfterSaleIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark.StartsWith(batch.Id) || item.Source == source)))
                    .ToListAsync();
                if (residualAfterSales.Count > 0)
                {
                    cleanupContext.AfterSales.RemoveRange(residualAfterSales);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualBatches = await cleanupContext.StockBatches
                    .Where(item => item.BatchNo == inboundBatchNo
                                   || item.BatchNo == returnBatchNo
                                   || (inboundBatchId.HasValue && item.Id == inboundBatchId.Value)
                                   || (returnBatchId.HasValue && item.Id == returnBatchId.Value))
                    .ToListAsync();
                if (residualBatches.Count > 0)
                {
                    cleanupContext.StockBatches.RemoveRange(residualBatches);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualSaleOrderIds = new List<Guid>();
                if (saleOrderId.HasValue)
                    residualSaleOrderIds.Add(saleOrderId.Value);
                var residualSaleOrders = await cleanupContext.SaleOrders
                    .Where(item => residualSaleOrderIds.Contains(item.Id)
                                   || item.InnerRemark == saleOrderInnerRemark
                                   || (item.InnerRemark != null && item.InnerRemark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualSaleOrders.Count > 0)
                {
                    cleanupContext.SaleOrders.RemoveRange(residualSaleOrders);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };
                var residualUsers = await cleanupContext.Users
                    .Where(user => residualUserIds.Contains(user.Id)
                                   || user.Username == adminUsername
                                   || user.Username == limitedUsername
                                   || (user.Username != null && user.Username.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualUsers.Count > 0)
                {
                    var residualUserIdSet = residualUsers.Select(user => user.Id).ToHashSet();
                    var residualUserRoles = await cleanupContext.UserRoles
                        .Where(item => residualUserIdSet.Contains(item.UserId))
                        .ToListAsync();
                    if (residualUserRoles.Count > 0)
                    {
                        cleanupContext.UserRoles.RemoveRange(residualUserRoles);
                        await cleanupContext.SaveChangesAsync();
                    }

                    cleanupContext.Users.RemoveRange(residualUsers);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoleMenus = await cleanupContext.RoleMenus
                    .Where(item => item.RoleId == limitedRoleId || item.MenuId == seedMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => button.Id == seedReadButtonId
                                     || button.MenuId == seedMenuId
                                     || button.CreateName == createName)
                    .ToListAsync();
                if (residualButtons.Count > 0)
                {
                    cleanupContext.MenuButtons.RemoveRange(residualButtons);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualMenus = await cleanupContext.Menus
                    .Where(menu => menu.Id == seedMenuId || menu.Name == seedMenuName)
                    .ToListAsync();
                if (residualMenus.Count > 0)
                {
                    cleanupContext.Menus.RemoveRange(residualMenus);
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
            }

            await fixture.CleanupBatchAsync(registry);

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.PickupTasks.AnyAsync(item =>
                (pickupTaskId.HasValue && item.Id == pickupTaskId.Value)
                || item.PickupAddressSnapshot == pickupAddress
                || item.PickupAddressSnapshot.StartsWith(batch.Id)));
            Assert.False(await residualContext.AfterSales.AnyAsync(item =>
                (returnAfterSaleId.HasValue && item.Id == returnAfterSaleId.Value)
                || (item.Remark != null && item.Remark.StartsWith(batch.Id))
                || item.Source == source));
            Assert.False(await residualContext.SaleOrders.AnyAsync(item =>
                (saleOrderId.HasValue && item.Id == saleOrderId.Value)
                || item.InnerRemark == saleOrderInnerRemark
                || (item.InnerRemark != null && item.InnerRemark.StartsWith(batch.Id))));
            Assert.False(await residualContext.StockInOrders.AnyAsync(item =>
                (inboundOrderId.HasValue && item.Id == inboundOrderId.Value)
                || (salesReturnOrderId.HasValue && item.Id == salesReturnOrderId.Value)
                || (item.Remark != null
                    && (item.Remark == inboundRemark
                        || item.Remark == salesReturnRemark
                        || item.Remark.StartsWith(batch.Id)))));
            Assert.False(await residualContext.StockBatches.AnyAsync(item =>
                item.BatchNo == inboundBatchNo
                || item.BatchNo == returnBatchNo
                || (inboundBatchId.HasValue && item.Id == inboundBatchId.Value)
                || (returnBatchId.HasValue && item.Id == returnBatchId.Value)));
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Id == adminUserId
                || user.Id == limitedUserId
                || user.Username == adminUsername
                || user.Username == limitedUsername
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId
                || role.Code == limitedRoleCode
                || (role.Code != null && role.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Id == seedMenuId || menu.Name == seedMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedReadButtonId || button.CreateName == createName));
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
            Assert.True(await residualContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
            Assert.True(await residualContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
            Assert.True(await residualContext.Drivers.AnyAsync(item =>
                item.Id == managedDriverId && item.Code == DemoDataStableKeyCatalog.Create("DRIVER", 1)));
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
                    remark = "T9退货入库其他入库明细"
                }
            }
        };
    }

    private static object BuildCreateSaleOrderPayload(
        Guid customerId,
        Guid wareId,
        Guid goodsId,
        Guid goodsUnitId,
        decimal quantity,
        decimal unitPrice,
        string contactName,
        string contactPhone,
        string deliveryAddress,
        string remark,
        string innerRemark)
    {
        return new
        {
            customerId,
            wareId,
            orderDate = "2026-07-20T08:00:00Z",
            receiveDate = "2026-07-21T06:00:00Z",
            contactName,
            contactPhone,
            deliveryAddress,
            remark,
            innerRemark,
            details = new[]
            {
                new
                {
                    goodsId,
                    goodsUnitId,
                    quantity,
                    fixedPrice = unitPrice,
                    fixedGoodsUnitId = goodsUnitId
                }
            }
        };
    }

    private static object BuildCreateAfterSalePayload(
        Guid saleOrderId,
        Guid customerId,
        Guid saleOrderDetailId,
        decimal quantity,
        AfterSaleType afterSaleType,
        AfterSaleHandleType handleType,
        string source,
        string contactName,
        string contactPhone,
        string pickupAddress,
        string remark)
    {
        return new
        {
            saleOrderId,
            customerId,
            source,
            contactName,
            contactPhone,
            pickupAddress,
            remark,
            goods = new[]
            {
                new
                {
                    saleOrderDetailId,
                    actualRefundQuantity = quantity,
                    afterSaleType,
                    reasonType = AfterSaleReasonType.QualityIssue,
                    handleType,
                    remark = $"{remark}-明细"
                }
            }
        };
    }

    private static object BuildCreateSalesReturnPayload(
        Guid afterSaleId,
        Guid wareId,
        Guid customerId,
        Guid pickupTaskId,
        Guid goodsId,
        Guid goodsUnitId,
        decimal quantity,
        decimal unitPrice,
        string batchNo,
        string remark)
    {
        return new
        {
            afterSaleId,
            wareId,
            customerId,
            inTime = "2026-07-20T12:00:00Z",
            remark,
            details = new[]
            {
                new
                {
                    pickupTaskId,
                    goodsId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    batchNo,
                    remark = "T9售后取货退货入库明细"
                }
            }
        };
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
