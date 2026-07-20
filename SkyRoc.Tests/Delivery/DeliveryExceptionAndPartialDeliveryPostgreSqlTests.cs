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
///     T8 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证配送异常登记/处理闭环，
///     以及同一销售订单分批出库签收时订单聚合暂不完成。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class DeliveryExceptionAndPartialDeliveryPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库建批次 → 销售订单审核 → 两张分批销售出库 → 生成两任务 → 待分配拒绝异常
    ///     → 分配/开始后登记双异常并分次处理恢复配送中 → 首任务签收后订单仍为配送中且不生成客户账单
    ///     → 401/403 异常创建与处理权限矩阵；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task Delivery_ExceptionLifecycleAndPartialSignKeepsOrderOpen_OnPostgreSql()
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
        var exceptionDesc1 = $"{batch.Id}-客户临时闭店";
        var exceptionDesc2 = $"{batch.Id}-道路临时管制";
        var signRemark = $"{batch.Id}-首批签收";
        var password = "SkyRocDeliveryException!2026";
        var userAgent = $"SkyRoc-T8-DeliveryException/{batch.Id}";
        var createName = "T8-DeliveryExceptionPartial";
        var contactName = "配送异常王老师";
        var contactPhone = "13900008851";
        var deliveryAddress = "上海市浦东新区配送异常路 28 号食堂东门";

        var deliveryReadPermission = PermissionCodes.Business.Delivery.Read;
        var deliveryCreatePermission = PermissionCodes.Business.Delivery.Create;
        var deliveryUpdatePermission = PermissionCodes.Business.Delivery.Update;

        var inboundQuantity = NumericPrecision.RoundQuantity(6m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(6m);
        var saleQuantity = NumericPrecision.RoundQuantity(4m);
        var partialQuantity = NumericPrecision.RoundQuantity(2m);
        var saleUnitPrice = NumericPrecision.RoundMoney(9m);

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
                Desc = "T8 配送异常分批最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T8配送异常操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008861",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T8配送只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900008862",
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
                    Title = "T8配送异常只读菜单",
                    Component = "page.t8.delivery.exception.seed",
                    MenuType = MenuType.Menu,
                    Order = 9861,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T8配送异常写权限菜单",
                    Component = "page.t8.delivery.exception.write",
                    MenuType = MenuType.Menu,
                    Order = 9862,
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
        Guid? exceptionId1 = null;
        Guid? exceptionId2 = null;
        Guid? receiptId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/delivery-exceptions",
                       new CreateDeliveryExceptionDto
                       {
                           DeliveryTaskId = Guid.NewGuid(),
                           Description = "未认证登记异常"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousCreate, ResponseCode.Unauthorized);
            }

            using (var anonymousHandle = await anonymousClient.PutAsJsonAsync(
                       $"/api/delivery-exceptions/{Guid.NewGuid()}/handle",
                       new HandleDeliveryExceptionDto { HandleRemark = "未认证处理异常" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousHandle, ResponseCode.Unauthorized);
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
                           remark = "T8配送异常分批切片销售订单",
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

            var saleOrderDetail = Assert.Single(saleOrder.Details);

            using (var approveOrder = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{saleOrder.Id}/approve",
                       new SaleOrderAuditDto { Remark = $"{saleOrderInnerRemark}-审核通过" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveOrder.StatusCode);
                var approved = await ReadApiDataAsync<SaleOrderDto>(approveOrder);
                Assert.Equal(SaleOrderStatus.SortingPending, approved.OrderStatus);
            }

            var (task1, detail1) = await CreateAuditedSaleOutAndTaskAsync(
                adminClient,
                registry,
                managedWareId,
                saleOrder.Id,
                managedCustomerId,
                createdBatchId!.Value,
                managedGoodsUnitId,
                saleOrderDetail.Id,
                partialQuantity,
                saleUnitPrice,
                saleOutRemark1);
            saleOutOrderId1 = task1.StockOutOrderId;
            deliveryTaskId1 = task1.Id;
            Assert.Equal(DeliveryTaskStatus.PendingAssign, task1.DeliveryStatus);

            using (var rejectUnassigned = await adminClient.PostAsJsonAsync(
                       "/api/delivery-exceptions",
                       new CreateDeliveryExceptionDto
                       {
                           DeliveryTaskId = task1.Id,
                           Description = $"{batch.Id}-待分配禁止登记"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectUnassigned, ResponseCode.DatabaseError);
            }

            await using (var afterReject = fixture.CreateDbContext())
            {
                Assert.False(await afterReject.DeliveryExceptions.AnyAsync(item =>
                    item.DeliveryTaskId == task1.Id));
                Assert.Equal(
                    DeliveryTaskStatus.PendingAssign,
                    (await afterReject.DeliveryTasks.AsNoTracking().SingleAsync(item => item.Id == task1.Id))
                    .DeliveryStatus);
            }

            using (var assign1 = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [task1.Id],
                           DriverId = managedDriverId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assign1.StatusCode);
                var assigned = Assert.Single(await ReadApiDataAsync<List<DeliveryTaskDto>>(assign1));
                Assert.Equal(DeliveryTaskStatus.Assigned, assigned.DeliveryStatus);
            }

            using (var start1 = await adminClient.PutAsync($"/api/delivery-tasks/{task1.Id}/start", null))
            {
                Assert.Equal(HttpStatusCode.OK, start1.StatusCode);
                var started = await ReadApiDataAsync<DeliveryTaskDto>(start1);
                Assert.Equal(DeliveryTaskStatus.Delivering, started.DeliveryStatus);
            }

            DeliveryExceptionDto createdException1;
            using (var createException1 = await adminClient.PostAsJsonAsync(
                       "/api/delivery-exceptions",
                       new CreateDeliveryExceptionDto
                       {
                           DeliveryTaskId = task1.Id,
                           Description = $"  {exceptionDesc1}  "
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createException1.StatusCode);
                createdException1 = await ReadApiDataAsync<DeliveryExceptionDto>(createException1);
                exceptionId1 = createdException1.Id;
                registry.Register<DeliveryException>(
                    createdException1.Id,
                    nameof(DeliveryException.Description),
                    exceptionDesc1);
            }

            Assert.Equal(task1.Id, createdException1.DeliveryTaskId);
            Assert.Equal(managedDriverId, createdException1.DriverId);
            Assert.Equal(managedCustomerId, createdException1.CustomerId);
            Assert.Equal(exceptionDesc1, createdException1.Description);
            Assert.Equal(DeliveryExceptionStatus.Pending, createdException1.HandleStatus);

            DeliveryExceptionDto createdException2;
            using (var createException2 = await adminClient.PostAsJsonAsync(
                       "/api/delivery-exceptions",
                       new CreateDeliveryExceptionDto
                       {
                           DeliveryTaskId = task1.Id,
                           Description = exceptionDesc2
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createException2.StatusCode);
                createdException2 = await ReadApiDataAsync<DeliveryExceptionDto>(createException2);
                exceptionId2 = createdException2.Id;
                registry.Register<DeliveryException>(
                    createdException2.Id,
                    nameof(DeliveryException.Description),
                    exceptionDesc2);
            }

            await using (var afterExceptions = fixture.CreateDbContext())
            {
                var persistedTask = await afterExceptions.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task1.Id);
                Assert.Equal(DeliveryTaskStatus.Exception, persistedTask.DeliveryStatus);
                Assert.Equal(2, await afterExceptions.DeliveryExceptions.CountAsync(item =>
                    item.DeliveryTaskId == task1.Id && item.HandleStatus == DeliveryExceptionStatus.Pending));
            }

            using (var handle1 = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-exceptions/{createdException1.Id}/handle",
                       new HandleDeliveryExceptionDto { HandleRemark = "  已改约次日送达  " }))
            {
                Assert.Equal(HttpStatusCode.OK, handle1.StatusCode);
                var handled = await ReadApiDataAsync<DeliveryExceptionDto>(handle1);
                Assert.Equal(DeliveryExceptionStatus.Handled, handled.HandleStatus);
                Assert.Equal("已改约次日送达", handled.HandleRemark);
                Assert.NotNull(handled.HandleTime);
            }

            await using (var afterHandle1 = fixture.CreateDbContext())
            {
                var persistedTask = await afterHandle1.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task1.Id);
                Assert.Equal(DeliveryTaskStatus.Exception, persistedTask.DeliveryStatus);
            }

            using (var handle2 = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-exceptions/{createdException2.Id}/handle",
                       new HandleDeliveryExceptionDto { HandleRemark = "管制解除，继续配送" }))
            {
                Assert.Equal(HttpStatusCode.OK, handle2.StatusCode);
                var handled = await ReadApiDataAsync<DeliveryExceptionDto>(handle2);
                Assert.Equal(DeliveryExceptionStatus.Handled, handled.HandleStatus);
            }

            await using (var afterHandle2 = fixture.CreateDbContext())
            {
                var persistedTask = await afterHandle2.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task1.Id);
                Assert.Equal(DeliveryTaskStatus.Delivering, persistedTask.DeliveryStatus);
            }

            using (var rejectDuplicate = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-exceptions/{createdException1.Id}/handle",
                       new HandleDeliveryExceptionDto { HandleRemark = "重复处理" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectDuplicate, ResponseCode.DatabaseError);
            }

            var (task2, detail2) = await CreateAuditedSaleOutAndTaskAsync(
                adminClient,
                registry,
                managedWareId,
                saleOrder.Id,
                managedCustomerId,
                createdBatchId.Value,
                managedGoodsUnitId,
                saleOrderDetail.Id,
                partialQuantity,
                saleUnitPrice,
                saleOutRemark2);
            saleOutOrderId2 = task2.StockOutOrderId;
            deliveryTaskId2 = task2.Id;

            using (var assign2 = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [task2.Id],
                           DriverId = managedDriverId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assign2.StatusCode);
            }

            using (var start2 = await adminClient.PutAsync($"/api/delivery-tasks/{task2.Id}/start", null))
            {
                Assert.Equal(HttpStatusCode.OK, start2.StatusCode);
            }

            OrderReceiptDto receipt;
            using (var sign1 = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task1.Id}/sign",
                       new SignDeliveryTaskDto
                       {
                           SignerName = " 王老师 ",
                           Remark = signRemark,
                           Details =
                           [
                               new SignDeliveryCheckDetailDto
                               {
                                   StockOutDetailId = detail1,
                                   AcceptedBaseQuantity = partialQuantity,
                                   CheckStatus = OrderCustomerCheckStatus.Accepted
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, sign1.StatusCode);
                receipt = await ReadApiDataAsync<OrderReceiptDto>(sign1);
                receiptId = receipt.Id;
                registry.Register<OrderReceipt>(receipt.Id, nameof(OrderReceipt.SignRemark), signRemark);
            }

            Assert.Equal("王老师", receipt.SignerName);
            Assert.Equal(signRemark, receipt.SignRemark);
            Assert.Equal(task1.Id, receipt.DeliveryTaskId);
            var check = Assert.Single(receipt.CheckDetails);
            Assert.Equal(partialQuantity, check.AcceptedBaseQuantity);

            await using (var afterPartialSign = fixture.CreateDbContext())
            {
                var persistedTask1 = await afterPartialSign.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task1.Id);
                var persistedTask2 = await afterPartialSign.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task2.Id);
                Assert.Equal(DeliveryTaskStatus.Signed, persistedTask1.DeliveryStatus);
                Assert.Equal(DeliveryTaskStatus.Delivering, persistedTask2.DeliveryStatus);

                var persistedOrder = await afterPartialSign.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == saleOrder.Id);
                Assert.Equal(SaleOrderStatus.Delivering, persistedOrder.OrderStatus);
                Assert.Equal(OrderOutStorageStatus.Generated, persistedOrder.OutStorageStatus);
                Assert.NotEqual(SaleOrderStatus.Signed, persistedOrder.OrderStatus);

                Assert.False(await afterPartialSign.CustomerBills.AnyAsync(bill =>
                    bill.SaleOrderId == saleOrder.Id));
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

            using (var allowedList = await limitedClient.GetAsync(
                       $"/api/delivery-exceptions?current=1&size=20&keyword={Uri.EscapeDataString(createdException1.ExceptionNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<DeliveryExceptionDto>>(allowedList);
                Assert.Contains(page.Records!, item => item.Id == createdException1.Id);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/delivery-exceptions",
                       new CreateDeliveryExceptionDto
                       {
                           DeliveryTaskId = task2.Id,
                           Description = $"{batch.Id}-无创建权限"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedHandle = await limitedClient.PutAsJsonAsync(
                       $"/api/delivery-exceptions/{createdException2.Id}/handle",
                       new HandleDeliveryExceptionDto { HandleRemark = "无更新权限" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedHandle, ResponseCode.Forbidden);
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
                var residualExceptionIds = new List<Guid>();
                if (exceptionId1.HasValue)
                    residualExceptionIds.Add(exceptionId1.Value);
                if (exceptionId2.HasValue)
                    residualExceptionIds.Add(exceptionId2.Value);

                var residualExceptions = await cleanupContext.DeliveryExceptions
                    .Where(item => residualExceptionIds.Contains(item.Id)
                                   || item.Description.StartsWith(batch.Id))
                    .ToListAsync();
                if (residualExceptions.Count > 0)
                {
                    cleanupContext.DeliveryExceptions.RemoveRange(residualExceptions);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualReceiptIds = new List<Guid>();
                if (receiptId.HasValue)
                    residualReceiptIds.Add(receiptId.Value);
                var residualReceipts = await cleanupContext.OrderReceipts
                    .Where(item => residualReceiptIds.Contains(item.Id)
                                   || (item.SignRemark != null
                                       && (item.SignRemark == signRemark || item.SignRemark.StartsWith(batch.Id))))
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
            Assert.False(await residualContext.DeliveryExceptions.AnyAsync(item =>
                item.Description.StartsWith(batch.Id)));
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
