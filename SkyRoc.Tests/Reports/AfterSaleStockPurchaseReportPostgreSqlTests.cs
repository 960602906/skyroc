using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.AfterSales;
using Application.DTOs.Auth;
using Application.DTOs.Delivery;
using Application.DTOs.Orders;
using Application.DTOs.Reports;
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

namespace SkyRoc.Tests.Reports;

/// <summary>
///     T11 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证售后、日库存与采购出入库报表
///     口径、未审核/未完成排除、补货零退款数量与 401/403 权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AfterSaleStockPurchaseReportPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     采购入库审核→草稿其他入库排除→采购退货→销售签收→仅退款完成计入售后→草稿售后排除→
    ///     补货完成退款数量为 0→日库存/采购三维独立重算→401/403 权限矩阵；临时批次精确清理。
    /// </summary>
    [Fact]
    public async Task AfterSaleStockPurchaseReport_CaliberIndependentExpectationAndPermissionMatrix_OnPostgreSql()
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
        var purchaseBatchNo = $"{batch.Id}-P";
        var draftBatchNo = $"{batch.Id}-D";
        var purchaseInRemark = $"{batch.Id}-采购入库报表";
        var draftOtherInRemark = $"{batch.Id}-草稿其他入库排除";
        var purchaseReturnRemark = $"{batch.Id}-采购退货报表";
        var saleOrderInnerRemark = $"{batch.Id}O";
        var saleOutRemark = $"{batch.Id}-销售出库报表";
        var signRemark = $"{batch.Id}-签收衔售后报表";
        var refundRemark = $"{batch.Id}-仅退款报表";
        var draftAfterSaleRemark = $"{batch.Id}-草稿售后排除";
        var replenishRemark = $"{batch.Id}-补货零退款";
        var source = batch.Id;
        var password = "SkyRocT11AspReport!2026";
        var userAgent = $"SkyRoc-T11-AfterSaleStockPurchase/{batch.Id}";
        var createName = "T11-AfterSaleStockPurchase";
        var contactName = "报表联调周老师";
        var contactPhone = "13900001141";
        var deliveryAddress = $"杭州市余杭区报表联调路 {batch.Id} 号食堂";
        var pickupAddress = $"{batch.Id}-杭州市余杭区报表联调路 {batch.Id} 号食堂后门";

        // 联调库存/采购单据多落在 7–9 月，选用 4 月隔离窗口。
        var reportDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var reportDateStart = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var reportDateEnd = new DateTime(2026, 4, 15, 23, 59, 59, DateTimeKind.Utc);
        var expectedReportDate = DateOnly.FromDateTime(reportDate);
        var orderDate = new DateTime(2026, 4, 15, 8, 0, 0, DateTimeKind.Utc);
        var receiveDate = new DateTime(2026, 4, 16, 6, 0, 0, DateTimeKind.Utc);

        var reportsReadPermission = PermissionCodes.Business.Reports.Read;
        var financeReadPermission = PermissionCodes.Business.Finance.Read;

        var purchaseInQuantity = NumericPrecision.RoundQuantity(20m);
        var purchaseUnitPrice = NumericPrecision.RoundMoney(6m);
        var draftOtherInQuantity = NumericPrecision.RoundQuantity(5m);
        var purchaseReturnQuantity = NumericPrecision.RoundQuantity(3m);
        var saleQuantity = NumericPrecision.RoundQuantity(8m);
        var saleUnitPrice = NumericPrecision.RoundMoney(9m);
        var refundQuantity = NumericPrecision.RoundQuantity(2m);
        var replenishQuantity = NumericPrecision.RoundQuantity(1m);

        var expectedPurchaseInAmount = NumericPrecision.RoundMoney(purchaseInQuantity * purchaseUnitPrice);
        var expectedPurchaseReturnAmount = NumericPrecision.RoundMoney(purchaseReturnQuantity * purchaseUnitPrice);
        var expectedSaleOutAmount = NumericPrecision.RoundMoney(saleQuantity * saleUnitPrice);
        var expectedRefundAmount = NumericPrecision.RoundMoney(refundQuantity * saleUnitPrice);
        var expectedStockInQuantity = purchaseInQuantity;
        var expectedStockOutQuantity = NumericPrecision.RoundQuantity(purchaseReturnQuantity + saleQuantity);
        var expectedStockInAmount = expectedPurchaseInAmount;
        var expectedStockOutAmount = NumericPrecision.RoundMoney(expectedPurchaseReturnAmount + expectedSaleOutAmount);
        var expectedPurchaseNetAmount = NumericPrecision.RoundMoney(expectedPurchaseInAmount - expectedPurchaseReturnAmount);

        Guid adminRoleId;
        Guid managedCustomerId;
        Guid managedGoodsId;
        string managedGoodsName = null!;
        string managedGoodsCode = null!;
        string managedBaseUnitName = null!;
        Guid managedGoodsUnitId;
        Guid managedWareId;
        Guid managedSupplierId;
        string managedSupplierName = null!;
        Guid managedPurchaserId;
        string managedPurchaserName = null!;
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
            var purchaserCode = DemoDataStableKeyCatalog.Create("PURCHASER", 1);
            var driverCode = DemoDataStableKeyCatalog.Create("DRIVER", 1);

            var customer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == customerCode);
            Assert.NotNull(customer);
            managedCustomerId = customer.Id;

            var goods = await seedContext.Set<GoodsEntity>()
                .Include(item => item.BaseUnit)
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsCode);
            Assert.NotNull(goods);
            managedGoodsId = goods.Id;
            managedGoodsName = goods.Name;
            managedGoodsCode = goods.Code!;
            managedBaseUnitName = goods.BaseUnit?.Name ?? string.Empty;

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
            managedSupplierName = supplier.Name;

            var purchaser = await seedContext.Purchasers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == purchaserCode);
            Assert.NotNull(purchaser);
            managedPurchaserId = purchaser.Id;
            managedPurchaserName = purchaser.Name;

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
                Desc = "T11 售后库存采购报表相邻权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T11售后库存采购报表操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001151",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T11财务只读无报表权限",
                    Gender = GenderType.Female,
                    Phone = "13900001152",
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
                Title = "T11财务只读菜单",
                Component = "page.t11.aftersale.stock.purchase.seed",
                MenuType = MenuType.Menu,
                Order = 9112,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedReadButtonId,
                Code = financeReadPermission,
                Desc = "T11 财务读取权限按钮（故意不含报表）",
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

        Guid? purchaseInOrderId = null;
        Guid? draftOtherInOrderId = null;
        Guid? purchaseReturnOrderId = null;
        Guid? saleOrderId = null;
        Guid? saleOutOrderId = null;
        Guid? deliveryTaskId = null;
        Guid? receiptId = null;
        Guid? customerBillId = null;
        Guid? refundAfterSaleId = null;
        Guid? draftAfterSaleId = null;
        Guid? replenishAfterSaleId = null;
        Guid? createdBatchId = null;
        Guid? inboundBillId = null;
        Guid? returnBillId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousAfterSale = await anonymousClient.GetAsync(
                       BuildAfterSaleUrl(reportDateStart, reportDateEnd, managedCustomerId)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousAfterSale, ResponseCode.Unauthorized);
            }

            using (var anonymousStock = await anonymousClient.GetAsync(
                       BuildDailyStockUrl(reportDateStart, reportDateEnd, managedWareId, managedGoodsId)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousStock, ResponseCode.Unauthorized);
            }

            using (var anonymousPurchase = await anonymousClient.GetAsync(
                       BuildPurchaseGoodsUrl(
                           reportDateStart,
                           reportDateEnd,
                           managedWareId,
                           managedSupplierId,
                           managedPurchaserId,
                           managedGoodsId)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousPurchase, ResponseCode.Unauthorized);
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

            StockInOrderDto purchaseInOrder;
            using (var createPurchaseIn = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/purchase",
                       BuildCreatePurchaseInPayload(
                           managedWareId,
                           managedSupplierId,
                           managedPurchaserId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           purchaseBatchNo,
                           purchaseInQuantity,
                           purchaseUnitPrice,
                           purchaseInRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createPurchaseIn.StatusCode);
                purchaseInOrder = await ReadApiDataAsync<StockInOrderDto>(createPurchaseIn);
                purchaseInOrderId = purchaseInOrder.Id;
                registry.Register<StockInOrder>(purchaseInOrder.Id, nameof(StockInOrder.Remark), purchaseInRemark);
            }

            using (var auditPurchaseIn = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/purchase/{purchaseInOrder.Id}/audit",
                       new StockInAuditDto { Remark = $"{purchaseInRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditPurchaseIn.StatusCode);
                var audited = await ReadApiDataAsync<StockInOrderDto>(auditPurchaseIn);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
            }

            await using (var afterPurchaseIn = fixture.CreateDbContext())
            {
                var stockBatch = await afterPurchaseIn.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == purchaseBatchNo);
                createdBatchId = stockBatch.Id;
                Assert.Equal(purchaseInQuantity, stockBatch.AvailableQuantity);

                var bill = await afterPurchaseIn.SupplierBills.AsNoTracking()
                    .SingleAsync(item => item.StockInOrderId == purchaseInOrder.Id);
                inboundBillId = bill.Id;
            }

            StockInOrderDto draftOtherIn;
            using (var createDraftOther = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/other",
                       BuildCreateOtherInPayload(
                           managedWareId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           draftBatchNo,
                           draftOtherInQuantity,
                           purchaseUnitPrice,
                           draftOtherInRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createDraftOther.StatusCode);
                draftOtherIn = await ReadApiDataAsync<StockInOrderDto>(createDraftOther);
                draftOtherInOrderId = draftOtherIn.Id;
                registry.Register<StockInOrder>(draftOtherIn.Id, nameof(StockInOrder.Remark), draftOtherInRemark);
                Assert.Equal(StockDocumentStatus.Draft, draftOtherIn.BusinessStatus);
            }

            StockOutOrderDto purchaseReturnOrder;
            using (var createReturn = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/purchase-return",
                       BuildPurchaseReturnPayload(
                           managedWareId,
                           managedSupplierId,
                           createdBatchId!.Value,
                           managedGoodsUnitId,
                           purchaseReturnQuantity,
                           purchaseUnitPrice,
                           purchaseReturnRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createReturn.StatusCode);
                purchaseReturnOrder = await ReadApiDataAsync<StockOutOrderDto>(createReturn);
                purchaseReturnOrderId = purchaseReturnOrder.Id;
                registry.Register<StockOutOrder>(
                    purchaseReturnOrder.Id,
                    nameof(StockOutOrder.Remark),
                    purchaseReturnRemark);
            }

            using (var auditReturn = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/purchase-return/{purchaseReturnOrder.Id}/audit",
                       new StockOutAuditDto { Remark = $"{purchaseReturnRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditReturn.StatusCode);
                var audited = await ReadApiDataAsync<StockOutOrderDto>(auditReturn);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
            }

            await using (var afterReturn = fixture.CreateDbContext())
            {
                var bill = await afterReturn.SupplierBills.AsNoTracking()
                    .SingleAsync(item => item.StockOutOrderId == purchaseReturnOrder.Id);
                returnBillId = bill.Id;
            }

            SaleOrderDto saleOrder;
            using (var createOrder = await adminClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = orderDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                           receiveDate = receiveDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                           contactName,
                           contactPhone,
                           deliveryAddress,
                           remark = "T11售后库存采购报表切片销售订单",
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

            var (task, stockOutDetailId) = await CreateAuditedSaleOutAndTaskAsync(
                adminClient,
                registry,
                managedWareId,
                saleOrder.Id,
                managedCustomerId,
                createdBatchId.Value,
                managedGoodsUnitId,
                saleOrderDetailId,
                saleQuantity,
                saleUnitPrice,
                saleOutRemark);
            deliveryTaskId = task.Id;
            saleOutOrderId = task.StockOutOrderId;

            using (var assign = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [task.Id],
                           DriverId = managedDriverId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
            }

            using (var start = await adminClient.PutAsync($"/api/delivery-tasks/{task.Id}/start", null))
            {
                Assert.Equal(HttpStatusCode.OK, start.StatusCode);
            }

            using (var sign = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task.Id}/sign",
                       BuildSignPayload(contactName, stockOutDetailId, saleQuantity, signRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, sign.StatusCode);
                var receipt = await ReadApiDataAsync<OrderReceiptDto>(sign);
                receiptId = receipt.Id;
                registry.Register<OrderReceipt>(receipt.Id, nameof(OrderReceipt.SignRemark), signRemark);
            }

            await using (var afterSign = fixture.CreateDbContext())
            {
                var bill = await afterSign.CustomerBills.AsNoTracking()
                    .SingleAsync(item => item.SaleOrderId == saleOrder.Id);
                customerBillId = bill.Id;
            }

            var afterSaleWindowStart = DateTime.UtcNow.AddSeconds(-2);

            AfterSaleDto refundDraft;
            using (var createRefund = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetailId,
                           refundQuantity,
                           AfterSaleType.RefundOnly,
                           AfterSaleHandleType.GoodsDiscount,
                           source,
                           contactName,
                           contactPhone,
                           pickupAddress,
                           refundRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createRefund.StatusCode);
                refundDraft = await ReadApiDataAsync<AfterSaleDto>(createRefund);
                refundAfterSaleId = refundDraft.Id;
                registry.Register<AfterSale>(refundDraft.Id, nameof(AfterSale.Remark), refundRemark);
            }

            using (var submitRefund = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/submit",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-提交" }))
            {
                Assert.Equal(HttpStatusCode.OK, submitRefund.StatusCode);
            }

            using (var approveRefund = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-同意" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveRefund.StatusCode);
            }

            using (var beforeCompleteReport = await adminClient.GetAsync(
                       BuildAfterSaleUrl(afterSaleWindowStart, DateTime.UtcNow.AddMinutes(1), managedCustomerId)))
            {
                Assert.Equal(HttpStatusCode.OK, beforeCompleteReport.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<AfterSaleSummaryDto>>(beforeCompleteReport);
                Assert.DoesNotContain(
                    page.Records ?? [],
                    item => item.HandleType == AfterSaleHandleType.GoodsDiscount
                            && item.RefundBaseQuantity == refundQuantity
                            && item.RefundAmount == expectedRefundAmount);
            }

            using (var completeRefund = await adminClient.PostAsync(
                       $"/api/after-sales/{refundDraft.Id}/complete",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, completeRefund.StatusCode);
                var completed = await ReadApiDataAsync<AfterSaleDto>(completeRefund);
                Assert.Equal(AfterSaleStatus.Completed, completed.AfterStatus);
            }

            AfterSaleDto incompleteDraft;
            using (var createDraftAfterSale = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetailId,
                           replenishQuantity,
                           AfterSaleType.RefundOnly,
                           AfterSaleHandleType.GoodsDiscount,
                           source,
                           contactName,
                           contactPhone,
                           pickupAddress,
                           draftAfterSaleRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createDraftAfterSale.StatusCode);
                incompleteDraft = await ReadApiDataAsync<AfterSaleDto>(createDraftAfterSale);
                draftAfterSaleId = incompleteDraft.Id;
                registry.Register<AfterSale>(incompleteDraft.Id, nameof(AfterSale.Remark), draftAfterSaleRemark);
                Assert.Equal(AfterSaleStatus.Draft, incompleteDraft.AfterStatus);
            }

            AfterSaleDto replenishDraft;
            using (var createReplenish = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetailId,
                           replenishQuantity,
                           AfterSaleType.RefundOnly,
                           AfterSaleHandleType.Replenishment,
                           source,
                           contactName,
                           contactPhone,
                           pickupAddress,
                           replenishRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createReplenish.StatusCode);
                replenishDraft = await ReadApiDataAsync<AfterSaleDto>(createReplenish);
                replenishAfterSaleId = replenishDraft.Id;
                registry.Register<AfterSale>(replenishDraft.Id, nameof(AfterSale.Remark), replenishRemark);
            }

            using (var submitReplenish = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{replenishDraft.Id}/submit",
                       new AfterSaleActionDto { Remark = $"{replenishRemark}-提交" }))
            {
                Assert.Equal(HttpStatusCode.OK, submitReplenish.StatusCode);
            }

            using (var approveReplenish = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{replenishDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{replenishRemark}-同意" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveReplenish.StatusCode);
            }

            using (var completeReplenish = await adminClient.PostAsync(
                       $"/api/after-sales/{replenishDraft.Id}/complete",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, completeReplenish.StatusCode);
                var completed = await ReadApiDataAsync<AfterSaleDto>(completeReplenish);
                Assert.Equal(AfterSaleStatus.Completed, completed.AfterStatus);
            }

            var afterSaleWindowEnd = DateTime.UtcNow.AddSeconds(2);

            await using (var expectContext = fixture.CreateDbContext())
            {
                var expectedAfterSaleRows = await expectContext.AfterSaleGoods
                    .AsNoTracking()
                    .Where(item => item.AfterSale.AfterStatus == AfterSaleStatus.Completed
                                   && item.AfterSale.CustomerId == managedCustomerId
                                   && item.AfterSale.CreateTime.HasValue
                                   && item.AfterSale.CreateTime.Value >= afterSaleWindowStart
                                   && item.AfterSale.CreateTime.Value <= afterSaleWindowEnd)
                    .GroupBy(item => new { item.AfterSaleType, item.ReasonType, item.HandleType })
                    .Select(group => new
                    {
                        group.Key.AfterSaleType,
                        group.Key.ReasonType,
                        group.Key.HandleType,
                        RefundBaseQuantity = group.Sum(item =>
                            item.HandleType == AfterSaleHandleType.Replenishment
                            || item.HandleType == AfterSaleHandleType.Exchange
                            || item.HandleType == AfterSaleHandleType.CustomerCommunication
                                ? 0m
                                : item.BaseRefundQuantity),
                        RefundAmount = group.Sum(item => item.RefundAmount),
                        AfterSaleCount = group.Select(item => item.AfterSaleId).Distinct().Count(),
                        CustomerCount = group.Select(item => item.AfterSale.CustomerId).Distinct().Count()
                    })
                    .ToListAsync();

                Assert.Contains(
                    expectedAfterSaleRows,
                    item => item.HandleType == AfterSaleHandleType.GoodsDiscount
                            && item.RefundBaseQuantity == refundQuantity
                            && item.RefundAmount == expectedRefundAmount
                            && item.AfterSaleCount == 1);
                Assert.Contains(
                    expectedAfterSaleRows,
                    item => item.HandleType == AfterSaleHandleType.Replenishment
                            && item.RefundBaseQuantity == 0m
                            && item.RefundAmount == 0m
                            && item.AfterSaleCount == 1);
                Assert.DoesNotContain(
                    expectedAfterSaleRows,
                    item => item.HandleType == AfterSaleHandleType.GoodsDiscount
                            && item.AfterSaleCount > 1);

                using (var afterSaleSummary = await adminClient.GetAsync(
                           BuildAfterSaleUrl(afterSaleWindowStart, afterSaleWindowEnd, managedCustomerId)))
                {
                    Assert.Equal(HttpStatusCode.OK, afterSaleSummary.StatusCode);
                    var page = await ReadApiDataAsync<PagedResult<AfterSaleSummaryDto>>(afterSaleSummary);
                    Assert.Equal(expectedAfterSaleRows.Count, page.Total);
                    foreach (var expected in expectedAfterSaleRows)
                    {
                        var actual = Assert.Single(
                            page.Records!,
                            item => item.AfterSaleType == expected.AfterSaleType
                                    && item.ReasonType == expected.ReasonType
                                    && item.HandleType == expected.HandleType);
                        Assert.Equal(NumericPrecision.RoundQuantity(expected.RefundBaseQuantity), actual.RefundBaseQuantity);
                        Assert.Equal(NumericPrecision.RoundMoney(expected.RefundAmount), actual.RefundAmount);
                        Assert.Equal(expected.AfterSaleCount, actual.AfterSaleCount);
                        Assert.Equal(expected.CustomerCount, actual.CustomerCount);
                    }
                }

                var expectedInDetails = await expectContext.StockInDetails.AsNoTracking()
                    .Where(item => item.StockInOrder.BusinessStatus == StockDocumentStatus.Audited
                                   && item.StockInOrder.WareId == managedWareId
                                   && item.GoodsId == managedGoodsId
                                   && item.StockInOrder.InTime >= reportDateStart
                                   && item.StockInOrder.InTime <= reportDateEnd)
                    .ToListAsync();
                var expectedOutDetails = await expectContext.StockOutDetails.AsNoTracking()
                    .Where(item => item.StockOutOrder.BusinessStatus == StockDocumentStatus.Audited
                                   && item.StockOutOrder.WareId == managedWareId
                                   && item.GoodsId == managedGoodsId
                                   && item.StockOutOrder.OutTime >= reportDateStart
                                   && item.StockOutOrder.OutTime <= reportDateEnd)
                    .ToListAsync();

                Assert.Contains(expectedInDetails, item => item.StockInOrderId == purchaseInOrderId);
                Assert.DoesNotContain(expectedInDetails, item => item.StockInOrderId == draftOtherInOrderId);
                Assert.Contains(expectedOutDetails, item => item.StockOutOrderId == purchaseReturnOrderId);
                Assert.Contains(expectedOutDetails, item => item.StockOutOrderId == saleOutOrderId);

                var independentStockInQty = NumericPrecision.RoundQuantity(expectedInDetails.Sum(x => x.BaseQuantity));
                var independentStockInAmount = NumericPrecision.RoundMoney(expectedInDetails.Sum(x => x.TotalPrice));
                var independentStockOutQty = NumericPrecision.RoundQuantity(expectedOutDetails.Sum(x => x.BaseQuantity));
                var independentStockOutAmount = NumericPrecision.RoundMoney(expectedOutDetails.Sum(x => x.TotalPrice));
                Assert.True(independentStockInQty >= expectedStockInQuantity);
                Assert.True(independentStockOutQty >= expectedStockOutQuantity);

                using (var dailyStock = await adminClient.GetAsync(
                           BuildDailyStockUrl(reportDateStart, reportDateEnd, managedWareId, managedGoodsId)))
                {
                    Assert.Equal(HttpStatusCode.OK, dailyStock.StatusCode);
                    var page = await ReadApiDataAsync<PagedResult<DailyStockInOutSummaryDto>>(dailyStock);
                    var item = Assert.Single(page.Records!);
                    Assert.Equal(expectedReportDate, item.ReportDate);
                    Assert.Equal(independentStockInQty, item.InBaseQuantity);
                    Assert.Equal(independentStockInAmount, item.InAmount);
                    Assert.Equal(independentStockOutQty, item.OutBaseQuantity);
                    Assert.Equal(independentStockOutAmount, item.OutAmount);
                    Assert.Equal(expectedInDetails.Select(x => x.StockInOrderId).Distinct().Count(), item.InOrderCount);
                    Assert.Equal(expectedOutDetails.Select(x => x.StockOutOrderId).Distinct().Count(), item.OutOrderCount);
                }

                using (var dailyGoods = await adminClient.GetAsync(
                           BuildDailyGoodsStockUrl(reportDateStart, reportDateEnd, managedWareId, managedGoodsId)))
                {
                    Assert.Equal(HttpStatusCode.OK, dailyGoods.StatusCode);
                    var page = await ReadApiDataAsync<PagedResult<DailyGoodsStockInOutSummaryDto>>(dailyGoods);
                    var item = Assert.Single(page.Records!);
                    Assert.Equal(expectedReportDate, item.ReportDate);
                    Assert.Equal(managedGoodsId, item.GoodsId);
                    Assert.Equal(managedGoodsName, item.GoodsName);
                    Assert.Equal(managedGoodsCode, item.GoodsCode);
                    Assert.Equal(managedBaseUnitName, item.BaseUnitName);
                    Assert.Equal(independentStockInQty, item.InBaseQuantity);
                    Assert.Equal(independentStockInAmount, item.InAmount);
                    Assert.Equal(independentStockOutQty, item.OutBaseQuantity);
                    Assert.Equal(independentStockOutAmount, item.OutAmount);
                }

                var expectedPurchaseIn = await expectContext.StockInDetails.AsNoTracking()
                    .Where(item => item.StockInOrder.BusinessStatus == StockDocumentStatus.Audited
                                   && item.StockInOrder.OrderType == StockInOrderType.Purchase
                                   && item.StockInOrder.SupplierId == managedSupplierId
                                   && item.StockInOrder.PurchaserId == managedPurchaserId
                                   && item.GoodsId == managedGoodsId
                                   && item.StockInOrder.InTime >= reportDateStart
                                   && item.StockInOrder.InTime <= reportDateEnd)
                    .ToListAsync();
                var expectedPurchaseOut = await expectContext.StockOutDetails.AsNoTracking()
                    .Where(item => item.StockOutOrder.BusinessStatus == StockDocumentStatus.Audited
                                   && item.StockOutOrder.OrderType == StockOutOrderType.PurchaseReturn
                                   && item.StockOutOrder.SupplierId == managedSupplierId
                                   && item.GoodsId == managedGoodsId
                                   && item.StockOutOrder.OutTime >= reportDateStart
                                   && item.StockOutOrder.OutTime <= reportDateEnd)
                    .ToListAsync();

                Assert.Contains(expectedPurchaseIn, item => item.StockInOrderId == purchaseInOrderId);
                Assert.Contains(expectedPurchaseOut, item => item.StockOutOrderId == purchaseReturnOrderId);
                Assert.DoesNotContain(expectedPurchaseOut, item => item.StockOutOrderId == saleOutOrderId);

                var independentPurchaseInQty = NumericPrecision.RoundQuantity(expectedPurchaseIn.Sum(x => x.BaseQuantity));
                var independentPurchaseInAmount = NumericPrecision.RoundMoney(expectedPurchaseIn.Sum(x => x.TotalPrice));
                var independentPurchaseOutQty = NumericPrecision.RoundQuantity(expectedPurchaseOut.Sum(x => x.BaseQuantity));
                var independentPurchaseOutAmount = NumericPrecision.RoundMoney(expectedPurchaseOut.Sum(x => x.TotalPrice));
                var independentPurchaseNet = NumericPrecision.RoundMoney(
                    independentPurchaseInAmount - independentPurchaseOutAmount);
                Assert.True(independentPurchaseInQty >= purchaseInQuantity);
                Assert.True(independentPurchaseOutQty >= purchaseReturnQuantity);

                using (var purchaseGoods = await adminClient.GetAsync(
                           BuildPurchaseGoodsUrl(
                               reportDateStart,
                               reportDateEnd,
                               managedWareId,
                               managedSupplierId,
                               managedPurchaserId,
                               managedGoodsId)))
                {
                    Assert.Equal(HttpStatusCode.OK, purchaseGoods.StatusCode);
                    var page = await ReadApiDataAsync<PagedResult<PurchaseInOutGoodsSummaryDto>>(purchaseGoods);
                    var item = Assert.Single(page.Records!);
                    Assert.Equal(managedGoodsId, item.GoodsId);
                    Assert.Equal(independentPurchaseInQty, item.InBaseQuantity);
                    Assert.Equal(independentPurchaseInAmount, item.InAmount);
                    Assert.Equal(independentPurchaseOutQty, item.OutBaseQuantity);
                    Assert.Equal(independentPurchaseOutAmount, item.OutAmount);
                    Assert.Equal(independentPurchaseNet, item.NetAmount);
                }

                using (var purchaseSuppliers = await adminClient.GetAsync(
                           BuildPurchaseSuppliersUrl(
                               reportDateStart,
                               reportDateEnd,
                               managedWareId,
                               managedSupplierId,
                               managedPurchaserId,
                               managedGoodsId)))
                {
                    Assert.Equal(HttpStatusCode.OK, purchaseSuppliers.StatusCode);
                    var page = await ReadApiDataAsync<PagedResult<PurchaseInOutSupplierSummaryDto>>(purchaseSuppliers);
                    var item = Assert.Single(page.Records!);
                    Assert.Equal(managedSupplierId, item.SupplierId);
                    Assert.Equal(managedSupplierName, item.SupplierName);
                    Assert.Equal(independentPurchaseInQty, item.InBaseQuantity);
                    Assert.Equal(independentPurchaseInAmount, item.InAmount);
                    Assert.Equal(independentPurchaseOutQty, item.OutBaseQuantity);
                    Assert.Equal(independentPurchaseOutAmount, item.OutAmount);
                    Assert.Equal(independentPurchaseNet, item.NetAmount);
                }

                using (var purchasePurchasers = await adminClient.GetAsync(
                           BuildPurchasePurchasersUrl(
                               reportDateStart,
                               reportDateEnd,
                               managedWareId,
                               managedSupplierId,
                               managedPurchaserId,
                               managedGoodsId)))
                {
                    Assert.Equal(HttpStatusCode.OK, purchasePurchasers.StatusCode);
                    var page = await ReadApiDataAsync<PagedResult<PurchaseInOutPurchaserSummaryDto>>(purchasePurchasers);
                    var item = Assert.Single(page.Records!);
                    Assert.Equal(managedPurchaserId, item.PurchaserId);
                    Assert.Equal(managedPurchaserName, item.PurchaserName);
                    Assert.Equal(independentPurchaseInQty, item.InBaseQuantity);
                    Assert.Equal(independentPurchaseInAmount, item.InAmount);
                    Assert.Equal(independentPurchaseOutQty, item.OutBaseQuantity);
                    Assert.Equal(independentPurchaseOutAmount, item.OutAmount);
                    Assert.Equal(independentPurchaseNet, item.NetAmount);
                }
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
                Assert.Contains(financeReadPermission, info.Buttons);
                Assert.DoesNotContain(reportsReadPermission, info.Buttons);
            }

            using (var deniedAfterSale = await limitedClient.GetAsync(
                       BuildAfterSaleUrl(afterSaleWindowStart, afterSaleWindowEnd, managedCustomerId)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAfterSale, ResponseCode.Forbidden);
            }

            using (var deniedStock = await limitedClient.GetAsync(
                       BuildDailyStockUrl(reportDateStart, reportDateEnd, managedWareId, managedGoodsId)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedStock, ResponseCode.Forbidden);
            }

            using (var deniedPurchase = await limitedClient.GetAsync(
                       BuildPurchaseGoodsUrl(
                           reportDateStart,
                           reportDateEnd,
                           managedWareId,
                           managedSupplierId,
                           managedPurchaserId,
                           managedGoodsId)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedPurchase, ResponseCode.Forbidden);
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
            Assert.True(await auditContext.Purchasers.AnyAsync(item => item.Id == managedPurchaserId));
            Assert.True(await auditContext.Drivers.AnyAsync(item => item.Id == managedDriverId));
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

                var residualCustomerBillIds = new List<Guid>();
                if (customerBillId.HasValue)
                    residualCustomerBillIds.Add(customerBillId.Value);
                var residualCustomerBills = await cleanupContext.CustomerBills
                    .Where(item => residualCustomerBillIds.Contains(item.Id)
                                   || residualSaleOrderIds.Contains(item.SaleOrderId))
                    .ToListAsync();
                if (residualCustomerBills.Count > 0)
                {
                    cleanupContext.CustomerBills.RemoveRange(residualCustomerBills);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualAfterSaleIds = new List<Guid>();
                if (refundAfterSaleId.HasValue)
                    residualAfterSaleIds.Add(refundAfterSaleId.Value);
                if (draftAfterSaleId.HasValue)
                    residualAfterSaleIds.Add(draftAfterSaleId.Value);
                if (replenishAfterSaleId.HasValue)
                    residualAfterSaleIds.Add(replenishAfterSaleId.Value);
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

                var residualSupplierBillIds = new List<Guid>();
                if (inboundBillId.HasValue)
                    residualSupplierBillIds.Add(inboundBillId.Value);
                if (returnBillId.HasValue)
                    residualSupplierBillIds.Add(returnBillId.Value);
                var residualSupplierBills = await cleanupContext.SupplierBills
                    .Where(item => residualSupplierBillIds.Contains(item.Id)
                                   || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value)
                                   || (purchaseReturnOrderId.HasValue
                                       && item.StockOutOrderId == purchaseReturnOrderId.Value))
                    .ToListAsync();
                if (residualSupplierBills.Count > 0)
                {
                    cleanupContext.SupplierBills.RemoveRange(residualSupplierBills);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualReceipts = await cleanupContext.OrderReceipts
                    .Where(item => (receiptId.HasValue && item.Id == receiptId.Value)
                                   || (item.SignRemark != null && item.SignRemark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualReceipts.Count > 0)
                {
                    cleanupContext.OrderReceipts.RemoveRange(residualReceipts);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualTasks = await cleanupContext.DeliveryTasks
                    .Where(item => (deliveryTaskId.HasValue && item.Id == deliveryTaskId.Value)
                                   || (item.Remark != null
                                       && (item.Remark == saleOutRemark || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualTasks.Count > 0)
                {
                    cleanupContext.DeliveryTasks.RemoveRange(residualTasks);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => (saleOutOrderId.HasValue && item.Id == saleOutOrderId.Value)
                                   || (purchaseReturnOrderId.HasValue && item.Id == purchaseReturnOrderId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => (purchaseInOrderId.HasValue && item.Id == purchaseInOrderId.Value)
                                   || (draftOtherInOrderId.HasValue && item.Id == draftOtherInOrderId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
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
                    .Where(item => item.BatchNo == purchaseBatchNo
                                   || item.BatchNo == draftBatchNo
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
                    .Where(relation => relation.RoleId == limitedRoleId || relation.MenuId == seedMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoles = await cleanupContext.Roles
                    .Where(role => role.Id == limitedRoleId || role.Code == limitedRoleCode)
                    .ToListAsync();
                if (residualRoles.Count > 0)
                {
                    cleanupContext.Roles.RemoveRange(residualRoles);
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
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.CustomerBills.AnyAsync(item =>
                (customerBillId.HasValue && item.Id == customerBillId.Value)
                || (saleOrderId.HasValue && item.SaleOrderId == saleOrderId.Value)));
            Assert.False(await residualContext.SupplierBills.AnyAsync(item =>
                (inboundBillId.HasValue && item.Id == inboundBillId.Value)
                || (returnBillId.HasValue && item.Id == returnBillId.Value)
                || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value)
                || (purchaseReturnOrderId.HasValue && item.StockOutOrderId == purchaseReturnOrderId.Value)));
            Assert.False(await residualContext.AfterSales.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.OrderReceipts.AnyAsync(item =>
                item.SignRemark != null && item.SignRemark.StartsWith(batch.Id)));
            Assert.False(await residualContext.DeliveryTasks.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockOutOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockInOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockBatches.AnyAsync(item =>
                item.BatchNo == purchaseBatchNo || item.BatchNo == draftBatchNo));
            Assert.False(await residualContext.SaleOrders.AnyAsync(item =>
                item.InnerRemark != null && item.InnerRemark.StartsWith(batch.Id)));
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername
                || user.Username == limitedUsername
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId || role.Code == limitedRoleCode));
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
            Assert.True(await residualContext.Purchasers.AnyAsync(item => item.Id == managedPurchaserId));
            Assert.True(await residualContext.Drivers.AnyAsync(item => item.Id == managedDriverId));
        }
    }

    private static string BuildAfterSaleUrl(DateTime dateStart, DateTime dateEnd, Guid customerId)
    {
        return $"/api/reports/after-sales?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&customerId={customerId}";
    }

    private static string BuildDailyStockUrl(DateTime dateStart, DateTime dateEnd, Guid wareId, Guid goodsId)
    {
        return $"/api/reports/stock/daily?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&wareId={wareId}&goodsIds[0]={goodsId}";
    }

    private static string BuildDailyGoodsStockUrl(DateTime dateStart, DateTime dateEnd, Guid wareId, Guid goodsId)
    {
        return $"/api/reports/stock/daily-goods?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&wareId={wareId}&goodsIds[0]={goodsId}";
    }

    private static string BuildPurchaseGoodsUrl(
        DateTime dateStart,
        DateTime dateEnd,
        Guid wareId,
        Guid supplierId,
        Guid purchaserId,
        Guid goodsId)
    {
        return $"/api/reports/purchase-in-out/goods?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&wareId={wareId}&supplierId={supplierId}&purchaserId={purchaserId}&goodsIds[0]={goodsId}";
    }

    private static string BuildPurchaseSuppliersUrl(
        DateTime dateStart,
        DateTime dateEnd,
        Guid wareId,
        Guid supplierId,
        Guid purchaserId,
        Guid goodsId)
    {
        return $"/api/reports/purchase-in-out/suppliers?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&wareId={wareId}&supplierId={supplierId}&purchaserId={purchaserId}&goodsIds[0]={goodsId}";
    }

    private static string BuildPurchasePurchasersUrl(
        DateTime dateStart,
        DateTime dateEnd,
        Guid wareId,
        Guid supplierId,
        Guid purchaserId,
        Guid goodsId)
    {
        return $"/api/reports/purchase-in-out/purchasers?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&wareId={wareId}&supplierId={supplierId}&purchaserId={purchaserId}&goodsIds[0]={goodsId}";
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

    private static object BuildCreatePurchaseInPayload(
        Guid wareId,
        Guid supplierId,
        Guid purchaserId,
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
            purchaserId,
            purchasePattern = PurchasePattern.SupplierDirect,
            inTime = "2026-04-15T09:00:00Z",
            expectedArrivalTime = "2026-04-15T15:00:00Z",
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
                    productDate = "2026-04-14",
                    remark = "T11采购入库明细"
                }
            }
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
            inTime = "2026-04-15T09:30:00Z",
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
                    remark = "T11草稿其他入库明细"
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
            outTime = "2026-04-15T11:00:00Z",
            remark,
            details = new[]
            {
                new
                {
                    stockBatchId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    remark = "T11采购退货出库明细"
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
            outTime = "2026-04-15T12:00:00Z",
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
                    remark = "T11销售出库明细"
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
