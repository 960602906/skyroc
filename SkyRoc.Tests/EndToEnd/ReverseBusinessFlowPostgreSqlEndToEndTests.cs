using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.AfterSales;
using Application.DTOs.Auth;
using Application.DTOs.Delivery;
using Application.DTOs.Finance;
using Application.DTOs.Orders;
using Application.DTOs.Storage;
using Domain.Entities;
using Domain.Entities.AfterSales;
using Domain.Entities.Delivery;
using Domain.Entities.Finance;
using Domain.Entities.Orders;
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

namespace SkyRoc.Tests.EndToEnd;

/// <summary>
///     T13 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证逆向全链路
///     （售后→取货→退货入库→账单冲减，以及采购退货→应付调整）。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class ReverseBusinessFlowPostgreSqlEndToEndTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库→销售订单审核→销售出库→配送签收生账单→退货退款审核取货→
    ///     销售退货入库回补→完成售后冲减应收→重复完成拒绝；采购入库应付→采购退货负向应付→
    ///     重复审核拒绝；401/403（报表相邻权限无售后/库存写）权限矩阵；临时批次精确清理。
    /// </summary>
    [Fact]
    public async Task ReverseBusinessFlow_AfterSaleReturnInboundOffsetAndPurchaseReturnPayable_OnPostgreSql()
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
        var saleBatchNo = $"{batch.Id}-SB";
        var returnBatchNo = $"{batch.Id}-SR";
        var purchaseBatchNo = $"{batch.Id}-PB";
        var inboundRemark = $"{batch.Id}-入库建批次";
        var saleOrderInnerRemark = $"{batch.Id}O";
        var saleOutRemark = $"{batch.Id}-销售出库";
        var signRemark = $"{batch.Id}-签收生账单";
        var afterSaleRemark = $"{batch.Id}-退货退款冲减";
        var salesReturnRemark = $"{batch.Id}-售后退货入库";
        var purchaseInRemark = $"{batch.Id}-采购入库应付";
        var purchaseReturnRemark = $"{batch.Id}-采购退货冲应付";
        var source = batch.Id;
        var password = "SkyRocT13ReverseE2E!2026";
        var userAgent = $"SkyRoc-T13-Reverse/{batch.Id}";
        var createName = "T13-ReverseE2E";
        var contactName = "逆向全链路王老师";
        var contactPhone = "13900001321";
        var deliveryAddress = "上海市浦东新区逆向全链路路 13 号食堂西门";
        var pickupAddress = $"{batch.Id}-上海市浦东新区逆向取货路 13 号食堂后门";
        var plannedPickupTime = new DateTime(2026, 7, 20, 14, 0, 0, DateTimeKind.Utc);

        var reportReadPermission = PermissionCodes.Business.Reports.Read;
        var afterSalesAuditPermission = PermissionCodes.Business.AfterSales.Audit;
        var storageCreatePermission = PermissionCodes.Business.Storage.Create;

        var saleInboundQuantity = NumericPrecision.RoundQuantity(20m);
        var saleInboundUnitPrice = NumericPrecision.RoundMoney(5m);
        var saleQuantity = NumericPrecision.RoundQuantity(10m);
        var saleUnitPrice = NumericPrecision.RoundMoney(8m);
        var refundQuantity = NumericPrecision.RoundQuantity(2m);
        var expectedOrderAmount = NumericPrecision.RoundMoney(saleQuantity * saleUnitPrice);
        var expectedAdjustment = NumericPrecision.RoundMoney(-(refundQuantity * saleUnitPrice));
        var expectedReceivableAfterOffset = NumericPrecision.RoundMoney(expectedOrderAmount + expectedAdjustment);

        var purchaseInboundQuantity = NumericPrecision.RoundQuantity(10m);
        var purchaseUnitPrice = NumericPrecision.RoundMoney(6.5m);
        var purchaseReturnQuantity = NumericPrecision.RoundQuantity(3m);
        var expectedInboundPayable = NumericPrecision.RoundMoney(purchaseInboundQuantity * purchaseUnitPrice);
        var expectedReturnPayable = NumericPrecision.RoundMoney(-(purchaseReturnQuantity * purchaseUnitPrice));
        var expectedReturnDocument = NumericPrecision.RoundMoney(purchaseReturnQuantity * purchaseUnitPrice);

        Guid adminRoleId;
        Guid managedCustomerId;
        Guid managedGoodsId;
        Guid managedGoodsUnitId;
        Guid managedWareId;
        Guid managedSupplierId;
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
            var supplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);
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

            var supplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == supplierCode);
            Assert.NotNull(supplier);
            managedSupplierId = supplier.Id;

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
                Desc = "T13 逆向全链路最小权限临时角色，仅持报表读取",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T13逆向操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001331",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T13逆向报表只读",
                    Gender = GenderType.Female,
                    Phone = "13900001332",
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
                Title = "T13逆向报表只读菜单",
                Component = "page.t13.reverse.seed",
                MenuType = MenuType.Menu,
                Order = 9132,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedReadButtonId,
                Code = reportReadPermission,
                Desc = "T13 报表读取权限按钮",
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

        Guid? otherInboundOrderId = null;
        Guid? saleOrderId = null;
        Guid? saleOutOrderId = null;
        Guid? deliveryTaskId = null;
        Guid? receiptId = null;
        Guid? customerBillId = null;
        Guid? afterSaleId = null;
        Guid? pickupTaskId = null;
        Guid? salesReturnOrderId = null;
        Guid? purchaseInOrderId = null;
        Guid? purchaseReturnOrderId = null;
        Guid? inboundSupplierBillId = null;
        Guid? returnSupplierBillId = null;
        Guid? saleStockBatchId = null;
        Guid? returnStockBatchId = null;
        Guid? purchaseStockBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousComplete = await anonymousClient.PostAsync(
                       $"/api/after-sales/{Guid.NewGuid()}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousComplete, ResponseCode.Unauthorized);
            }

            using (var anonymousPurchaseReturn = await anonymousClient.PostAsJsonAsync(
                       "/api/stock-out/purchase-return",
                       new { wareId = managedWareId, supplierId = managedSupplierId, details = Array.Empty<object>() }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousPurchaseReturn, ResponseCode.Unauthorized);
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

            // —— 售后端：其他入库建批次 → 订单 → 出库签收生账单 ——
            StockInOrderDto otherInbound;
            using (var createInbound = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherInPayload(
                           managedWareId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           saleBatchNo,
                           saleInboundQuantity,
                           saleInboundUnitPrice,
                           inboundRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createInbound.StatusCode);
                otherInbound = await ReadApiDataAsync<StockInOrderDto>(createInbound);
                otherInboundOrderId = otherInbound.Id;
                registry.Register<StockInOrder>(otherInbound.Id, nameof(StockInOrder.Remark), inboundRemark);
            }

            using (var auditInbound = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{otherInbound.Id}/audit",
                       new StockInAuditDto { Remark = $"{inboundRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditInbound.StatusCode);
                var audited = await ReadApiDataAsync<StockInOrderDto>(auditInbound);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
            }

            await using (var afterOtherIn = fixture.CreateDbContext())
            {
                var stockBatch = await afterOtherIn.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == saleBatchNo);
                saleStockBatchId = stockBatch.Id;
                Assert.Equal(saleInboundQuantity, stockBatch.AvailableQuantity);
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
                           remark = "T13逆向全链路销售订单",
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
                Assert.Equal(SaleOrderStatus.SortingPending, saleOrder.OrderStatus);
            }

            StockOutOrderDto saleOut;
            using (var createSaleOut = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/sale",
                       BuildCreateSaleOutPayload(
                           managedWareId,
                           saleOrder.Id,
                           managedCustomerId,
                           saleStockBatchId!.Value,
                           managedGoodsUnitId,
                           saleOrderDetailId,
                           saleQuantity,
                           saleUnitPrice,
                           saleOutRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createSaleOut.StatusCode);
                saleOut = await ReadApiDataAsync<StockOutOrderDto>(createSaleOut);
                saleOutOrderId = saleOut.Id;
                registry.Register<StockOutOrder>(saleOut.Id, nameof(StockOutOrder.Remark), saleOutRemark);
            }

            var stockOutDetailId = Assert.Single(saleOut.Details).Id;

            using (var auditSaleOut = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/sale/{saleOut.Id}/audit",
                       new StockOutAuditDto { Remark = $"{saleOutRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditSaleOut.StatusCode);
                saleOut = await ReadApiDataAsync<StockOutOrderDto>(auditSaleOut);
                Assert.Equal(StockDocumentStatus.Audited, saleOut.BusinessStatus);
            }

            DeliveryTaskDto deliveryTask;
            using (var generateTask = await adminClient.PostAsync(
                       $"/api/delivery-tasks/generate/{saleOut.Id}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, generateTask.StatusCode);
                deliveryTask = await ReadApiDataAsync<DeliveryTaskDto>(generateTask);
                deliveryTaskId = deliveryTask.Id;
                registry.Register<DeliveryTask>(deliveryTask.Id, nameof(DeliveryTask.Remark), saleOutRemark);
            }

            using (var assignDriver = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [deliveryTask.Id],
                           DriverId = managedDriverId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignDriver.StatusCode);
            }

            using (var startDelivery = await adminClient.PutAsync(
                       $"/api/delivery-tasks/{deliveryTask.Id}/start",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, startDelivery.StatusCode);
            }

            using (var sign = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{deliveryTask.Id}/sign",
                       BuildSignPayload(contactName, stockOutDetailId, saleQuantity, signRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, sign.StatusCode);
                var receipt = await ReadApiDataAsync<OrderReceiptDto>(sign);
                receiptId = receipt.Id;
                registry.Register<OrderReceipt>(receipt.Id, nameof(OrderReceipt.SignRemark), signRemark);
            }

            await using (var afterSign = fixture.CreateDbContext())
            {
                var bill = await afterSign.CustomerBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.SaleOrderId == saleOrder.Id);
                customerBillId = bill.Id;
                Assert.Equal(expectedOrderAmount, bill.ReceivableAmount);
                Assert.Equal(0m, bill.AfterSaleAdjustmentAmount);
                Assert.Equal(CustomerBillStatus.Pending, bill.BillStatus);
                AssertIndependentCustomerBillBalance(bill);
            }

            // —— 售后→取货→退货入库→冲减 ——
            AfterSaleDto returnDraft;
            using (var createAfterSale = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetailId,
                           refundQuantity,
                           AfterSaleType.ReturnAndRefund,
                           AfterSaleHandleType.GoodsDiscount,
                           source,
                           contactName,
                           contactPhone,
                           pickupAddress,
                           afterSaleRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createAfterSale.StatusCode);
                returnDraft = await ReadApiDataAsync<AfterSaleDto>(createAfterSale);
                afterSaleId = returnDraft.Id;
                registry.Register<AfterSale>(returnDraft.Id, nameof(AfterSale.Remark), afterSaleRemark);
            }

            var afterSaleGoods = Assert.Single(returnDraft.Goods);
            var refundUnitPrice = NumericPrecision.RoundMoney(afterSaleGoods.UnitPrice);
            Assert.Equal(saleUnitPrice, refundUnitPrice);

            using (var submit = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{returnDraft.Id}/submit",
                       new AfterSaleActionDto { Remark = $"{afterSaleRemark}-提交" }))
            {
                Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
            }

            AfterSaleDto approvedReturn;
            using (var approve = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{returnDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{afterSaleRemark}-同意退货" }))
            {
                Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
                approvedReturn = await ReadApiDataAsync<AfterSaleDto>(approve);
                Assert.Equal(AfterSaleStatus.ReturnPending, approvedReturn.AfterStatus);
                var createdPickup = Assert.Single(approvedReturn.PickupTasks);
                Assert.Equal(PickupTaskStatus.PendingAssign, createdPickup.PickupStatus);
                pickupTaskId = createdPickup.Id;
                registry.Register<PickupTask>(
                    createdPickup.Id,
                    nameof(PickupTask.PickupAddressSnapshot),
                    pickupAddress);
            }

            var taskId = pickupTaskId!.Value;

            using (var assignPickup = await adminClient.PutAsJsonAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/assign",
                       new AssignPickupTaskDto
                       {
                           DriverId = managedDriverId,
                           PlannedPickupTime = plannedPickupTime,
                           Remark = $"{afterSaleRemark}-分配司机"
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignPickup.StatusCode);
            }

            using (var startPickup = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/start",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, startPickup.StatusCode);
            }

            using (var completePickup = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/complete",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, completePickup.StatusCode);
                var completedPickup = await ReadApiDataAsync<PickupTaskDto>(completePickup);
                Assert.Equal(PickupTaskStatus.Completed, completedPickup.PickupStatus);
            }

            using (var rejectCompleteBeforeInbound = await adminClient.PostAsync(
                       $"/api/after-sales/{returnDraft.Id}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectCompleteBeforeInbound, ResponseCode.DatabaseError);
            }

            StockInOrderDto salesReturnOrder;
            using (var createSalesReturn = await adminClient.PostAsJsonAsync(
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
                           salesReturnRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createSalesReturn.StatusCode);
                salesReturnOrder = await ReadApiDataAsync<StockInOrderDto>(createSalesReturn);
                salesReturnOrderId = salesReturnOrder.Id;
                registry.Register<StockInOrder>(salesReturnOrder.Id, nameof(StockInOrder.Remark), salesReturnRemark);
            }

            using (var auditSalesReturn = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/sales-return/{salesReturnOrder.Id}/audit",
                       new StockInAuditDto { Remark = $"{salesReturnRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditSalesReturn.StatusCode);
                salesReturnOrder = await ReadApiDataAsync<StockInOrderDto>(auditSalesReturn);
                Assert.Equal(StockDocumentStatus.Audited, salesReturnOrder.BusinessStatus);
            }

            await using (var afterSalesReturn = fixture.CreateDbContext())
            {
                var returnBatch = await afterSalesReturn.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == returnBatchNo);
                returnStockBatchId = returnBatch.Id;
                Assert.Equal(refundQuantity, returnBatch.AvailableQuantity);
                Assert.Equal(refundQuantity, returnBatch.CurrentQuantity);

                var remainingSaleBatch = await afterSalesReturn.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == saleStockBatchId.Value);
                Assert.Equal(
                    NumericPrecision.RoundQuantity(saleInboundQuantity - saleQuantity),
                    remainingSaleBatch.AvailableQuantity);
            }

            await using (var beforeComplete = fixture.CreateDbContext())
            {
                var bill = await beforeComplete.CustomerBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == customerBillId.Value);
                Assert.Equal(0m, bill.AfterSaleAdjustmentAmount);
                Assert.DoesNotContain(
                    bill.Details,
                    detail => detail.SourceType == CustomerBillDetailSourceType.AfterSaleAdjustment);
            }

            using (var completeAfterSale = await adminClient.PostAsync(
                       $"/api/after-sales/{returnDraft.Id}/complete",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, completeAfterSale.StatusCode);
                var completed = await ReadApiDataAsync<AfterSaleDto>(completeAfterSale);
                Assert.Equal(AfterSaleStatus.Completed, completed.AfterStatus);
            }

            await using (var afterComplete = fixture.CreateDbContext())
            {
                var bill = await afterComplete.CustomerBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == customerBillId.Value);
                Assert.Equal(expectedOrderAmount, bill.OrderAmount);
                Assert.Equal(expectedAdjustment, bill.AfterSaleAdjustmentAmount);
                Assert.Equal(expectedReceivableAfterOffset, bill.ReceivableAmount);
                Assert.Equal(CustomerBillStatus.Pending, bill.BillStatus);
                Assert.Equal(2, bill.Details.Count);
                var adjustment = Assert.Single(
                    bill.Details,
                    detail => detail.SourceType == CustomerBillDetailSourceType.AfterSaleAdjustment);
                Assert.Equal(returnDraft.Id, adjustment.AfterSaleId);
                Assert.Equal(expectedAdjustment, adjustment.Amount);
                AssertIndependentCustomerBillBalance(bill);
            }

            using (var repeatComplete = await adminClient.PostAsync(
                       $"/api/after-sales/{returnDraft.Id}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(repeatComplete, ResponseCode.DatabaseError);
            }

            await using (var afterRepeat = fixture.CreateDbContext())
            {
                var bill = await afterRepeat.CustomerBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == customerBillId.Value);
                Assert.Equal(
                    1,
                    bill.Details.Count(detail =>
                        detail.SourceType == CustomerBillDetailSourceType.AfterSaleAdjustment));
                Assert.Equal(expectedReceivableAfterOffset, bill.ReceivableAmount);
            }

            // —— 采购端：采购入库应付 → 采购退货负向应付 ——
            StockInOrderDto purchaseIn;
            using (var createPurchaseIn = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/purchase",
                       BuildCreatePurchaseInPayload(
                           managedWareId,
                           managedSupplierId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           purchaseBatchNo,
                           purchaseInboundQuantity,
                           purchaseUnitPrice,
                           purchaseInRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createPurchaseIn.StatusCode);
                purchaseIn = await ReadApiDataAsync<StockInOrderDto>(createPurchaseIn);
                purchaseInOrderId = purchaseIn.Id;
                registry.Register<StockInOrder>(purchaseIn.Id, nameof(StockInOrder.Remark), purchaseInRemark);
            }

            using (var auditPurchaseIn = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/purchase/{purchaseIn.Id}/audit",
                       new StockInAuditDto { Remark = $"{purchaseInRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditPurchaseIn.StatusCode);
                purchaseIn = await ReadApiDataAsync<StockInOrderDto>(auditPurchaseIn);
                Assert.Equal(StockDocumentStatus.Audited, purchaseIn.BusinessStatus);
            }

            await using (var afterPurchaseIn = fixture.CreateDbContext())
            {
                var purchaseBatch = await afterPurchaseIn.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == purchaseBatchNo);
                purchaseStockBatchId = purchaseBatch.Id;
                Assert.Equal(purchaseInboundQuantity, purchaseBatch.AvailableQuantity);

                var bill = await afterPurchaseIn.SupplierBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.StockInOrderId == purchaseIn.Id);
                inboundSupplierBillId = bill.Id;
                Assert.Equal(SupplierBillSourceType.PurchaseStockIn, bill.SourceType);
                Assert.Equal(expectedInboundPayable, bill.PayableAmount);
                AssertIndependentSupplierBillBalance(bill);
            }

            StockOutOrderDto purchaseReturn;
            using (var createPurchaseReturn = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/purchase-return",
                       BuildPurchaseReturnPayload(
                           managedWareId,
                           managedSupplierId,
                           purchaseStockBatchId!.Value,
                           managedGoodsUnitId,
                           purchaseReturnQuantity,
                           purchaseUnitPrice,
                           purchaseReturnRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createPurchaseReturn.StatusCode);
                purchaseReturn = await ReadApiDataAsync<StockOutOrderDto>(createPurchaseReturn);
                purchaseReturnOrderId = purchaseReturn.Id;
                registry.Register<StockOutOrder>(
                    purchaseReturn.Id,
                    nameof(StockOutOrder.Remark),
                    purchaseReturnRemark);
            }

            using (var auditPurchaseReturn = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/purchase-return/{purchaseReturn.Id}/audit",
                       new StockOutAuditDto { Remark = $"{purchaseReturnRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditPurchaseReturn.StatusCode);
                purchaseReturn = await ReadApiDataAsync<StockOutOrderDto>(auditPurchaseReturn);
                Assert.Equal(StockDocumentStatus.Audited, purchaseReturn.BusinessStatus);
            }

            await using (var afterPurchaseReturn = fixture.CreateDbContext())
            {
                var purchaseBatch = await afterPurchaseReturn.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == purchaseStockBatchId.Value);
                Assert.Equal(
                    NumericPrecision.RoundQuantity(purchaseInboundQuantity - purchaseReturnQuantity),
                    purchaseBatch.AvailableQuantity);

                var returnBill = await afterPurchaseReturn.SupplierBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.StockOutOrderId == purchaseReturn.Id);
                returnSupplierBillId = returnBill.Id;
                Assert.Equal(SupplierBillSourceType.PurchaseReturnOut, returnBill.SourceType);
                Assert.Equal(expectedReturnDocument, returnBill.DocumentAmount);
                Assert.Equal(expectedReturnPayable, returnBill.PayableAmount);
                AssertIndependentSupplierBillBalance(returnBill);

                var detail = Assert.Single(returnBill.Details);
                Assert.Equal(-purchaseReturnQuantity, detail.Quantity);
                Assert.Equal(expectedReturnPayable, detail.Amount);
            }

            using (var rejectRepeatReturnAudit = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/purchase-return/{purchaseReturn.Id}/audit",
                       new StockOutAuditDto { Remark = $"{purchaseReturnRemark}-重复审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectRepeatReturnAudit, ResponseCode.DatabaseError);
            }

            // —— 权限矩阵：报表只读相邻权限 ——
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
                Assert.Contains(reportReadPermission, info.Buttons);
                Assert.DoesNotContain(afterSalesAuditPermission, info.Buttons);
                Assert.DoesNotContain(storageCreatePermission, info.Buttons);
            }

            using (var deniedComplete = await limitedClient.PostAsync(
                       $"/api/after-sales/{returnDraft.Id}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedComplete, ResponseCode.Forbidden);
            }

            using (var deniedPurchaseReturn = await limitedClient.PostAsJsonAsync(
                       "/api/stock-out/purchase-return",
                       BuildPurchaseReturnPayload(
                           managedWareId,
                           managedSupplierId,
                           purchaseStockBatchId.Value,
                           managedGoodsUnitId,
                           1m,
                           purchaseUnitPrice,
                           $"{batch.Id}-拒绝采购退货")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedPurchaseReturn, ResponseCode.Forbidden);
            }

            using (var deniedSettlement = await limitedClient.PostAsJsonAsync(
                       "/api/customer-settlements",
                       new CreateCustomerSettlementDto
                       {
                           Details =
                           [
                               new CreateCustomerSettlementDetailDto
                               {
                                   CustomerBillId = customerBillId!.Value,
                                   PaymentAmount = 1m
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedSettlement, ResponseCode.Forbidden);
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
            Assert.True(await auditContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await auditContext.Drivers.AnyAsync(item => item.Id == managedDriverId));
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await using (var preCleanup = fixture.CreateDbContext())
            {
                var residualCustomerBills = await preCleanup.CustomerBills
                    .Where(item => (customerBillId.HasValue && item.Id == customerBillId.Value)
                                   || (saleOrderId.HasValue && item.SaleOrderId == saleOrderId.Value))
                    .ToListAsync();
                if (residualCustomerBills.Count > 0)
                {
                    preCleanup.CustomerBills.RemoveRange(residualCustomerBills);
                    await preCleanup.SaveChangesAsync();
                }

                var residualSupplierBills = await preCleanup.SupplierBills
                    .Where(item => (inboundSupplierBillId.HasValue && item.Id == inboundSupplierBillId.Value)
                                   || (returnSupplierBillId.HasValue && item.Id == returnSupplierBillId.Value)
                                   || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value)
                                   || (purchaseReturnOrderId.HasValue
                                       && item.StockOutOrderId == purchaseReturnOrderId.Value))
                    .ToListAsync();
                if (residualSupplierBills.Count > 0)
                {
                    preCleanup.SupplierBills.RemoveRange(residualSupplierBills);
                    await preCleanup.SaveChangesAsync();
                }

                var residualReceipts = await preCleanup.OrderReceipts
                    .Where(item => (receiptId.HasValue && item.Id == receiptId.Value)
                                   || (item.SignRemark != null && item.SignRemark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualReceipts.Count > 0)
                {
                    preCleanup.OrderReceipts.RemoveRange(residualReceipts);
                    await preCleanup.SaveChangesAsync();
                }

                var residualDeliveryTasks = await preCleanup.DeliveryTasks
                    .Where(item => (deliveryTaskId.HasValue && item.Id == deliveryTaskId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualDeliveryTasks.Count > 0)
                {
                    preCleanup.DeliveryTasks.RemoveRange(residualDeliveryTasks);
                    await preCleanup.SaveChangesAsync();
                }

                // 取货任务被 stock_in_detail.pickup_task_id RESTRICT：须先删退货入库单再删取货/售后。
            }

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => (saleOutOrderId.HasValue && item.Id == saleOutOrderId.Value)
                                   || (purchaseReturnOrderId.HasValue && item.Id == purchaseReturnOrderId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => (otherInboundOrderId.HasValue && item.Id == otherInboundOrderId.Value)
                                   || (salesReturnOrderId.HasValue && item.Id == salesReturnOrderId.Value)
                                   || (purchaseInOrderId.HasValue && item.Id == purchaseInOrderId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                var residualOrderIdSet = residualStockOutOrders.Select(item => item.Id)
                    .Concat(residualStockInOrders.Select(item => item.Id))
                    .ToHashSet();

                var residualSupplierBills = await cleanupContext.SupplierBills
                    .Where(item => (inboundSupplierBillId.HasValue && item.Id == inboundSupplierBillId.Value)
                                   || (returnSupplierBillId.HasValue && item.Id == returnSupplierBillId.Value)
                                   || (item.StockInOrderId.HasValue
                                       && residualOrderIdSet.Contains(item.StockInOrderId.Value))
                                   || (item.StockOutOrderId.HasValue
                                       && residualOrderIdSet.Contains(item.StockOutOrderId.Value)))
                    .ToListAsync();
                if (residualSupplierBills.Count > 0)
                {
                    cleanupContext.SupplierBills.RemoveRange(residualSupplierBills);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualLedgers = await cleanupContext.StockLedgers
                    .Where(ledger => residualOrderIdSet.Contains(ledger.SourceOrderId)
                                     || (saleStockBatchId.HasValue && ledger.StockBatchId == saleStockBatchId.Value)
                                     || (returnStockBatchId.HasValue
                                         && ledger.StockBatchId == returnStockBatchId.Value)
                                     || (purchaseStockBatchId.HasValue
                                         && ledger.StockBatchId == purchaseStockBatchId.Value)
                                     || ledger.BatchNoSnapshot == saleBatchNo
                                     || ledger.BatchNoSnapshot == returnBatchNo
                                     || ledger.BatchNoSnapshot == purchaseBatchNo)
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

                var residualPickupTasks = await cleanupContext.PickupTasks
                    .Where(item => (pickupTaskId.HasValue && item.Id == pickupTaskId.Value)
                                   || (afterSaleId.HasValue && item.AfterSaleId == afterSaleId.Value)
                                   || item.PickupAddressSnapshot == pickupAddress
                                   || item.PickupAddressSnapshot.StartsWith(batch.Id))
                    .ToListAsync();
                if (residualPickupTasks.Count > 0)
                {
                    cleanupContext.PickupTasks.RemoveRange(residualPickupTasks);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualAfterSales = await cleanupContext.AfterSales
                    .Where(item => (afterSaleId.HasValue && item.Id == afterSaleId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id))
                                   || item.Source == source)
                    .ToListAsync();
                if (residualAfterSales.Count > 0)
                {
                    cleanupContext.AfterSales.RemoveRange(residualAfterSales);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualBatches = await cleanupContext.StockBatches
                    .Where(item => item.BatchNo == saleBatchNo
                                   || item.BatchNo == returnBatchNo
                                   || item.BatchNo == purchaseBatchNo
                                   || item.BatchNo.StartsWith(batch.Id)
                                   || (saleStockBatchId.HasValue && item.Id == saleStockBatchId.Value)
                                   || (returnStockBatchId.HasValue && item.Id == returnStockBatchId.Value)
                                   || (purchaseStockBatchId.HasValue && item.Id == purchaseStockBatchId.Value))
                    .ToListAsync();
                if (residualBatches.Count > 0)
                {
                    cleanupContext.StockBatches.RemoveRange(residualBatches);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualSaleOrders = await cleanupContext.SaleOrders
                    .Where(item => (saleOrderId.HasValue && item.Id == saleOrderId.Value)
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
                var residualUserRoles = await cleanupContext.UserRoles
                    .Where(item => residualUserIds.Contains(item.UserId))
                    .ToListAsync();
                if (residualUserRoles.Count > 0)
                {
                    cleanupContext.UserRoles.RemoveRange(residualUserRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualUsers = await cleanupContext.Users
                    .Where(user => residualUserIds.Contains(user.Id)
                                   || user.Username == adminUsername
                                   || user.Username == limitedUsername
                                   || (user.Username != null && user.Username.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualUsers.Count > 0)
                {
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
            Assert.False(await residualContext.CustomerBills.AnyAsync(item =>
                (customerBillId.HasValue && item.Id == customerBillId.Value)
                || (saleOrderId.HasValue && item.SaleOrderId == saleOrderId.Value)));
            Assert.False(await residualContext.SupplierBills.AnyAsync(item =>
                (inboundSupplierBillId.HasValue && item.Id == inboundSupplierBillId.Value)
                || (returnSupplierBillId.HasValue && item.Id == returnSupplierBillId.Value)
                || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value)
                || (purchaseReturnOrderId.HasValue && item.StockOutOrderId == purchaseReturnOrderId.Value)));
            Assert.False(await residualContext.AfterSales.AnyAsync(item =>
                (afterSaleId.HasValue && item.Id == afterSaleId.Value)
                || (item.Remark != null && item.Remark.StartsWith(batch.Id))
                || item.Source == source));
            Assert.False(await residualContext.PickupTasks.AnyAsync(item =>
                item.PickupAddressSnapshot.StartsWith(batch.Id)));
            Assert.False(await residualContext.OrderReceipts.AnyAsync(item =>
                item.SignRemark != null && item.SignRemark.StartsWith(batch.Id)));
            Assert.False(await residualContext.DeliveryTasks.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockOutOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockInOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockBatches.AnyAsync(item =>
                item.BatchNo == saleBatchNo
                || item.BatchNo == returnBatchNo
                || item.BatchNo == purchaseBatchNo
                || item.BatchNo.StartsWith(batch.Id)));
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
            Assert.True(await residualContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await residualContext.Drivers.AnyAsync(item => item.Id == managedDriverId));
        }
    }

    private static void AssertIndependentCustomerBillBalance(CustomerBill bill)
    {
        var pending = NumericPrecision.RoundMoney(
            Math.Max(0m, bill.ReceivableAmount - bill.SettledAmount));
        var expectedStatus = pending <= 0m && bill.SettledAmount > 0m
            ? CustomerBillStatus.Settled
            : bill.SettledAmount <= 0m
                ? CustomerBillStatus.Pending
                : CustomerBillStatus.PartiallySettled;
        Assert.Equal(expectedStatus, bill.BillStatus);
        if (expectedStatus == CustomerBillStatus.Settled)
            Assert.Equal(bill.ReceivableAmount, bill.SettledAmount);
    }

    private static void AssertIndependentSupplierBillBalance(SupplierBill bill)
    {
        var documentAmount = NumericPrecision.RoundMoney(
            bill.Details.Sum(detail => Math.Abs(detail.Amount)));
        var payableAmount = NumericPrecision.RoundMoney(
            bill.Details.Sum(detail => detail.Amount));
        Assert.Equal(documentAmount, bill.DocumentAmount);
        Assert.Equal(payableAmount, bill.PayableAmount);
        Assert.True(bill.SettledAmount >= 0m);
        Assert.True(bill.SettledAmount <= bill.DocumentAmount);
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
                    remark = "T13逆向其他入库明细"
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
                    remark = "T13逆向销售出库明细"
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
            inTime = "2026-07-20T15:00:00Z",
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
                    remark = "T13逆向售后退货入库明细"
                }
            }
        };
    }

    private static object BuildCreatePurchaseInPayload(
        Guid wareId,
        Guid supplierId,
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
            supplierId,
            purchasePattern = PurchasePattern.SupplierDirect,
            inTime = "2026-07-20T09:30:00Z",
            expectedArrivalTime = "2026-07-20T15:00:00Z",
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
                    productDate = "2026-07-19",
                    remark = "T13逆向采购入库明细"
                }
            }
        };
    }

    private static object BuildPurchaseReturnPayload(
        Guid wareId,
        Guid supplierId,
        Guid stockBatchId,
        Guid goodsUnitId,
        decimal quantity,
        decimal unitPrice,
        string remark)
    {
        return new
        {
            wareId,
            supplierId,
            outTime = "2026-07-20T16:00:00Z",
            remark,
            details = new[]
            {
                new
                {
                    stockBatchId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    remark = "T13逆向采购退货出库明细"
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
