using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Delivery;
using Application.DTOs.Orders;
using Application.DTOs.Role;
using Application.DTOs.Storage;
using Domain.Entities;
using Domain.Entities.Delivery;
using Domain.Entities.Finance;
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

namespace SkyRoc.Tests.Delivery;

/// <summary>
///     T8 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证分批签收聚合为订单签收、
///     客户账单生成，以及回单归档聚合为整单已回单。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class DeliverySignAndReturnReceiptAggregationPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库建批次 → 销售订单审核 → 两张分批销售出库 → 生成两任务 →
    ///     未开始签收/未签收回单拒绝 → 首任务签收后订单仍配送中且无账单 →
    ///     末任务签收后订单 Signed 并生成客户账单 → 分次回单聚合为 Returned →
    ///     重复签收/回单与超额验收拒绝 → 401/403 签收/回单权限矩阵；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task Delivery_SignAndReturnReceiptAggregatesOrderBillAndReturnStatus_OnPostgreSql()
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

        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var writeMenuName = $"{batch.Id}W";
        var batchNo = $"{batch.Id}-BATCH";
        var inboundRemark = $"{batch.Id}-入库建批次";
        var saleOrderInnerRemark = $"{batch.Id}O";
        var saleOutRemark1 = $"{batch.Id}-销售出库1";
        var saleOutRemark2 = $"{batch.Id}-销售出库2";
        var signRemark1 = $"{batch.Id}-首批签收";
        var signRemark2 = $"{batch.Id}-末批签收";
        var returnRemark1 = $"{batch.Id}-首批回单";
        var returnRemark2 = $"{batch.Id}-末批回单";
        var receiptImageUrl1 = $"https://assets.skyroc.example/autotest/{batch.Id}/receipt-1.pdf";
        var receiptImageUrl2 = $"https://assets.skyroc.example/autotest/{batch.Id}/receipt-2.pdf";
        var password = "SkyRocDeliverySignReceipt!2026";
        var userAgent = $"SkyRoc-T8-DeliverySignReceipt/{batch.Id}";
        var createName = "T8-DeliverySignReturnReceipt";
        var contactName = "签收回单李老师";
        var contactPhone = "13900008871";
        var deliveryAddress = "上海市浦东新区签收回单路 36 号食堂西门";

        var deliveryReadPermission = PermissionCodes.Business.Delivery.Read;
        var deliveryCreatePermission = PermissionCodes.Business.Delivery.Create;
        var deliveryUpdatePermission = PermissionCodes.Business.Delivery.Update;

        var inboundQuantity = NumericPrecision.RoundQuantity(6m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(6m);
        var saleQuantity = NumericPrecision.RoundQuantity(4m);
        var partialQuantity = NumericPrecision.RoundQuantity(2m);
        var saleUnitPrice = NumericPrecision.RoundMoney(9m);
        var expectedSettlement = NumericPrecision.RoundMoney(saleQuantity * saleUnitPrice);

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
                Desc = "T8 签收回单聚合最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T8签收回单操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008881",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T8签收回单只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900008882",
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
                    Title = "T8签收回单只读菜单",
                    Component = "page.t8.delivery.sign.seed",
                    MenuType = MenuType.Menu,
                    Order = 9871,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T8签收回单写权限菜单",
                    Component = "page.t8.delivery.sign.write",
                    MenuType = MenuType.Menu,
                    Order = 9872,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedReadButtonId,
                    Code = deliveryReadPermission,
                    Desc = "T8 配送读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = deliveryCreatePermission,
                    Desc = "T8 配送创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = deliveryUpdatePermission,
                    Desc = "T8 配送更新权限按钮",
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
        Guid? saleOrderId = null;
        Guid? saleOutOrderId1 = null;
        Guid? saleOutOrderId2 = null;
        Guid? deliveryTaskId1 = null;
        Guid? deliveryTaskId2 = null;
        Guid? receiptId1 = null;
        Guid? receiptId2 = null;
        Guid? customerBillId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousSign = await anonymousClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{Guid.NewGuid()}/sign",
                       new SignDeliveryTaskDto
                       {
                           SignerName = "未认证签收",
                           Details =
                           [
                               new SignDeliveryCheckDetailDto
                               {
                                   StockOutDetailId = Guid.NewGuid(),
                                   AcceptedBaseQuantity = 1m,
                                   CheckStatus = OrderCustomerCheckStatus.Accepted
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousSign, ResponseCode.Unauthorized);
            }

            using (var anonymousReceipt = await anonymousClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{Guid.NewGuid()}/receipt",
                       new ReturnOrderReceiptDto
                       {
                           ReceiptImageUrl = "https://example.invalid/unauth.pdf",
                           Remark = "未认证回单"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousReceipt, ResponseCode.Unauthorized);
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
                           batchNo,
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
                                         && item.BatchNo == batchNo);
                createdBatchId = stockBatch.Id;
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);
            }

            SaleOrderDto saleOrder;
            using (var createOrder = await adminClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-20T08:00:00Z",
                           receiveDate = "2026-07-21T06:00:00Z",
                           contactName,
                           contactPhone,
                           deliveryAddress,
                           remark = "T8签收回单聚合切片销售订单",
                           innerRemark = saleOrderInnerRemark,
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = saleQuantity,
                                   fixedPrice = saleUnitPrice,
                                   fixedGoodsUnitId = managedGoodsUnitId
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createOrder.StatusCode);
                saleOrder = await ReadApiDataAsync<SaleOrderDto>(createOrder);
                saleOrderId = saleOrder.Id;
                registry.Register<SaleOrder>(saleOrder.Id, nameof(SaleOrder.InnerRemark), saleOrderInnerRemark);
            }

            var saleOrderDetailId = Assert.Single(saleOrder.Details).Id;

            using (var approveOrder = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{saleOrder.Id}/approve",
                       new SaleOrderAuditDto { Remark = $"{saleOrderInnerRemark}-审核通过" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveOrder.StatusCode);
                saleOrder = await ReadApiDataAsync<SaleOrderDto>(approveOrder);
            }

            var (task1, detail1) = await CreateAuditedSaleOutAndTaskAsync(
                adminClient,
                registry,
                managedWareId,
                saleOrder.Id,
                managedCustomerId,
                createdBatchId!.Value,
                managedGoodsUnitId,
                saleOrderDetailId,
                partialQuantity,
                saleUnitPrice,
                saleOutRemark1);
            deliveryTaskId1 = task1.Id;
            saleOutOrderId1 = task1.StockOutOrderId;

            var (task2, detail2) = await CreateAuditedSaleOutAndTaskAsync(
                adminClient,
                registry,
                managedWareId,
                saleOrder.Id,
                managedCustomerId,
                createdBatchId.Value,
                managedGoodsUnitId,
                saleOrderDetailId,
                partialQuantity,
                saleUnitPrice,
                saleOutRemark2);
            deliveryTaskId2 = task2.Id;
            saleOutOrderId2 = task2.StockOutOrderId;

            using (var assignBoth = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [task1.Id, task2.Id],
                           DriverId = managedDriverId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignBoth.StatusCode);
                var assigned = await ReadApiDataAsync<List<DeliveryTaskDto>>(assignBoth);
                Assert.Equal(2, assigned.Count);
                Assert.All(assigned, item => Assert.Equal(DeliveryTaskStatus.Assigned, item.DeliveryStatus));
            }

            using (var start1 = await adminClient.PutAsync($"/api/delivery-tasks/{task1.Id}/start", null))
            {
                Assert.Equal(HttpStatusCode.OK, start1.StatusCode);
                var started = await ReadApiDataAsync<DeliveryTaskDto>(start1);
                Assert.Equal(DeliveryTaskStatus.Delivering, started.DeliveryStatus);
            }

            using (var rejectSignAssigned = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task2.Id}/sign",
                       BuildSignPayload("非法签收", detail2, partialQuantity, $"{batch.Id}-未开始签收")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectSignAssigned, ResponseCode.DatabaseError);
            }

            using (var rejectReturnBeforeSign = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task1.Id}/receipt",
                       new ReturnOrderReceiptDto
                       {
                           ReceiptImageUrl = receiptImageUrl1,
                           Remark = $"{batch.Id}-未签收回单"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectReturnBeforeSign, ResponseCode.DatabaseError);
            }

            using (var start2 = await adminClient.PutAsync($"/api/delivery-tasks/{task2.Id}/start", null))
            {
                Assert.Equal(HttpStatusCode.OK, start2.StatusCode);
            }

            OrderReceiptDto receipt1;
            using (var sign1 = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task1.Id}/sign",
                       BuildSignPayload(" 李老师 ", detail1, partialQuantity, signRemark1)))
            {
                Assert.Equal(HttpStatusCode.OK, sign1.StatusCode);
                receipt1 = await ReadApiDataAsync<OrderReceiptDto>(sign1);
                receiptId1 = receipt1.Id;
                registry.Register<OrderReceipt>(receipt1.Id, nameof(OrderReceipt.SignRemark), signRemark1);
            }

            Assert.Equal("李老师", receipt1.SignerName);
            Assert.Equal(signRemark1, receipt1.SignRemark);
            Assert.Null(receipt1.ReturnedTime);
            Assert.Equal(partialQuantity, Assert.Single(receipt1.CheckDetails).AcceptedBaseQuantity);

            await using (var afterFirstSign = fixture.CreateDbContext())
            {
                var persistedTask1 = await afterFirstSign.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task1.Id);
                var persistedTask2 = await afterFirstSign.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task2.Id);
                Assert.Equal(DeliveryTaskStatus.Signed, persistedTask1.DeliveryStatus);
                Assert.Equal(DeliveryTaskStatus.Delivering, persistedTask2.DeliveryStatus);

                var persistedOrder = await afterFirstSign.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == saleOrder.Id);
                Assert.Equal(SaleOrderStatus.Delivering, persistedOrder.OrderStatus);
                Assert.Equal(OrderOutStorageStatus.Generated, persistedOrder.OutStorageStatus);
                Assert.Equal(OrderReturnStatus.NotReturned, persistedOrder.ReturnStatus);
                Assert.False(await afterFirstSign.CustomerBills.AnyAsync(bill => bill.SaleOrderId == saleOrder.Id));
            }

            using (var rejectDuplicateSign = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task1.Id}/sign",
                       BuildSignPayload("重复签收", detail1, partialQuantity, $"{batch.Id}-重复签收")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectDuplicateSign, ResponseCode.DatabaseError);
            }

            using (var rejectOverAccept = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task2.Id}/sign",
                       BuildSignPayload(
                           "超额验收",
                           detail2,
                           NumericPrecision.RoundQuantity(partialQuantity + 1m),
                           $"{batch.Id}-超额验收")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectOverAccept, ResponseCode.DatabaseError);
            }

            OrderReceiptDto receipt2;
            using (var sign2 = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task2.Id}/sign",
                       BuildSignPayload("王主任", detail2, partialQuantity, signRemark2)))
            {
                Assert.Equal(HttpStatusCode.OK, sign2.StatusCode);
                receipt2 = await ReadApiDataAsync<OrderReceiptDto>(sign2);
                receiptId2 = receipt2.Id;
                registry.Register<OrderReceipt>(receipt2.Id, nameof(OrderReceipt.SignRemark), signRemark2);
            }

            Assert.Equal("王主任", receipt2.SignerName);
            Assert.Equal(signRemark2, receipt2.SignRemark);

            await using (var afterFullSign = fixture.CreateDbContext())
            {
                var persistedOrder = await afterFullSign.SaleOrders
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == saleOrder.Id);
                Assert.Equal(SaleOrderStatus.Signed, persistedOrder.OrderStatus);
                Assert.Equal(OrderReturnStatus.NotReturned, persistedOrder.ReturnStatus);
                Assert.Equal(expectedSettlement, persistedOrder.SettlementPrice);

                var orderDetail = Assert.Single(persistedOrder.Details);
                Assert.Equal(saleQuantity, orderDetail.CustomerCheckBaseQuantity);
                Assert.Equal(expectedSettlement, orderDetail.CustomerCheckPrice);
                Assert.Equal(OrderCustomerCheckStatus.Accepted, orderDetail.CustomerCheckStatus);

                var bill = await afterFullSign.CustomerBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.SaleOrderId == saleOrder.Id);
                customerBillId = bill.Id;
                Assert.Equal(expectedSettlement, bill.OrderAmount);
                Assert.Equal(expectedSettlement, bill.ReceivableAmount);
                Assert.Equal(CustomerBillStatus.Pending, bill.BillStatus);
                Assert.Contains(
                    bill.Details,
                    detail => detail.SourceType == CustomerBillDetailSourceType.OrderAcceptance
                              && detail.Amount == expectedSettlement);
            }

            using (var return1 = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task1.Id}/receipt",
                       new ReturnOrderReceiptDto
                       {
                           ReceiptImageUrl = $" {receiptImageUrl1} ",
                           Remark = returnRemark1
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, return1.StatusCode);
                var returned = await ReadApiDataAsync<OrderReceiptDto>(return1);
                Assert.Equal(receiptImageUrl1, returned.ReceiptImageUrl);
                Assert.Equal(returnRemark1, returned.ReturnRemark);
                Assert.NotNull(returned.ReturnedTime);
            }

            await using (var afterFirstReturn = fixture.CreateDbContext())
            {
                var persistedOrder = await afterFirstReturn.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == saleOrder.Id);
                Assert.Equal(SaleOrderStatus.Signed, persistedOrder.OrderStatus);
                Assert.Equal(OrderReturnStatus.NotReturned, persistedOrder.ReturnStatus);

                var persistedReceipt1 = await afterFirstReturn.OrderReceipts.AsNoTracking()
                    .SingleAsync(item => item.Id == receiptId1);
                var persistedReceipt2 = await afterFirstReturn.OrderReceipts.AsNoTracking()
                    .SingleAsync(item => item.Id == receiptId2);
                Assert.NotNull(persistedReceipt1.ReturnedTime);
                Assert.Null(persistedReceipt2.ReturnedTime);
            }

            using (var rejectDuplicateReturn = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task1.Id}/receipt",
                       new ReturnOrderReceiptDto
                       {
                           ReceiptImageUrl = receiptImageUrl1,
                           Remark = $"{batch.Id}-重复回单"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectDuplicateReturn, ResponseCode.DatabaseError);
            }

            using (var return2 = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task2.Id}/receipt",
                       new ReturnOrderReceiptDto
                       {
                           ReceiptImageUrl = receiptImageUrl2,
                           Remark = returnRemark2
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, return2.StatusCode);
                var returned = await ReadApiDataAsync<OrderReceiptDto>(return2);
                Assert.Equal(receiptImageUrl2, returned.ReceiptImageUrl);
                Assert.Equal(returnRemark2, returned.ReturnRemark);
                Assert.NotNull(returned.ReturnedTime);
            }

            await using (var afterFullReturn = fixture.CreateDbContext())
            {
                var persistedOrder = await afterFullReturn.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == saleOrder.Id);
                Assert.Equal(SaleOrderStatus.Signed, persistedOrder.OrderStatus);
                Assert.Equal(OrderReturnStatus.Returned, persistedOrder.ReturnStatus);

                Assert.All(
                    await afterFullReturn.OrderReceipts.AsNoTracking()
                        .Where(item => item.SaleOrderId == saleOrder.Id)
                        .ToListAsync(),
                    receipt => Assert.NotNull(receipt.ReturnedTime));
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
                Assert.Contains(deliveryReadPermission, info.Buttons);
                Assert.DoesNotContain(deliveryCreatePermission, info.Buttons);
                Assert.DoesNotContain(deliveryUpdatePermission, info.Buttons);
            }

            using (var allowedGet = await limitedClient.GetAsync($"/api/delivery-tasks/{task1.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedGet.StatusCode);
                var task = await ReadApiDataAsync<DeliveryTaskDto>(allowedGet);
                Assert.Equal(task1.Id, task.Id);
                Assert.Equal(DeliveryTaskStatus.Signed, task.DeliveryStatus);
            }

            using (var deniedSign = await limitedClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task2.Id}/sign",
                       BuildSignPayload("无更新权限", detail2, partialQuantity, $"{batch.Id}-无签收权限")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedSign, ResponseCode.Forbidden);
            }

            using (var deniedReceipt = await limitedClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task2.Id}/receipt",
                       new ReturnOrderReceiptDto
                       {
                           ReceiptImageUrl = receiptImageUrl2,
                           Remark = $"{batch.Id}-无回单权限"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedReceipt, ResponseCode.Forbidden);
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
            Assert.True(await auditContext.Drivers.AnyAsync(item =>
                item.Id == managedDriverId && item.Code == DemoDataStableKeyCatalog.Create("DRIVER", 1)));
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualSaleOrderIds = new List<Guid>();
                if (saleOrderId.HasValue)
                    residualSaleOrderIds.Add(saleOrderId.Value);

                var residualBillIds = new List<Guid>();
                if (customerBillId.HasValue)
                    residualBillIds.Add(customerBillId.Value);

                var residualBills = await cleanupContext.CustomerBills
                    .Where(item => residualBillIds.Contains(item.Id)
                                   || residualSaleOrderIds.Contains(item.SaleOrderId))
                    .ToListAsync();
                if (residualBills.Count > 0)
                {
                    cleanupContext.CustomerBills.RemoveRange(residualBills);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualReceiptIds = new List<Guid>();
                if (receiptId1.HasValue)
                    residualReceiptIds.Add(receiptId1.Value);
                if (receiptId2.HasValue)
                    residualReceiptIds.Add(receiptId2.Value);
                var residualReceipts = await cleanupContext.OrderReceipts
                    .Where(item => residualReceiptIds.Contains(item.Id)
                                   || (item.SignRemark != null
                                       && (item.SignRemark == signRemark1
                                           || item.SignRemark == signRemark2
                                           || item.SignRemark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualReceipts.Count > 0)
                {
                    cleanupContext.OrderReceipts.RemoveRange(residualReceipts);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualTaskIds = new List<Guid>();
                if (deliveryTaskId1.HasValue)
                    residualTaskIds.Add(deliveryTaskId1.Value);
                if (deliveryTaskId2.HasValue)
                    residualTaskIds.Add(deliveryTaskId2.Value);
                var residualTasks = await cleanupContext.DeliveryTasks
                    .Where(item => residualTaskIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == saleOutRemark1
                                           || item.Remark == saleOutRemark2
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualTasks.Count > 0)
                {
                    cleanupContext.DeliveryTasks.RemoveRange(residualTasks);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualStockOutIds = new List<Guid>();
                if (saleOutOrderId1.HasValue)
                    residualStockOutIds.Add(saleOutOrderId1.Value);
                if (saleOutOrderId2.HasValue)
                    residualStockOutIds.Add(saleOutOrderId2.Value);
                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => residualStockOutIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == saleOutRemark1
                                           || item.Remark == saleOutRemark2
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
                    writeUpdateButtonId
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
            Assert.False(await residualContext.CustomerBills.AnyAsync(item =>
                (customerBillId.HasValue && item.Id == customerBillId.Value)
                || (saleOrderId.HasValue && item.SaleOrderId == saleOrderId.Value)));
            Assert.False(await residualContext.OrderReceipts.AnyAsync(item =>
                item.SignRemark != null && item.SignRemark.StartsWith(batch.Id)));
            Assert.False(await residualContext.DeliveryTasks.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockOutOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockInOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockBatches.AnyAsync(item => item.BatchNo == batchNo));
            Assert.False(await residualContext.SaleOrders.AnyAsync(item =>
                item.InnerRemark != null && item.InnerRemark.StartsWith(batch.Id)));
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
                || button.CreateName == createName));
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
            Assert.True(await residualContext.Drivers.AnyAsync(item => item.Id == managedDriverId));
        }
    }

    private static SignDeliveryTaskDto BuildSignPayload(
        string signerName,
        Guid stockOutDetailId,
        decimal acceptedBaseQuantity,
        string remark)
    {
        return new SignDeliveryTaskDto
        {
            SignerName = signerName,
            Remark = remark,
            Details =
            [
                new SignDeliveryCheckDetailDto
                {
                    StockOutDetailId = stockOutDetailId,
                    AcceptedBaseQuantity = acceptedBaseQuantity,
                    CheckStatus = OrderCustomerCheckStatus.Accepted
                }
            ]
        };
    }

    private static async Task<(DeliveryTaskDto Task, Guid StockOutDetailId)> CreateAuditedSaleOutAndTaskAsync(
        HttpClient adminClient,
        BatchCleanupRegistry registry,
        Guid wareId,
        Guid saleOrderId,
        Guid customerId,
        Guid stockBatchId,
        Guid goodsUnitId,
        Guid saleOrderDetailId,
        decimal quantity,
        decimal unitPrice,
        string remark)
    {
        StockOutOrderDto saleOutOrder;
        using (var createSaleOut = await adminClient.PostAsJsonAsync(
                   "/api/stock-out/sale",
                   BuildCreateSaleOutPayload(
                       wareId,
                       saleOrderId,
                       customerId,
                       stockBatchId,
                       goodsUnitId,
                       saleOrderDetailId,
                       quantity,
                       unitPrice,
                       remark)))
        {
            Assert.Equal(HttpStatusCode.OK, createSaleOut.StatusCode);
            saleOutOrder = await ReadApiDataAsync<StockOutOrderDto>(createSaleOut);
            registry.Register<StockOutOrder>(saleOutOrder.Id, nameof(StockOutOrder.Remark), remark);
        }

        using (var auditSaleOut = await adminClient.PostAsJsonAsync(
                   $"/api/stock-out/sale/{saleOutOrder.Id}/audit",
                   new StockOutAuditDto { Remark = $"{remark}-审核" }))
        {
            Assert.Equal(HttpStatusCode.OK, auditSaleOut.StatusCode);
            var audited = await ReadApiDataAsync<StockOutOrderDto>(auditSaleOut);
            Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
        }

        DeliveryTaskDto task;
        using (var generate = await adminClient.PostAsync(
                   $"/api/delivery-tasks/generate/{saleOutOrder.Id}",
                   null))
        {
            Assert.Equal(HttpStatusCode.OK, generate.StatusCode);
            task = await ReadApiDataAsync<DeliveryTaskDto>(generate);
            registry.Register<DeliveryTask>(task.Id, nameof(DeliveryTask.Remark), remark);
        }

        var detailId = Assert.Single(saleOutOrder.Details).Id;
        return (task, detailId);
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
                    remark = "T8其他入库明细"
                }
            }
        };
    }

    private static object BuildCreateSaleOutPayload(
        Guid wareId,
        Guid saleOrderId,
        Guid customerId,
        Guid stockBatchId,
        Guid goodsUnitId,
        Guid saleOrderDetailId,
        decimal quantity,
        decimal unitPrice,
        string remark)
    {
        return new
        {
            wareId,
            saleOrderId,
            customerId,
            outTime = "2026-07-20T10:00:00Z",
            remark,
            details = new[]
            {
                new
                {
                    saleOrderDetailId,
                    stockBatchId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    remark = "T8销售出库明细"
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
