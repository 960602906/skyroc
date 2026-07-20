using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Delivery;
using Application.DTOs.Finance;
using Application.DTOs.Orders;
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

namespace SkyRoc.Tests.Finance;

/// <summary>
///     T10 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证客户结款凭证的
///     部分/全部结款、优惠核销、超额拒绝、作废回滚、余额独立重算与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class CustomerSettlementPartialFullVoidPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库→销售订单审核→销售出库→配送签收生成客户账单→
    ///     部分结款（收款+优惠）→超额拒绝→尾款结清→余额独立重算→
    ///     作废尾款回滚→重复作废拒绝→401/403 权限矩阵；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task CustomerSettlement_PartialFullVoidDiscountAndPermissionMatrix_OnPostgreSql()
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
        var batchNo = $"{batch.Id}-BATCH";
        var inboundRemark = $"{batch.Id}-入库建批次";
        var saleOrderInnerRemark = $"{batch.Id}O";
        var saleOutRemark = $"{batch.Id}-销售出库";
        var signRemark = $"{batch.Id}-签收生账单";
        var partialSerialNo = $"{batch.Id}-BANK-P";
        var finalSerialNo = $"{batch.Id}-BANK-F";
        var partialSettlementRemark = $"{batch.Id}-部分结款优惠";
        var finalSettlementRemark = $"{batch.Id}-尾款结清";
        var voidRemark = $"{batch.Id}-尾款作废回滚";
        var password = "SkyRocCustomerSettlement!2026";
        var userAgent = $"SkyRoc-T10-CustomerSettlement/{batch.Id}";
        var createName = "T10-CustomerSettlement";
        var contactName = "结款王老师";
        var contactPhone = "13900001001";
        var deliveryAddress = "上海市浦东新区客户结款路 10 号食堂西门";

        var financeReadPermission = PermissionCodes.Business.Finance.Read;
        var financeCreatePermission = PermissionCodes.Business.Finance.Create;
        var financeDeletePermission = PermissionCodes.Business.Finance.Delete;

        var inboundQuantity = NumericPrecision.RoundQuantity(12m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(5m);
        var saleQuantity = NumericPrecision.RoundQuantity(10m);
        var saleUnitPrice = NumericPrecision.RoundMoney(8m);
        var expectedReceivable = NumericPrecision.RoundMoney(saleQuantity * saleUnitPrice);
        var partialPayment = NumericPrecision.RoundMoney(30m);
        var partialDiscount = NumericPrecision.RoundMoney(5m);
        var partialApplied = NumericPrecision.RoundMoney(partialPayment + partialDiscount);
        var remainingAfterPartial = NumericPrecision.RoundMoney(expectedReceivable - partialApplied);

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
                Desc = "T10 客户结款最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T10客户结款操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001011",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T10客户结款只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900001012",
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
                Title = "T10客户结款只读菜单",
                Component = "page.t10.customer.settlement.seed",
                MenuType = MenuType.Menu,
                Order = 9101,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedReadButtonId,
                Code = financeReadPermission,
                Desc = "T10 财务读取权限按钮",
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
        Guid? saleOutOrderId = null;
        Guid? deliveryTaskId = null;
        Guid? receiptId = null;
        Guid? customerBillId = null;
        Guid? partialSettlementId = null;
        Guid? finalSettlementId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
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
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousCreate, ResponseCode.Unauthorized);
            }

            using (var anonymousVoid = await anonymousClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, $"/api/customer-settlements/{Guid.NewGuid()}/void")
                       {
                           Content = JsonContent.Create(new VoidCustomerSettlementDto { Remark = "未认证作废" })
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousVoid, ResponseCode.Unauthorized);
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
                           remark = "T10客户结款切片销售订单",
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
                createdBatchId!.Value,
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
                var assigned = Assert.Single(await ReadApiDataAsync<List<DeliveryTaskDto>>(assign));
                Assert.Equal(DeliveryTaskStatus.Assigned, assigned.DeliveryStatus);
            }

            using (var start = await adminClient.PutAsync($"/api/delivery-tasks/{task.Id}/start", null))
            {
                Assert.Equal(HttpStatusCode.OK, start.StatusCode);
                var started = await ReadApiDataAsync<DeliveryTaskDto>(start);
                Assert.Equal(DeliveryTaskStatus.Delivering, started.DeliveryStatus);
            }

            using (var sign = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task.Id}/sign",
                       BuildSignPayload("结款王老师", stockOutDetailId, saleQuantity, signRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, sign.StatusCode);
                var receipt = await ReadApiDataAsync<OrderReceiptDto>(sign);
                receiptId = receipt.Id;
                registry.Register<OrderReceipt>(receipt.Id, nameof(OrderReceipt.SignRemark), signRemark);
            }

            await using (var afterSign = fixture.CreateDbContext())
            {
                var persistedOrder = await afterSign.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == saleOrder.Id);
                Assert.Equal(SaleOrderStatus.Signed, persistedOrder.OrderStatus);
                Assert.Equal(expectedReceivable, persistedOrder.SettlementPrice);

                var bill = await afterSign.CustomerBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.SaleOrderId == saleOrder.Id);
                customerBillId = bill.Id;
                Assert.Equal(expectedReceivable, bill.ReceivableAmount);
                Assert.Equal(0m, bill.SettledAmount);
                Assert.Equal(CustomerBillStatus.Pending, bill.BillStatus);
                Assert.Contains(
                    bill.Details,
                    detail => detail.SourceType == CustomerBillDetailSourceType.OrderAcceptance
                              && detail.Amount == expectedReceivable);
            }

            CustomerSettlementDto partialSettlement;
            using (var createPartial = await adminClient.PostAsJsonAsync(
                       "/api/customer-settlements",
                       new CreateCustomerSettlementDto
                       {
                           SettlementDate = new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc),
                           SerialNo = partialSerialNo,
                           Remark = partialSettlementRemark,
                           Details =
                           [
                               new CreateCustomerSettlementDetailDto
                               {
                                   CustomerBillId = customerBillId!.Value,
                                   PaymentAmount = partialPayment,
                                   DiscountAmount = partialDiscount,
                                   Remark = $"{batch.Id}-明细优惠"
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createPartial.StatusCode);
                partialSettlement = await ReadApiDataAsync<CustomerSettlementDto>(createPartial);
                partialSettlementId = partialSettlement.Id;
                registry.Register<CustomerSettlement>(
                    partialSettlement.Id,
                    nameof(CustomerSettlement.SerialNo),
                    partialSerialNo);
            }

            Assert.Equal(CustomerSettlementStatus.PartiallySettled, partialSettlement.SettlementStatus);
            Assert.Equal(expectedReceivable, partialSettlement.ShouldAmount);
            Assert.Equal(partialPayment, partialSettlement.PaymentAmount);
            Assert.Equal(partialDiscount, partialSettlement.DiscountAmount);
            Assert.Equal(partialApplied, partialSettlement.AppliedAmount);
            Assert.Equal(remainingAfterPartial, partialSettlement.RemainingAmount);
            var partialDetail = Assert.Single(partialSettlement.Details);
            Assert.Equal(partialApplied, partialDetail.AppliedAmount);
            Assert.Equal(remainingAfterPartial, partialDetail.RemainingAmount);

            await using (var afterPartial = fixture.CreateDbContext())
            {
                var bill = await afterPartial.CustomerBills.AsNoTracking()
                    .SingleAsync(item => item.Id == customerBillId.Value);
                Assert.Equal(partialApplied, bill.SettledAmount);
                Assert.Equal(CustomerBillStatus.PartiallySettled, bill.BillStatus);
                AssertIndependentBillBalance(bill);
            }

            using (var rejectOver = await adminClient.PostAsJsonAsync(
                       "/api/customer-settlements",
                       new CreateCustomerSettlementDto
                       {
                           SerialNo = $"{batch.Id}-BANK-OVER",
                           Remark = $"{batch.Id}-超额结款",
                           Details =
                           [
                               new CreateCustomerSettlementDetailDto
                               {
                                   CustomerBillId = customerBillId.Value,
                                   PaymentAmount = NumericPrecision.RoundMoney(remainingAfterPartial + 1m)
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectOver, ResponseCode.DatabaseError);
            }

            using (var rejectZero = await adminClient.PostAsJsonAsync(
                       "/api/customer-settlements",
                       new CreateCustomerSettlementDto
                       {
                           SerialNo = $"{batch.Id}-BANK-ZERO",
                           Remark = $"{batch.Id}-零金额结款",
                           Details =
                           [
                               new CreateCustomerSettlementDetailDto
                               {
                                   CustomerBillId = customerBillId.Value,
                                   PaymentAmount = 0m,
                                   DiscountAmount = 0m
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectZero, ResponseCode.ValidationError);
            }

            await using (var afterReject = fixture.CreateDbContext())
            {
                Assert.False(await afterReject.CustomerSettlements.AnyAsync(item =>
                    item.SerialNo == $"{batch.Id}-BANK-OVER"
                    || item.SerialNo == $"{batch.Id}-BANK-ZERO"));
                var bill = await afterReject.CustomerBills.AsNoTracking()
                    .SingleAsync(item => item.Id == customerBillId.Value);
                Assert.Equal(partialApplied, bill.SettledAmount);
                Assert.Equal(CustomerBillStatus.PartiallySettled, bill.BillStatus);
            }

            CustomerSettlementDto finalSettlement;
            using (var createFinal = await adminClient.PostAsJsonAsync(
                       "/api/customer-settlements",
                       new CreateCustomerSettlementDto
                       {
                           SettlementDate = new DateTime(2026, 7, 20, 13, 0, 0, DateTimeKind.Utc),
                           SerialNo = finalSerialNo,
                           Remark = finalSettlementRemark,
                           Details =
                           [
                               new CreateCustomerSettlementDetailDto
                               {
                                   CustomerBillId = customerBillId.Value,
                                   PaymentAmount = remainingAfterPartial,
                                   DiscountAmount = 0m,
                                   Remark = $"{batch.Id}-明细尾款"
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createFinal.StatusCode);
                finalSettlement = await ReadApiDataAsync<CustomerSettlementDto>(createFinal);
                finalSettlementId = finalSettlement.Id;
                registry.Register<CustomerSettlement>(
                    finalSettlement.Id,
                    nameof(CustomerSettlement.SerialNo),
                    finalSerialNo);
            }

            Assert.Equal(CustomerSettlementStatus.Settled, finalSettlement.SettlementStatus);
            Assert.Equal(remainingAfterPartial, finalSettlement.ShouldAmount);
            Assert.Equal(remainingAfterPartial, finalSettlement.PaymentAmount);
            Assert.Equal(0m, finalSettlement.DiscountAmount);
            Assert.Equal(remainingAfterPartial, finalSettlement.AppliedAmount);
            Assert.Equal(0m, finalSettlement.RemainingAmount);

            await using (var afterFinal = fixture.CreateDbContext())
            {
                var bill = await afterFinal.CustomerBills.AsNoTracking()
                    .SingleAsync(item => item.Id == customerBillId.Value);
                Assert.Equal(expectedReceivable, bill.SettledAmount);
                Assert.Equal(CustomerBillStatus.Settled, bill.BillStatus);
                AssertIndependentBillBalance(bill);

                var activeApplied = await afterFinal.CustomerSettlementDetails
                    .AsNoTracking()
                    .Where(detail => detail.CustomerBillId == customerBillId.Value
                                     && detail.CustomerSettlement.SettlementStatus
                                     != CustomerSettlementStatus.Voided)
                    .SumAsync(detail => detail.AppliedAmount);
                Assert.Equal(expectedReceivable, NumericPrecision.RoundMoney(activeApplied));
            }

            CustomerSettlementDto voidedFinal;
            using (var voidFinal = await adminClient.SendAsync(
                       new HttpRequestMessage(
                           HttpMethod.Delete,
                           $"/api/customer-settlements/{finalSettlement.Id}/void")
                       {
                           Content = JsonContent.Create(new VoidCustomerSettlementDto { Remark = voidRemark })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, voidFinal.StatusCode);
                voidedFinal = await ReadApiDataAsync<CustomerSettlementDto>(voidFinal);
            }

            Assert.Equal(CustomerSettlementStatus.Voided, voidedFinal.SettlementStatus);
            Assert.Equal(voidRemark, voidedFinal.Remark);
            Assert.NotNull(voidedFinal.VoidedTime);
            Assert.False(string.IsNullOrWhiteSpace(voidedFinal.VoidedByName));

            await using (var afterVoid = fixture.CreateDbContext())
            {
                var bill = await afterVoid.CustomerBills.AsNoTracking()
                    .SingleAsync(item => item.Id == customerBillId.Value);
                Assert.Equal(partialApplied, bill.SettledAmount);
                Assert.Equal(CustomerBillStatus.PartiallySettled, bill.BillStatus);
                AssertIndependentBillBalance(bill);

                var activeApplied = await afterVoid.CustomerSettlementDetails
                    .AsNoTracking()
                    .Where(detail => detail.CustomerBillId == customerBillId.Value
                                     && detail.CustomerSettlement.SettlementStatus
                                     != CustomerSettlementStatus.Voided)
                    .SumAsync(detail => detail.AppliedAmount);
                Assert.Equal(partialApplied, NumericPrecision.RoundMoney(activeApplied));
            }

            using (var rejectRepeatVoid = await adminClient.SendAsync(
                       new HttpRequestMessage(
                           HttpMethod.Delete,
                           $"/api/customer-settlements/{finalSettlement.Id}/void")
                       {
                           Content = JsonContent.Create(new VoidCustomerSettlementDto
                           {
                               Remark = $"{batch.Id}-重复作废"
                           })
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectRepeatVoid, ResponseCode.DatabaseError);
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
                Assert.DoesNotContain(financeCreatePermission, info.Buttons);
                Assert.DoesNotContain(financeDeletePermission, info.Buttons);
            }

            using (var allowedGet = await limitedClient.GetAsync(
                       $"/api/customer-settlements/{partialSettlement.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedGet.StatusCode);
                var settlement = await ReadApiDataAsync<CustomerSettlementDto>(allowedGet);
                Assert.Equal(partialSettlement.Id, settlement.Id);
            }

            using (var allowedBills = await limitedClient.GetAsync("/api/customer-settlements/bills?current=1&size=20"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedBills.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<CustomerBillDto>>(allowedBills);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records, item => item.Id == customerBillId.Value);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/customer-settlements",
                       new CreateCustomerSettlementDto
                       {
                           SerialNo = $"{batch.Id}-BANK-DENIED",
                           Remark = $"{batch.Id}-无创建权限",
                           Details =
                           [
                               new CreateCustomerSettlementDetailDto
                               {
                                   CustomerBillId = customerBillId.Value,
                                   PaymentAmount = NumericPrecision.RoundMoney(1m)
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedVoid = await limitedClient.SendAsync(
                       new HttpRequestMessage(
                           HttpMethod.Delete,
                           $"/api/customer-settlements/{partialSettlement.Id}/void")
                       {
                           Content = JsonContent.Create(new VoidCustomerSettlementDto
                           {
                               Remark = $"{batch.Id}-无作废权限"
                           })
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedVoid, ResponseCode.Forbidden);
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

                var residualSettlementIds = new List<Guid>();
                if (partialSettlementId.HasValue)
                    residualSettlementIds.Add(partialSettlementId.Value);
                if (finalSettlementId.HasValue)
                    residualSettlementIds.Add(finalSettlementId.Value);

                var residualSettlements = await cleanupContext.CustomerSettlements
                    .Where(item => residualSettlementIds.Contains(item.Id)
                                   || (item.SerialNo != null && item.SerialNo.StartsWith(batch.Id))
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualSettlements.Count > 0)
                {
                    cleanupContext.CustomerSettlements.RemoveRange(residualSettlements);
                    await cleanupContext.SaveChangesAsync();
                }

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
                if (receiptId.HasValue)
                    residualReceiptIds.Add(receiptId.Value);
                var residualReceipts = await cleanupContext.OrderReceipts
                    .Where(item => residualReceiptIds.Contains(item.Id)
                                   || (item.SignRemark != null
                                       && (item.SignRemark == signRemark
                                           || item.SignRemark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualReceipts.Count > 0)
                {
                    cleanupContext.OrderReceipts.RemoveRange(residualReceipts);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualTaskIds = new List<Guid>();
                if (deliveryTaskId.HasValue)
                    residualTaskIds.Add(deliveryTaskId.Value);
                var residualTasks = await cleanupContext.DeliveryTasks
                    .Where(item => residualTaskIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == saleOutRemark || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualTasks.Count > 0)
                {
                    cleanupContext.DeliveryTasks.RemoveRange(residualTasks);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualStockOutIds = new List<Guid>();
                if (saleOutOrderId.HasValue)
                    residualStockOutIds.Add(saleOutOrderId.Value);
                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => residualStockOutIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == saleOutRemark || item.Remark.StartsWith(batch.Id))))
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
                                   || (role.Code != null && role.Code.StartsWith(batch.Id)))
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
            Assert.False(await residualContext.CustomerSettlements.AnyAsync(item =>
                (partialSettlementId.HasValue && item.Id == partialSettlementId.Value)
                || (finalSettlementId.HasValue && item.Id == finalSettlementId.Value)
                || (item.SerialNo != null && item.SerialNo.StartsWith(batch.Id))
                || (item.Remark != null && item.Remark.StartsWith(batch.Id))));
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
            Assert.True(await residualContext.Drivers.AnyAsync(item => item.Id == managedDriverId));
        }
    }

    private static void AssertIndependentBillBalance(CustomerBill bill)
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
                    remark = "T10其他入库明细"
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
                    remark = "T10销售出库明细"
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
