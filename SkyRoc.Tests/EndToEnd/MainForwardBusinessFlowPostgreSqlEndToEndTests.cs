using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Delivery;
using Application.DTOs.Finance;
using Application.DTOs.Orders;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Domain.Entities;
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
///     T13 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证主正向全链路
///     （销售订单→采购计划→采购单→采购入库→销售出库→配送签收→客户结款）。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class MainForwardBusinessFlowPostgreSqlEndToEndTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     主正向链路落库副作用→重复生成计划/重复完成采购单/重复签收拒绝→
    ///     401/403（报表相邻权限无订单/采购/配送/财务写）权限矩阵；临时批次精确清理。
    /// </summary>
    [Fact]
    public async Task MainForwardBusinessFlow_OrderPurchaseStockDeliverySettlementAndPermissionMatrix_OnPostgreSql()
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
        var saleOrderInnerRemark = $"{batch.Id}O";
        var planGenerateRemark = $"{batch.Id}PG";
        var poRemark = $"{batch.Id}PO";
        var purchaseInRemark = $"{batch.Id}-采购入库";
        var saleOutRemark = $"{batch.Id}-销售出库";
        var signRemark = $"{batch.Id}-签收结款";
        var returnRemark = $"{batch.Id}-回单归档";
        var settlementSerialNo = $"{batch.Id}-BANK";
        var settlementRemark = $"{batch.Id}-全额结款";
        var batchNo = $"{batch.Id}-BATCH";
        var receiptImageUrl = $"https://assets.skyroc.example/autotest/{batch.Id}/receipt.pdf";
        var password = "SkyRocT13MainForward!2026";
        var userAgent = $"SkyRoc-T13-MainForward/{batch.Id}";
        var createName = "T13-MainForwardE2E";
        var contactName = "全链路王老师";
        var contactPhone = "13900001301";
        var deliveryAddress = "上海市浦东新区主正向全链路路 13 号食堂西门";

        var reportReadPermission = PermissionCodes.Business.Reports.Read;

        var orderQuantity = NumericPrecision.RoundQuantity(6m);
        var saleUnitPrice = NumericPrecision.RoundMoney(4.5m);
        var purchaseUnitPrice = NumericPrecision.RoundMoney(3.2m);
        var expectedSettlement = NumericPrecision.RoundMoney(orderQuantity * saleUnitPrice);
        var expectedPayable = NumericPrecision.RoundMoney(orderQuantity * purchaseUnitPrice);

        Guid adminRoleId;
        Guid managedCustomerId;
        Guid managedGoodsId;
        Guid managedGoodsUnitId;
        Guid managedWareId;
        Guid managedSupplierId;
        Guid managedPurchaserId;
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

            var purchaser = await seedContext.Purchasers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == purchaserCode);
            Assert.NotNull(purchaser);
            managedPurchaserId = purchaser.Id;

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
                Desc = "T13 主正向全链路最小权限临时角色，仅持报表读取",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T13主正向操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001311",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T13主正向报表只读",
                    Gender = GenderType.Female,
                    Phone = "13900001312",
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
                Title = "T13主正向报表只读菜单",
                Component = "page.t13.mainforward.seed",
                MenuType = MenuType.Menu,
                Order = 9131,
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

        Guid? saleOrderId = null;
        Guid? purchasePlanId = null;
        Guid? purchaseOrderId = null;
        Guid? purchaseInOrderId = null;
        Guid? saleOutOrderId = null;
        Guid? deliveryTaskId = null;
        Guid? receiptId = null;
        Guid? customerBillId = null;
        Guid? supplierBillId = null;
        Guid? settlementId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousOrder = await anonymousClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-20T08:00:00Z",
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = 1m
                               }
                           }
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousOrder, ResponseCode.Unauthorized);
            }

            using (var anonymousPlan = await anonymousClient.PostAsJsonAsync(
                       "/api/purchase-plans/generate",
                       new GeneratePurchasePlanFromOrdersDto { OrderIds = [Guid.NewGuid()] }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousPlan, ResponseCode.Unauthorized);
            }

            using (var anonymousSettlement = await anonymousClient.PostAsJsonAsync(
                       "/api/customer-settlements",
                       new CreateCustomerSettlementDto
                       {
                           Details =
                           [
                               new CreateCustomerSettlementDetailDto
                               {
                                   CustomerBillId = Guid.NewGuid(),
                                   PaymentAmount = 1m
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousSettlement, ResponseCode.Unauthorized);
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
                           remark = "T13主正向全链路销售订单",
                           innerRemark = saleOrderInnerRemark,
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = orderQuantity,
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
                Assert.Equal(SaleOrderStatus.PendingAudit, saleOrder.OrderStatus);
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

            List<PurchasePlanDto> plans;
            using (var generatePlans = await adminClient.PostAsJsonAsync(
                       "/api/purchase-plans/generate",
                       new GeneratePurchasePlanFromOrdersDto
                       {
                           OrderIds = [saleOrder.Id],
                           Remark = planGenerateRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, generatePlans.StatusCode);
                plans = await ReadApiDataAsync<List<PurchasePlanDto>>(generatePlans);
            }

            var plan = Assert.Single(plans);
            purchasePlanId = plan.Id;
            registry.Register<PurchasePlan>(plan.Id, nameof(PurchasePlan.Remark), planGenerateRemark);
            var planDetail = Assert.Single(plan.Details);
            Assert.Equal(orderQuantity, planDetail.RequiredQuantity);
            Assert.Equal(saleOrder.Id, Assert.Single(planDetail.OrderRelations).SaleOrderId);

            using (var duplicatePlan = await adminClient.PostAsJsonAsync(
                       "/api/purchase-plans/generate",
                       new GeneratePurchasePlanFromOrdersDto
                       {
                           OrderIds = [saleOrder.Id],
                           Remark = planGenerateRemark
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(duplicatePlan, ResponseCode.DatabaseError);
            }

            using (var assignSupplier = await adminClient.PutAsJsonAsync(
                       "/api/purchase-plans/supplier",
                       new AssignPurchasePlanSupplierDto
                       {
                           PlanIds = [plan.Id],
                           SupplierId = managedSupplierId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignSupplier.StatusCode);
            }

            using (var assignPurchaser = await adminClient.PutAsJsonAsync(
                       "/api/purchase-plans/purchaser",
                       new AssignPurchasePlanPurchaserDto
                       {
                           PlanIds = [plan.Id],
                           PurchaserId = managedPurchaserId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignPurchaser.StatusCode);
            }

            PurchaseOrderDto purchaseOrder;
            using (var generatePo = await adminClient.PostAsJsonAsync(
                       "/api/purchase-orders/generate-from-plans",
                       new GeneratePurchaseOrdersFromPlansDto
                       {
                           PlanIds = [plan.Id],
                           ReceiveTime = new DateTime(2026, 7, 21, 6, 0, 0, DateTimeKind.Utc),
                           Remark = poRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, generatePo.StatusCode);
                purchaseOrder = Assert.Single(await ReadApiDataAsync<List<PurchaseOrderDto>>(generatePo));
                purchaseOrderId = purchaseOrder.Id;
                registry.Register<PurchaseOrder>(purchaseOrder.Id, nameof(PurchaseOrder.Remark), poRemark);
            }

            var purchaseOrderDetailId = Assert.Single(purchaseOrder.Details).Id;

            using (var completePo = await adminClient.PostAsync(
                       $"/api/purchase-orders/{purchaseOrder.Id}/complete",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, completePo.StatusCode);
                purchaseOrder = await ReadApiDataAsync<PurchaseOrderDto>(completePo);
                Assert.Equal(PurchaseOrderStatus.Completed, purchaseOrder.BusinessStatus);
            }

            using (var duplicateComplete = await adminClient.PostAsync(
                       $"/api/purchase-orders/{purchaseOrder.Id}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(duplicateComplete, ResponseCode.DatabaseError);
            }

            StockInOrderDto purchaseIn;
            using (var createPurchaseIn = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/purchase",
                       new CreatePurchaseStockInDto
                       {
                           WareId = managedWareId,
                           PurchaseOrderId = purchaseOrder.Id,
                           SupplierId = managedSupplierId,
                           PurchaserId = managedPurchaserId,
                           PurchasePattern = PurchasePattern.SupplierDirect,
                           InTime = new DateTime(2026, 7, 21, 7, 0, 0, DateTimeKind.Utc),
                           Remark = purchaseInRemark,
                           Details =
                           [
                               new CreateStockInDetailDto
                               {
                                   PurchaseOrderDetailId = purchaseOrderDetailId,
                                   GoodsId = managedGoodsId,
                                   GoodsUnitId = managedGoodsUnitId,
                                   Quantity = orderQuantity,
                                   UnitPrice = purchaseUnitPrice,
                                   ProductDate = new DateOnly(2026, 7, 20)
                               }
                           ]
                       }))
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
                var stockBatch = await afterPurchaseIn.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == batchNo);
                createdBatchId = stockBatch.Id;
                Assert.Equal(orderQuantity, stockBatch.AvailableQuantity);
                Assert.Equal(orderQuantity, stockBatch.CurrentQuantity);

                var supplierBill = await afterPurchaseIn.SupplierBills.AsNoTracking()
                    .SingleAsync(item => item.StockInOrderId == purchaseIn.Id);
                supplierBillId = supplierBill.Id;
                // 应付单 BillNo/来源单号不含批次前缀，不能登记到 BatchCleanupRegistry；finally 中先于入库单精确删除。
                Assert.Equal(expectedPayable, supplierBill.PayableAmount);
                Assert.Equal(SupplierBillStatus.Pending, supplierBill.BillStatus);
            }

            StockOutOrderDto saleOut;
            using (var createSaleOut = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/sale",
                       new
                       {
                           wareId = managedWareId,
                           saleOrderId = saleOrder.Id,
                           customerId = managedCustomerId,
                           outTime = "2026-07-21T08:00:00Z",
                           remark = saleOutRemark,
                           details = new[]
                           {
                               new
                               {
                                   saleOrderDetailId,
                                   stockBatchId = createdBatchId!.Value,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = orderQuantity,
                                   unitPrice = saleUnitPrice,
                                   remark = "T13销售出库明细"
                               }
                           }
                       }))
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
                Assert.Equal(DeliveryTaskStatus.PendingAssign, deliveryTask.DeliveryStatus);
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
                deliveryTask = Assert.Single(await ReadApiDataAsync<List<DeliveryTaskDto>>(assignDriver));
                Assert.Equal(DeliveryTaskStatus.Assigned, deliveryTask.DeliveryStatus);
            }

            using (var startTask = await adminClient.PutAsync(
                       $"/api/delivery-tasks/{deliveryTask.Id}/start",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, startTask.StatusCode);
                deliveryTask = await ReadApiDataAsync<DeliveryTaskDto>(startTask);
                Assert.Equal(DeliveryTaskStatus.Delivering, deliveryTask.DeliveryStatus);
            }

            OrderReceiptDto receipt;
            using (var sign = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{deliveryTask.Id}/sign",
                       new SignDeliveryTaskDto
                       {
                           SignerName = contactName,
                           Remark = signRemark,
                           Details =
                           [
                               new SignDeliveryCheckDetailDto
                               {
                                   StockOutDetailId = stockOutDetailId,
                                   AcceptedBaseQuantity = orderQuantity,
                                   CheckStatus = OrderCustomerCheckStatus.Accepted
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, sign.StatusCode);
                receipt = await ReadApiDataAsync<OrderReceiptDto>(sign);
                receiptId = receipt.Id;
                registry.Register<OrderReceipt>(receipt.Id, nameof(OrderReceipt.SignRemark), signRemark);
            }

            using (var duplicateSign = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{deliveryTask.Id}/sign",
                       new SignDeliveryTaskDto
                       {
                           SignerName = contactName,
                           Remark = $"{signRemark}-重复",
                           Details =
                           [
                               new SignDeliveryCheckDetailDto
                               {
                                   StockOutDetailId = stockOutDetailId,
                                   AcceptedBaseQuantity = orderQuantity,
                                   CheckStatus = OrderCustomerCheckStatus.Accepted
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(duplicateSign, ResponseCode.DatabaseError);
            }

            using (var returnReceipt = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{deliveryTask.Id}/receipt",
                       new ReturnOrderReceiptDto
                       {
                           ReceiptImageUrl = receiptImageUrl,
                           Remark = returnRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, returnReceipt.StatusCode);
                receipt = await ReadApiDataAsync<OrderReceiptDto>(returnReceipt);
                Assert.NotNull(receipt.ReturnedTime);
            }

            await using (var afterSign = fixture.CreateDbContext())
            {
                var persistedOrder = await afterSign.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == saleOrder.Id);
                Assert.Equal(SaleOrderStatus.Signed, persistedOrder.OrderStatus);
                Assert.Equal(OrderReturnStatus.Returned, persistedOrder.ReturnStatus);
                Assert.Equal(expectedSettlement, persistedOrder.SettlementPrice);

                var stockBatch = await afterSign.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId.Value);
                Assert.Equal(0m, stockBatch.CurrentQuantity);
                Assert.Equal(0m, stockBatch.AvailableQuantity);

                var ledgers = await afterSign.StockLedgers.AsNoTracking()
                    .Where(item => item.StockBatchId == createdBatchId.Value)
                    .OrderBy(item => item.OccurredTime)
                    .ThenBy(item => item.Id)
                    .ToListAsync();
                Assert.Equal(2, ledgers.Count);
                Assert.Equal(StockLedgerDirection.Increase, ledgers[0].Direction);
                Assert.Equal(orderQuantity, ledgers[0].ChangeQuantity);
                Assert.Equal(StockLedgerDirection.Decrease, ledgers[1].Direction);
                Assert.Equal(orderQuantity, ledgers[1].ChangeQuantity);
                Assert.Equal(0m, ledgers[1].BalanceQuantity);

                var customerBill = await afterSign.CustomerBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.SaleOrderId == saleOrder.Id);
                customerBillId = customerBill.Id;
                // 客户账单 BillNo 不含批次前缀，不能登记到 BatchCleanupRegistry；finally 中先于订单精确删除。
                Assert.Equal(expectedSettlement, customerBill.ReceivableAmount);
                Assert.Equal(0m, customerBill.SettledAmount);
                Assert.Equal(CustomerBillStatus.Pending, customerBill.BillStatus);
            }

            CustomerSettlementDto settlement;
            using (var createSettlement = await adminClient.PostAsJsonAsync(
                       "/api/customer-settlements",
                       new CreateCustomerSettlementDto
                       {
                           SettlementDate = new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc),
                           SerialNo = settlementSerialNo,
                           Remark = settlementRemark,
                           Details =
                           [
                               new CreateCustomerSettlementDetailDto
                               {
                                   CustomerBillId = customerBillId!.Value,
                                   PaymentAmount = expectedSettlement,
                                   DiscountAmount = 0m,
                                   Remark = $"{batch.Id}-结款明细"
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createSettlement.StatusCode);
                settlement = await ReadApiDataAsync<CustomerSettlementDto>(createSettlement);
                settlementId = settlement.Id;
                registry.Register<CustomerSettlement>(
                    settlement.Id,
                    nameof(CustomerSettlement.SerialNo),
                    settlementSerialNo);
            }

            Assert.Equal(CustomerSettlementStatus.Settled, settlement.SettlementStatus);
            Assert.Equal(expectedSettlement, settlement.ShouldAmount);
            Assert.Equal(expectedSettlement, settlement.PaymentAmount);
            Assert.Equal(0m, settlement.RemainingAmount);

            await using (var afterSettlement = fixture.CreateDbContext())
            {
                var bill = await afterSettlement.CustomerBills.AsNoTracking()
                    .SingleAsync(item => item.Id == customerBillId.Value);
                Assert.Equal(expectedSettlement, bill.SettledAmount);
                Assert.Equal(CustomerBillStatus.Settled, bill.BillStatus);
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

            using (var deniedOrder = await limitedClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-20T09:00:00Z",
                           contactName,
                           contactPhone,
                           deliveryAddress,
                           remark = "无权限拒绝",
                           innerRemark = $"{batch.Id}X",
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = 1m,
                                   fixedPrice = saleUnitPrice,
                                   fixedGoodsUnitId = managedGoodsUnitId
                               }
                           }
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedOrder, ResponseCode.Forbidden);
            }

            using (var deniedPlan = await limitedClient.PostAsJsonAsync(
                       "/api/purchase-plans/generate",
                       new GeneratePurchasePlanFromOrdersDto
                       {
                           OrderIds = [saleOrder.Id],
                           Remark = $"{batch.Id}PX"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedPlan, ResponseCode.Forbidden);
            }

            using (var deniedStart = await limitedClient.PutAsync(
                       $"/api/delivery-tasks/{deliveryTask.Id}/start",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedStart, ResponseCode.Forbidden);
            }

            using (var deniedSettlement = await limitedClient.PostAsJsonAsync(
                       "/api/customer-settlements",
                       new CreateCustomerSettlementDto
                       {
                           Details =
                           [
                               new CreateCustomerSettlementDetailDto
                               {
                                   CustomerBillId = customerBillId.Value,
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
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            // 账单/结款对出入库与订单为 Restrict：先于 BatchCleanupRegistry 按外键逆序精确删除。
            await using (var preCleanup = fixture.CreateDbContext())
            {
                var residualSettlements = await preCleanup.CustomerSettlements
                    .Where(item => (settlementId.HasValue && item.Id == settlementId.Value)
                                   || (item.SerialNo != null && item.SerialNo.StartsWith(batch.Id))
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualSettlements.Count > 0)
                {
                    preCleanup.CustomerSettlements.RemoveRange(residualSettlements);
                    await preCleanup.SaveChangesAsync();
                }

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
                    .Where(item => (supplierBillId.HasValue && item.Id == supplierBillId.Value)
                                   || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value))
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

                var residualTasks = await preCleanup.DeliveryTasks
                    .Where(item => (deliveryTaskId.HasValue && item.Id == deliveryTaskId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualTasks.Count > 0)
                {
                    preCleanup.DeliveryTasks.RemoveRange(residualTasks);
                    await preCleanup.SaveChangesAsync();
                }
            }

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => (saleOutOrderId.HasValue && item.Id == saleOutOrderId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => (purchaseInOrderId.HasValue && item.Id == purchaseInOrderId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                var residualOrderIdSet = residualStockOutOrders.Select(item => item.Id)
                    .Concat(residualStockInOrders.Select(item => item.Id))
                    .ToHashSet();

                var residualSupplierBills = await cleanupContext.SupplierBills
                    .Where(item => (supplierBillId.HasValue && item.Id == supplierBillId.Value)
                                   || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value)
                                   || (item.StockInOrderId.HasValue
                                       && residualOrderIdSet.Contains(item.StockInOrderId.Value)))
                    .ToListAsync();
                if (residualSupplierBills.Count > 0)
                {
                    cleanupContext.SupplierBills.RemoveRange(residualSupplierBills);
                    await cleanupContext.SaveChangesAsync();
                }

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
                                   || item.BatchNo.StartsWith(batch.Id)
                                   || (createdBatchId.HasValue && item.Id == createdBatchId.Value))
                    .ToListAsync();
                if (residualBatches.Count > 0)
                {
                    cleanupContext.StockBatches.RemoveRange(residualBatches);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualPurchaseOrders = await cleanupContext.PurchaseOrders
                    .Where(item => (purchaseOrderId.HasValue && item.Id == purchaseOrderId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualPurchaseOrders.Count > 0)
                {
                    cleanupContext.PurchaseOrders.RemoveRange(residualPurchaseOrders);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualPlans = await cleanupContext.PurchasePlans
                    .Where(item => (purchasePlanId.HasValue && item.Id == purchasePlanId.Value)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualPlans.Count > 0)
                {
                    cleanupContext.PurchasePlans.RemoveRange(residualPlans);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualSaleOrders = await cleanupContext.SaleOrders
                    .Where(item => (saleOrderId.HasValue && item.Id == saleOrderId.Value)
                                   || (item.InnerRemark != null && item.InnerRemark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualSaleOrders.Count > 0)
                {
                    var residualSaleOrderIds = residualSaleOrders.Select(item => item.Id).ToList();
                    var residualDetails = await cleanupContext.SaleOrderDetails
                        .Where(detail => residualSaleOrderIds.Contains(detail.SaleOrderId))
                        .ToListAsync();
                    if (residualDetails.Count > 0)
                    {
                        cleanupContext.SaleOrderDetails.RemoveRange(residualDetails);
                        await cleanupContext.SaveChangesAsync();
                    }

                    var residualAuditLogs = await cleanupContext.OrderAuditLogs
                        .Where(log => residualSaleOrderIds.Contains(log.SaleOrderId))
                        .ToListAsync();
                    if (residualAuditLogs.Count > 0)
                    {
                        cleanupContext.OrderAuditLogs.RemoveRange(residualAuditLogs);
                        await cleanupContext.SaveChangesAsync();
                    }

                    cleanupContext.SaleOrders.RemoveRange(residualSaleOrders);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };
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
                                   || (user.Username != null && user.Username.StartsWith(batch.Id))
                                   || user.CreateName == createName)
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
                    .Where(role => role.Id == limitedRoleId
                                   || role.Code == limitedRoleCode
                                   || (role.Code != null && role.Code.StartsWith(batch.Id))
                                   || role.CreateName == createName)
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
                    .Where(menu => menu.Id == seedMenuId
                                   || menu.Name == seedMenuName
                                   || menu.CreateName == createName)
                    .ToListAsync();
                if (residualMenus.Count > 0)
                {
                    cleanupContext.Menus.RemoveRange(residualMenus);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.CustomerSettlements.AnyAsync(item =>
                (settlementId.HasValue && item.Id == settlementId.Value)
                || (item.SerialNo != null && item.SerialNo.StartsWith(batch.Id))));
            Assert.False(await residualContext.CustomerBills.AnyAsync(item =>
                (customerBillId.HasValue && item.Id == customerBillId.Value)
                || (saleOrderId.HasValue && item.SaleOrderId == saleOrderId.Value)));
            Assert.False(await residualContext.SupplierBills.AnyAsync(item =>
                (supplierBillId.HasValue && item.Id == supplierBillId.Value)
                || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value)));
            Assert.False(await residualContext.OrderReceipts.AnyAsync(item =>
                item.SignRemark != null && item.SignRemark.StartsWith(batch.Id)));
            Assert.False(await residualContext.DeliveryTasks.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockOutOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockInOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockBatches.AnyAsync(item => item.BatchNo == batchNo));
            Assert.False(await residualContext.PurchaseOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.PurchasePlans.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
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
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(ResponseCode.Success, payload.Code);
        Assert.NotNull(payload.Data);
        return payload.Data;
    }
}
