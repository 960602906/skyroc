using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Delivery;
using Application.DTOs.Finance;
using Application.DTOs.Orders;
using Application.DTOs.Storage;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Entities.Delivery;
using Domain.Entities.Finance;
using Domain.Entities.Orders;
using Domain.Entities.Storage;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Common;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.EndToEnd;

/// <summary>
///     T13 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证跨模块故障注入与事务回滚
///     （签收中途账单同步失败整单回滚、超额验收/超额结款/已配送出库反审核拒绝、权限矩阵）。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class FaultInjectionPostgreSqlEndToEndTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库→销售订单审核→销售出库→配送开始→签收中途账单故障整单回滚→
    ///     超额验收拒绝→解除故障后签收成功→超额结款拒绝→已配送出库反审核拒绝→
    ///     401/403（报表相邻权限无配送签收）权限矩阵；临时批次精确清理。
    /// </summary>
    [Fact]
    public async Task FaultInjection_SignBillSyncRollbackAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);
        var faultGate = new FaultInjectionGate();

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
        var signFaultRemark = $"{batch.Id}-签收故障";
        var signSuccessRemark = $"{batch.Id}-签收成功";
        var settleSerial = $"{batch.Id}-BANK";
        var settleRemark = $"{batch.Id}-超额结款";
        var password = "SkyRocT13FaultInject!2026";
        var userAgent = $"SkyRoc-T13-FaultInject/{batch.Id}";
        var createName = "T13-FaultInject";
        var contactName = "故障注入王老师";
        var contactPhone = "13900001341";
        var deliveryAddress = "上海市浦东新区故障注入路 13 号食堂西门";

        var reportsReadPermission = PermissionCodes.Business.Reports.Read;

        var inboundQuantity = NumericPrecision.RoundQuantity(8m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(2.5m);
        var saleQuantity = NumericPrecision.RoundQuantity(8m);
        var saleUnitPrice = NumericPrecision.RoundMoney(4.8m);
        var expectedReceivable = NumericPrecision.RoundMoney(saleQuantity * saleUnitPrice);
        var overAcceptance = NumericPrecision.RoundQuantity(saleQuantity + 0.001m);
        var overSettlePayment = NumericPrecision.RoundMoney(expectedReceivable + 0.01m);

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

            var ware = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == wareCode);
            Assert.NotNull(ware);
            managedWareId = ware.Id;

            var driver = await seedContext.Drivers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == driverCode);
            Assert.NotNull(driver);
            managedDriverId = driver.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Menus.AddAsync(new Menu
            {
                Id = seedMenuId,
                Name = seedMenuName,
                Path = $"/{batch.Id}s",
                Title = "T13故障注入只读菜单",
                Component = "page.t13.fault.injection.seed",
                MenuType = MenuType.Menu,
                Order = 9132,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedReadButtonId,
                Code = reportsReadPermission,
                Desc = "T13 报表读取权限按钮",
                MenuId = seedMenuId,
                Menu = null!,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T13故障注入管理员",
                    Gender = GenderType.Male,
                    Phone = "13900001342",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T13故障注入受限用户",
                    Gender = GenderType.Female,
                    Phone = "13900001343",
                    Email = $"{batch.Id}-l@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.UserRoles.AddRangeAsync(
                new UserRole { UserId = adminUserId, RoleId = adminRoleId },
                new UserRole { UserId = limitedUserId, RoleId = limitedRoleId });

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
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory(services =>
            {
                services.AddSingleton(faultGate);

                var descriptors = services
                    .Where(descriptor => descriptor.ServiceType == typeof(ICustomerBillService))
                    .ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddScoped<CustomerBillService>();
                services.AddScoped<ICustomerBillService>(provider =>
                    new FaultInjectingCustomerBillService(
                        provider.GetRequiredService<CustomerBillService>(),
                        provider.GetRequiredService<FaultInjectionGate>()));
            });

            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousSign = await anonymousClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{Guid.NewGuid()}/sign",
                       BuildSignPayload(contactName, Guid.NewGuid(), saleQuantity, signFaultRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousSign, ResponseCode.Unauthorized);
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

            LoginResDto limitedLogin;
            using (var loginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
                limitedLogin = await ReadApiDataAsync<LoginResDto>(loginResponse);
                Assert.False(string.IsNullOrWhiteSpace(limitedLogin.Token));
            }

            using var adminClient = factory.CreateClient();
            adminClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            adminClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using var limitedClient = factory.CreateClient();
            limitedClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedLogin.Token);
            limitedClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

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
                           remark = "T13故障注入切片销售订单",
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

            using (var forbiddenSign = await limitedClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task.Id}/sign",
                       BuildSignPayload(contactName, stockOutDetailId, saleQuantity, signFaultRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(forbiddenSign, ResponseCode.Forbidden);
            }

            // 故障注入：账单同步在签收事务中途失败，回单/任务/订单状态不得落库
            faultGate.ArmCustomerBillAcceptanceSync();
            using (var faultSign = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task.Id}/sign",
                       BuildSignPayload(contactName, stockOutDetailId, saleQuantity, signFaultRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(faultSign, ResponseCode.DatabaseError);
            }

            await using (var afterFault = fixture.CreateDbContext())
            {
                var persistedTask = await afterFault.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task.Id);
                Assert.Equal(DeliveryTaskStatus.Delivering, persistedTask.DeliveryStatus);
                Assert.Null(persistedTask.SignedTime);

                var persistedOrder = await afterFault.SaleOrders
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == saleOrder.Id);
                Assert.Equal(SaleOrderStatus.Delivering, persistedOrder.OrderStatus);
                // 订单创建时已写入 SettlementPrice=OrderPrice；回滚后验收字段不得被签收副作用改写
                Assert.Equal(expectedReceivable, persistedOrder.SettlementPrice);
                var orderDetail = Assert.Single(persistedOrder.Details);
                Assert.Null(orderDetail.CustomerCheckBaseQuantity);
                Assert.Null(orderDetail.CustomerCheckPrice);
                Assert.Equal(OrderCustomerCheckStatus.Pending, orderDetail.CustomerCheckStatus);

                Assert.False(await afterFault.OrderReceipts.AnyAsync(item =>
                    item.DeliveryTaskId == task.Id
                    || (item.SignRemark != null && item.SignRemark.StartsWith(batch.Id))));
                Assert.False(await afterFault.CustomerBills.AnyAsync(item =>
                    item.SaleOrderId == saleOrder.Id));
            }

            // 自然故障：超额验收在写库前拒绝，状态仍保持配送中
            using (var overAccept = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task.Id}/sign",
                       BuildSignPayload(contactName, stockOutDetailId, overAcceptance, signFaultRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(overAccept, ResponseCode.DatabaseError);
            }

            await using (var afterOverAccept = fixture.CreateDbContext())
            {
                var persistedTask = await afterOverAccept.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task.Id);
                Assert.Equal(DeliveryTaskStatus.Delivering, persistedTask.DeliveryStatus);
                Assert.False(await afterOverAccept.OrderReceipts.AnyAsync(item =>
                    item.DeliveryTaskId == task.Id));
            }

            // 解除故障后签收成功，账单按应收独立生成
            faultGate.Disarm();
            using (var successSign = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task.Id}/sign",
                       BuildSignPayload(contactName, stockOutDetailId, saleQuantity, signSuccessRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, successSign.StatusCode);
                var receipt = await ReadApiDataAsync<OrderReceiptDto>(successSign);
                receiptId = receipt.Id;
                registry.Register<OrderReceipt>(receipt.Id, nameof(OrderReceipt.SignRemark), signSuccessRemark);
            }

            await using (var afterSign = fixture.CreateDbContext())
            {
                var persistedTask = await afterSign.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task.Id);
                Assert.Equal(DeliveryTaskStatus.Signed, persistedTask.DeliveryStatus);

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
            }

            // 自然故障：超额结款拒绝，账单余额不变
            using (var overSettle = await adminClient.PostAsJsonAsync(
                       "/api/customer-settlements",
                       new CreateCustomerSettlementDto
                       {
                           SettlementDate = DateTime.UtcNow,
                           SerialNo = settleSerial,
                           Remark = settleRemark,
                           Details =
                           [
                               new CreateCustomerSettlementDetailDto
                               {
                                   CustomerBillId = customerBillId!.Value,
                                   PaymentAmount = overSettlePayment,
                                   DiscountAmount = 0m
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(overSettle, ResponseCode.DatabaseError);
            }

            await using (var afterOverSettle = fixture.CreateDbContext())
            {
                var bill = await afterOverSettle.CustomerBills.AsNoTracking()
                    .SingleAsync(item => item.Id == customerBillId.Value);
                Assert.Equal(0m, bill.SettledAmount);
                Assert.Equal(CustomerBillStatus.Pending, bill.BillStatus);
                Assert.False(await afterOverSettle.CustomerSettlements.AnyAsync(item =>
                    (item.SerialNo != null && item.SerialNo.StartsWith(batch.Id))
                    || (item.Remark != null && item.Remark.StartsWith(batch.Id))));
            }

            // 自然故障：已生成配送任务的销售出库不可反审核，库存不得回补
            using (var reverseOut = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/sale/{saleOutOrderId}/reverse-audit",
                       new StockOutAuditDto { Remark = $"{saleOutRemark}-反审核拒绝" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reverseOut, ResponseCode.DatabaseError);
            }

            await using (var afterReverse = fixture.CreateDbContext())
            {
                var outOrder = await afterReverse.StockOutOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == saleOutOrderId);
                Assert.Equal(StockDocumentStatus.Audited, outOrder.BusinessStatus);

                var stockBatch = await afterReverse.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId.Value);
                Assert.Equal(0m, stockBatch.AvailableQuantity);
            }
        }
        finally
        {
            faultGate.Disarm();

            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                if (customerBillId.HasValue || saleOrderId.HasValue)
                {
                    var billIds = await cleanupContext.CustomerBills.AsNoTracking()
                        .Where(item =>
                            (customerBillId.HasValue && item.Id == customerBillId.Value)
                            || (saleOrderId.HasValue && item.SaleOrderId == saleOrderId.Value))
                        .Select(item => item.Id)
                        .ToListAsync();
                    if (billIds.Count > 0)
                    {
                        var details = await cleanupContext.Set<CustomerBillDetail>()
                            .Where(item => billIds.Contains(item.CustomerBillId))
                            .ToListAsync();
                        if (details.Count > 0)
                        {
                            cleanupContext.RemoveRange(details);
                            await cleanupContext.SaveChangesAsync();
                        }

                        var bills = await cleanupContext.CustomerBills
                            .Where(item => billIds.Contains(item.Id))
                            .ToListAsync();
                        cleanupContext.CustomerBills.RemoveRange(bills);
                        await cleanupContext.SaveChangesAsync();
                    }
                }

                if (receiptId.HasValue || deliveryTaskId.HasValue || saleOrderId.HasValue)
                {
                    var receiptQuery = cleanupContext.OrderReceipts
                        .Include(item => item.CheckDetails)
                        .Where(item =>
                            (receiptId.HasValue && item.Id == receiptId.Value)
                            || (deliveryTaskId.HasValue && item.DeliveryTaskId == deliveryTaskId.Value)
                            || (saleOrderId.HasValue && item.SaleOrderId == saleOrderId.Value)
                            || (item.SignRemark != null && item.SignRemark.StartsWith(batch.Id)));
                    var receipts = await receiptQuery.ToListAsync();
                    if (receipts.Count > 0)
                    {
                        foreach (var receipt in receipts)
                        {
                            if (receipt.CheckDetails.Count > 0)
                                cleanupContext.RemoveRange(receipt.CheckDetails);
                        }

                        cleanupContext.OrderReceipts.RemoveRange(receipts);
                        await cleanupContext.SaveChangesAsync();
                    }
                }

                if (deliveryTaskId.HasValue)
                {
                    var tasks = await cleanupContext.DeliveryTasks
                        .Where(item => item.Id == deliveryTaskId.Value
                                       || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                        .ToListAsync();
                    if (tasks.Count > 0)
                    {
                        cleanupContext.DeliveryTasks.RemoveRange(tasks);
                        await cleanupContext.SaveChangesAsync();
                    }
                }

                if (saleOutOrderId.HasValue)
                {
                    var ledgers = await cleanupContext.StockLedgers
                        .Where(item => item.SourceOrderId == saleOutOrderId.Value)
                        .ToListAsync();
                    if (ledgers.Count > 0)
                    {
                        cleanupContext.StockLedgers.RemoveRange(ledgers);
                        await cleanupContext.SaveChangesAsync();
                    }

                    var details = await cleanupContext.StockOutDetails
                        .Where(item => item.StockOutOrderId == saleOutOrderId.Value)
                        .ToListAsync();
                    if (details.Count > 0)
                    {
                        cleanupContext.StockOutDetails.RemoveRange(details);
                        await cleanupContext.SaveChangesAsync();
                    }

                    var outs = await cleanupContext.StockOutOrders
                        .Where(item => item.Id == saleOutOrderId.Value
                                       || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                        .ToListAsync();
                    if (outs.Count > 0)
                    {
                        cleanupContext.StockOutOrders.RemoveRange(outs);
                        await cleanupContext.SaveChangesAsync();
                    }
                }

                if (inboundOrderId.HasValue || createdBatchId.HasValue)
                {
                    if (inboundOrderId.HasValue)
                    {
                        var ledgers = await cleanupContext.StockLedgers
                            .Where(item => item.SourceOrderId == inboundOrderId.Value)
                            .ToListAsync();
                        if (ledgers.Count > 0)
                        {
                            cleanupContext.StockLedgers.RemoveRange(ledgers);
                            await cleanupContext.SaveChangesAsync();
                        }

                        var details = await cleanupContext.StockInDetails
                            .Where(item => item.StockInOrderId == inboundOrderId.Value)
                            .ToListAsync();
                        if (details.Count > 0)
                        {
                            cleanupContext.StockInDetails.RemoveRange(details);
                            await cleanupContext.SaveChangesAsync();
                        }

                        var ins = await cleanupContext.StockInOrders
                            .Where(item => item.Id == inboundOrderId.Value
                                           || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                            .ToListAsync();
                        if (ins.Count > 0)
                        {
                            cleanupContext.StockInOrders.RemoveRange(ins);
                            await cleanupContext.SaveChangesAsync();
                        }
                    }

                    if (createdBatchId.HasValue)
                    {
                        var batches = await cleanupContext.StockBatches
                            .Where(item => item.Id == createdBatchId.Value || item.BatchNo == batchNo)
                            .ToListAsync();
                        if (batches.Count > 0)
                        {
                            cleanupContext.StockBatches.RemoveRange(batches);
                            await cleanupContext.SaveChangesAsync();
                        }
                    }
                }

                if (saleOrderId.HasValue)
                {
                    var details = await cleanupContext.SaleOrderDetails
                        .Where(item => item.SaleOrderId == saleOrderId.Value)
                        .ToListAsync();
                    if (details.Count > 0)
                    {
                        cleanupContext.SaleOrderDetails.RemoveRange(details);
                        await cleanupContext.SaveChangesAsync();
                    }

                    var orders = await cleanupContext.SaleOrders
                        .Where(item => item.Id == saleOrderId.Value
                                       || (item.InnerRemark != null && item.InnerRemark.StartsWith(batch.Id)))
                        .ToListAsync();
                    if (orders.Count > 0)
                    {
                        cleanupContext.SaleOrders.RemoveRange(orders);
                        await cleanupContext.SaveChangesAsync();
                    }
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
                    remark = "T13故障注入其他入库明细"
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
                    remark = "T13故障注入销售出库明细"
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
