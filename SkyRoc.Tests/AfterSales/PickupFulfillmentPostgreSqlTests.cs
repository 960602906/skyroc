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
using Domain.Entities.Delivery;
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
///     T9 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证售后取货履约
///     （分配/换司机、开始、完成）状态机、禁用司机拒绝与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class PickupFulfillmentPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库建批次 → 销售订单审核 → 退货退款审核生成取货 →
    ///     未分配拒绝开始/完成 → 禁用司机拒绝 → 分配/换司机 → 开始 →
    ///     取货中拒绝再分配 → 完成 → 完成后拒绝再流转 → 401/403 权限矩阵；
    ///     临时数据精确清理。本切片不覆盖退货入库与库存回补。
    /// </summary>
    [Fact]
    public async Task Pickup_AssignStartCompletePermissionAndIllegalTransitions_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedReadButtonId = Guid.NewGuid();
        var disabledDriverId = Guid.NewGuid();

        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var disabledDriverCode = $"{batch.Id}D";
        var batchNo = $"{batch.Id}-BATCH";
        var inboundRemark = $"{batch.Id}-入库建批次";
        var saleOrderInnerRemark = $"{batch.Id}O";
        var returnRemark = $"{batch.Id}-退货取货履约";
        var assignRemark = $"{batch.Id}-分配取货司机";
        var reassignRemark = $"{batch.Id}-更换取货司机";
        var source = batch.Id;
        var password = "SkyRocPickupFulfill!2026";
        var userAgent = $"SkyRoc-T9-PickupFulfill/{batch.Id}";
        var createName = "T9-PickupFulfillment";
        var contactName = "取货履约王老师";
        var contactPhone = "13900009921";
        var pickupAddress = $"{batch.Id}-上海市浦东新区取货履约路 21 号食堂后门";
        var deliveryAddress = "上海市浦东新区取货联调路 21 号食堂西门";
        var plannedPickupTime = new DateTime(2026, 7, 20, 10, 30, 0, DateTimeKind.Utc);
        var replanPickupTime = new DateTime(2026, 7, 20, 14, 0, 0, DateTimeKind.Utc);

        var afterSalesReadPermission = PermissionCodes.Business.AfterSales.Read;
        var afterSalesUpdatePermission = PermissionCodes.Business.AfterSales.Update;

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
        string managedDriverName = null!;
        string managedDriverPhone = null!;
        Guid managedDriver2Id;
        string managedDriver2Name = null!;
        string managedDriver2Phone = null!;
        Guid managedCarrierId;

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
            var driver2Code = DemoDataStableKeyCatalog.Create("DRIVER", 2);

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
            Assert.NotNull(driver.CarrierId);
            managedDriverId = driver.Id;
            managedDriverName = driver.Name;
            managedDriverPhone = driver.Phone!;
            managedCarrierId = driver.CarrierId.Value;

            var driver2 = await seedContext.Drivers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == driver2Code);
            Assert.NotNull(driver2);
            Assert.Equal(Status.Enable, driver2.Status);
            managedDriver2Id = driver2.Id;
            managedDriver2Name = driver2.Name;
            managedDriver2Phone = driver2.Phone!;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T9 取货履约最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T9取货履约操作员",
                    Gender = GenderType.Male,
                    Phone = "13900009931",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T9取货只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900009932",
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
                Title = "T9取货只读菜单",
                Component = "page.t9.pickup.fulfill.seed",
                MenuType = MenuType.Menu,
                Order = 9942,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedReadButtonId,
                Code = afterSalesReadPermission,
                Desc = "T9 售后取货读取权限按钮",
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

            await seedContext.Drivers.AddAsync(new Driver
            {
                Id = disabledDriverId,
                Code = disabledDriverCode,
                Name = "T9临时停用取货司机",
                Phone = "13900009941",
                CarrierId = managedCarrierId,
                PlateNumber = "沪T9P0001",
                LicenseNo = "310115198900000021",
                Remark = $"{batch.Id}-禁用取货司机",
                Status = Status.Disable,
                CreateName = createName
            });

            await seedContext.SaveChangesAsync();
        }

        registry.Register<Role>(limitedRoleId, nameof(Role.Code), limitedRoleCode);
        registry.Register<User>(adminUserId, nameof(User.Username), adminUsername);
        registry.Register<User>(limitedUserId, nameof(User.Username), limitedUsername);
        registry.Register<Menu>(seedMenuId, nameof(Menu.Name), seedMenuName);
        registry.Register<Driver>(disabledDriverId, nameof(Driver.Code), disabledDriverCode);

        Guid? inboundOrderId = null;
        Guid? saleOrderId = null;
        Guid? returnAfterSaleId = null;
        Guid? pickupTaskId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousList = await anonymousClient.GetAsync("/api/after-sales/pickup-tasks?current=1&size=10"))
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
                           "T9取货履约销售订单",
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
                Assert.Null(createdPickup.DriverId);
                Assert.Equal(pickupAddress, createdPickup.PickupAddress);
                Assert.Null(createdPickup.StockInOrderId);
                pickupTaskId = createdPickup.Id;
                registry.Register<PickupTask>(createdPickup.Id, nameof(PickupTask.PickupAddressSnapshot), pickupAddress);
            }

            var taskId = pickupTaskId.Value;

            PickupTaskDto taskDetail;
            using (var getTask = await adminClient.GetAsync($"/api/after-sales/pickup-tasks/{taskId}"))
            {
                Assert.Equal(HttpStatusCode.OK, getTask.StatusCode);
                taskDetail = await ReadApiDataAsync<PickupTaskDto>(getTask);
                Assert.Equal(PickupTaskStatus.PendingAssign, taskDetail.PickupStatus);
                Assert.Equal(returnDraft.Id, taskDetail.AfterSaleId);
                Assert.Equal(contactName, taskDetail.ContactName);
                Assert.Equal(contactPhone, taskDetail.ContactPhone);
                Assert.Equal(refundQuantity, taskDetail.Quantity);
                Assert.Equal(managedGoodsId, taskDetail.GoodsId);
            }

            using (var rejectStartUnassigned = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/start",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectStartUnassigned, ResponseCode.DatabaseError);
            }

            using (var rejectCompleteUnassigned = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectCompleteUnassigned, ResponseCode.DatabaseError);
            }

            using (var rejectDisabled = await adminClient.PutAsJsonAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/assign",
                       new AssignPickupTaskDto
                       {
                           DriverId = disabledDriverId,
                           PlannedPickupTime = plannedPickupTime,
                           Remark = $"{assignRemark}-禁用司机"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectDisabled, ResponseCode.DatabaseError);
            }

            await using (var afterDisabled = fixture.CreateDbContext())
            {
                var persisted = await afterDisabled.PickupTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == taskId);
                Assert.Equal(PickupTaskStatus.PendingAssign, persisted.PickupStatus);
                Assert.Null(persisted.DriverId);
                Assert.Null(persisted.AssignedTime);
            }

            PickupTaskDto assigned;
            using (var assign = await adminClient.PutAsJsonAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/assign",
                       new AssignPickupTaskDto
                       {
                           DriverId = managedDriverId,
                           PlannedPickupTime = plannedPickupTime,
                           Remark = assignRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
                assigned = await ReadApiDataAsync<PickupTaskDto>(assign);
            }

            Assert.Equal(PickupTaskStatus.PendingPickup, assigned.PickupStatus);
            Assert.Equal(managedDriverId, assigned.DriverId);
            Assert.Equal(managedDriverName, assigned.DriverName);
            Assert.Equal(managedDriverPhone, assigned.DriverPhone);
            Assert.Equal(plannedPickupTime, assigned.PlannedPickupTime);
            Assert.NotNull(assigned.AssignedTime);
            Assert.Null(assigned.StartedTime);
            Assert.Null(assigned.CompletedTime);
            Assert.Equal(assignRemark, assigned.Remark);

            PickupTaskDto reassigned;
            using (var reassign = await adminClient.PutAsJsonAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/assign",
                       new AssignPickupTaskDto
                       {
                           DriverId = managedDriver2Id,
                           PlannedPickupTime = replanPickupTime,
                           Remark = reassignRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, reassign.StatusCode);
                reassigned = await ReadApiDataAsync<PickupTaskDto>(reassign);
            }

            Assert.Equal(PickupTaskStatus.PendingPickup, reassigned.PickupStatus);
            Assert.Equal(managedDriver2Id, reassigned.DriverId);
            Assert.Equal(managedDriver2Name, reassigned.DriverName);
            Assert.Equal(managedDriver2Phone, reassigned.DriverPhone);
            Assert.Equal(replanPickupTime, reassigned.PlannedPickupTime);
            Assert.Equal(reassignRemark, reassigned.Remark);

            PickupTaskDto started;
            using (var start = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/start",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, start.StatusCode);
                started = await ReadApiDataAsync<PickupTaskDto>(start);
            }

            Assert.Equal(PickupTaskStatus.PickingUp, started.PickupStatus);
            Assert.Equal(managedDriver2Id, started.DriverId);
            Assert.NotNull(started.StartedTime);
            Assert.Null(started.CompletedTime);

            using (var rejectAssignWhilePicking = await adminClient.PutAsJsonAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/assign",
                       new AssignPickupTaskDto
                       {
                           DriverId = managedDriverId,
                           PlannedPickupTime = plannedPickupTime,
                           Remark = $"{assignRemark}-取货中再分配"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectAssignWhilePicking, ResponseCode.DatabaseError);
            }

            await using (var afterRejectAssign = fixture.CreateDbContext())
            {
                var persisted = await afterRejectAssign.PickupTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == taskId);
                Assert.Equal(PickupTaskStatus.PickingUp, persisted.PickupStatus);
                Assert.Equal(managedDriver2Id, persisted.DriverId);
                Assert.Equal(managedDriver2Name, persisted.DriverNameSnapshot);
                Assert.NotNull(persisted.StartedTime);
            }

            using (var rejectStartAgain = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/start",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectStartAgain, ResponseCode.DatabaseError);
            }

            PickupTaskDto completed;
            using (var complete = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/complete",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
                completed = await ReadApiDataAsync<PickupTaskDto>(complete);
            }

            Assert.Equal(PickupTaskStatus.Completed, completed.PickupStatus);
            Assert.Equal(managedDriver2Id, completed.DriverId);
            Assert.NotNull(completed.StartedTime);
            Assert.NotNull(completed.CompletedTime);
            Assert.Null(completed.StockInOrderId);

            using (var rejectCompleteAgain = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectCompleteAgain, ResponseCode.DatabaseError);
            }

            using (var rejectStartAfterComplete = await adminClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/start",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectStartAfterComplete, ResponseCode.DatabaseError);
            }

            using (var rejectAssignAfterComplete = await adminClient.PutAsJsonAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/assign",
                       new AssignPickupTaskDto
                       {
                           DriverId = managedDriverId,
                           PlannedPickupTime = plannedPickupTime,
                           Remark = $"{assignRemark}-完成后分配"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectAssignAfterComplete, ResponseCode.DatabaseError);
            }

            await using (var assertContext = fixture.CreateDbContext())
            {
                var persistedTask = await assertContext.PickupTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == taskId);
                Assert.Equal(PickupTaskStatus.Completed, persistedTask.PickupStatus);
                Assert.Equal(managedDriver2Id, persistedTask.DriverId);
                Assert.Equal(replanPickupTime, persistedTask.PlannedPickupTime);
                Assert.NotNull(persistedTask.AssignedTime);
                Assert.NotNull(persistedTask.StartedTime);
                Assert.NotNull(persistedTask.CompletedTime);

                var persistedAfterSale = await assertContext.AfterSales.AsNoTracking()
                    .SingleAsync(item => item.Id == returnAfterSaleId);
                Assert.Equal(AfterSaleStatus.ReturnPending, persistedAfterSale.AfterStatus);

                Assert.Equal(0, await assertContext.StockInOrders.CountAsync(item =>
                    item.AfterSaleId == returnAfterSaleId));
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
                Assert.Contains(afterSalesReadPermission, info.Buttons);
                Assert.DoesNotContain(afterSalesUpdatePermission, info.Buttons);
            }

            using (var allowedList = await limitedClient.GetAsync(
                       $"/api/after-sales/pickup-tasks?current=1&size=20&keyword={Uri.EscapeDataString(taskDetail.TaskNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<PickupTaskDto>>(allowedList);
                Assert.Contains(page.Records!, item => item.Id == taskId);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/after-sales/pickup-tasks/{taskId}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
                var detail = await ReadApiDataAsync<PickupTaskDto>(allowedDetail);
                Assert.Equal(PickupTaskStatus.Completed, detail.PickupStatus);
            }

            using (var deniedAssign = await limitedClient.PutAsJsonAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/assign",
                       new AssignPickupTaskDto
                       {
                           DriverId = managedDriverId,
                           PlannedPickupTime = plannedPickupTime,
                           Remark = $"{assignRemark}-越权分配"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAssign, ResponseCode.Forbidden);
            }

            using (var deniedStart = await limitedClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/start",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedStart, ResponseCode.Forbidden);
            }

            using (var deniedComplete = await limitedClient.PostAsync(
                       $"/api/after-sales/pickup-tasks/{taskId}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedComplete, ResponseCode.Forbidden);
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
            Assert.True(await auditContext.Drivers.AnyAsync(item =>
                item.Id == managedDriver2Id && item.Code == DemoDataStableKeyCatalog.Create("DRIVER", 2)));
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
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

                var residualStockInIds = new List<Guid>();
                if (inboundOrderId.HasValue)
                    residualStockInIds.Add(inboundOrderId.Value);
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => residualStockInIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == inboundRemark || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualOrderIdSet = residualStockInOrders.Select(item => item.Id).ToHashSet();
                var residualLedgers = await cleanupContext.StockLedgers
                    .Where(ledger => residualOrderIdSet.Contains(ledger.SourceOrderId)
                                     || (createdBatchId.HasValue && ledger.StockBatchId == createdBatchId.Value))
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

                var residualDrivers = await cleanupContext.Drivers
                    .Where(driver => driver.Id == disabledDriverId
                                     || driver.Code == disabledDriverCode
                                     || (driver.Remark != null && driver.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualDrivers.Count > 0)
                {
                    cleanupContext.Drivers.RemoveRange(residualDrivers);
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
                || (item.Remark != null
                    && (item.Remark == inboundRemark || item.Remark.StartsWith(batch.Id)))));
            Assert.False(await residualContext.StockBatches.AnyAsync(item =>
                item.BatchNo == batchNo
                || (createdBatchId.HasValue && item.Id == createdBatchId.Value)));
            Assert.False(await residualContext.Drivers.AnyAsync(driver =>
                driver.Id == disabledDriverId
                || driver.Code == disabledDriverCode
                || (driver.Remark != null && driver.Remark.StartsWith(batch.Id))));
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
            Assert.True(await residualContext.Drivers.AnyAsync(item =>
                item.Id == managedDriver2Id && item.Code == DemoDataStableKeyCatalog.Create("DRIVER", 2)));
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
                    remark = "T9取货履约其他入库明细"
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
