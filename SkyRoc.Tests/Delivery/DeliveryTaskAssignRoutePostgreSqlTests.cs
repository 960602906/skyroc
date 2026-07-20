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
///     T8 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证配送任务分配司机、智能规划路线、
///     开始配送副作用、非法状态拒绝，以及更新权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class DeliveryTaskAssignRoutePostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库建批次 → 销售订单审核 → 销售出库审核 → 生成配送任务 → 分配启用司机（快照）
    ///     → 智能规划路线（路线快照与顺序）→ 换司机覆盖 → 开始配送并同步订单 → 配送中拒绝再分配/规划
    ///     → 禁用司机拒绝且不落库 → 未认证 401、只读角色列表允许/分配与规划 403；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task DeliveryTask_AssignDriverIntelligentPlanStartAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedReadButtonId = Guid.NewGuid();
        var writeMenuId = Guid.NewGuid();
        var writeUpdateButtonId = Guid.NewGuid();
        var disabledDriverId = Guid.NewGuid();

        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var writeMenuName = $"{batch.Id}W";
        var disabledDriverCode = $"{batch.Id}D";
        var batchNo = $"{batch.Id}-BATCH";
        var inboundRemark = $"{batch.Id}-入库建批次";
        var saleOrderInnerRemark = $"{batch.Id}O";
        var saleOutRemark = $"{batch.Id}-销售出库";
        var password = "SkyRocDeliveryAssign!2026";
        var userAgent = $"SkyRoc-T8-DeliveryAssign/{batch.Id}";
        var createName = "T8-DeliveryTaskAssignRoute";
        var contactName = "配送调度王老师";
        var contactPhone = "13900008821";
        var deliveryAddress = "上海市浦东新区配送调度路 18 号食堂侧门";

        var deliveryReadPermission = PermissionCodes.Business.Delivery.Read;
        var deliveryUpdatePermission = PermissionCodes.Business.Delivery.Update;

        var inboundQuantity = NumericPrecision.RoundQuantity(5m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(6m);
        var saleQuantity = NumericPrecision.RoundQuantity(2m);
        var saleUnitPrice = NumericPrecision.RoundMoney(9m);

        Guid adminRoleId;
        Guid managedCustomerId;
        string managedCustomerName = null!;
        Guid managedGoodsId;
        Guid managedGoodsUnitId;
        Guid managedWareId;
        string managedWareName = null!;
        Guid managedDriverId;
        string managedDriverName = null!;
        string managedDriverPhone = null!;
        Guid managedCarrierId;
        string managedCarrierName = null!;
        Guid managedDriver2Id;
        string managedDriver2Name = null!;
        Guid managedRouteId;
        string managedRouteName = null!;
        int managedRouteSequence;

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
            var routeCode = DemoDataStableKeyCatalog.Create("DELIVERY-ROUTE", 1);

            var customer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == customerCode);
            Assert.NotNull(customer);
            managedCustomerId = customer.Id;
            managedCustomerName = customer.Name;

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
            managedWareName = ware.Name;

            var driver = await seedContext.Drivers.AsNoTracking()
                .Include(item => item.Carrier)
                .SingleOrDefaultAsync(item => item.Code == driverCode);
            Assert.NotNull(driver);
            Assert.Equal(Status.Enable, driver.Status);
            Assert.NotNull(driver.CarrierId);
            Assert.NotNull(driver.Carrier);
            managedDriverId = driver.Id;
            managedDriverName = driver.Name;
            managedDriverPhone = driver.Phone!;
            managedCarrierId = driver.CarrierId.Value;
            managedCarrierName = driver.Carrier.Name;

            var driver2 = await seedContext.Drivers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == driver2Code);
            Assert.NotNull(driver2);
            Assert.Equal(Status.Enable, driver2.Status);
            managedDriver2Id = driver2.Id;
            managedDriver2Name = driver2.Name;

            var route = await seedContext.DeliveryRoutes.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == routeCode);
            Assert.NotNull(route);
            Assert.Equal(Status.Enable, route.Status);
            managedRouteId = route.Id;
            managedRouteName = route.Name;

            var customerRoute = await seedContext.CustomerRoutes.AsNoTracking()
                .SingleOrDefaultAsync(item => item.RouteId == route.Id && item.CustomerId == customer.Id);
            Assert.NotNull(customerRoute);
            managedRouteSequence = customerRoute.Sort;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T8 配送分配路线最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T8配送调度操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008831",
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
                    Phone = "13900008832",
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
                    Title = "T8配送只读菜单",
                    Component = "page.t8.delivery.assign.seed",
                    MenuType = MenuType.Menu,
                    Order = 9851,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T8配送更新权限菜单",
                    Component = "page.t8.delivery.assign.write",
                    MenuType = MenuType.Menu,
                    Order = 9852,
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

            await seedContext.Drivers.AddAsync(new Driver
            {
                Id = disabledDriverId,
                Code = disabledDriverCode,
                Name = "T8临时停用调度司机",
                Phone = "13900008841",
                CarrierId = managedCarrierId,
                PlateNumber = "沪T8A0001",
                LicenseNo = "310115198800000001",
                Remark = $"{batch.Id}-禁用司机",
                Status = Status.Disable,
                CreateName = createName
            });

            await seedContext.SaveChangesAsync();
        }

        registry.Register<Role>(limitedRoleId, nameof(Role.Code), limitedRoleCode);
        registry.Register<User>(adminUserId, nameof(User.Username), adminUsername);
        registry.Register<User>(limitedUserId, nameof(User.Username), limitedUsername);
        registry.Register<Menu>(seedMenuId, nameof(Menu.Name), seedMenuName);
        registry.Register<Menu>(writeMenuId, nameof(Menu.Name), writeMenuName);
        registry.Register<Driver>(disabledDriverId, nameof(Driver.Code), disabledDriverCode);

        Guid? inboundOrderId = null;
        Guid? saleOrderId = null;
        Guid? saleOutOrderId = null;
        Guid? deliveryTaskId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousAssign = await anonymousClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [Guid.NewGuid()],
                           DriverId = managedDriverId
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousAssign, ResponseCode.Unauthorized);
            }

            using (var anonymousPlan = await anonymousClient.PutAsJsonAsync(
                       "/api/delivery-tasks/intelligent-plan",
                       new IntelligentPlanDeliveryTasksDto { TaskIds = [Guid.NewGuid()] }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousPlan, ResponseCode.Unauthorized);
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
                           remark = "T8配送分配路线切片销售订单",
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

            StockOutOrderDto saleOutOrder;
            using (var createSaleOut = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/sale",
                       BuildCreateSaleOutPayload(
                           managedWareId,
                           saleOrder.Id,
                           managedCustomerId,
                           createdBatchId!.Value,
                           managedGoodsUnitId,
                           saleOrderDetail.Id,
                           saleQuantity,
                           saleUnitPrice,
                           saleOutRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createSaleOut.StatusCode);
                saleOutOrder = await ReadApiDataAsync<StockOutOrderDto>(createSaleOut);
                saleOutOrderId = saleOutOrder.Id;
                registry.Register<StockOutOrder>(saleOutOrder.Id, nameof(StockOutOrder.Remark), saleOutRemark);
            }

            using (var auditSaleOut = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/sale/{saleOutOrder.Id}/audit",
                       new StockOutAuditDto { Remark = $"{saleOutRemark}-审核" }))
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
                deliveryTaskId = task.Id;
                registry.Register<DeliveryTask>(task.Id, nameof(DeliveryTask.Remark), saleOutRemark);
            }

            Assert.Equal(DeliveryTaskStatus.PendingAssign, task.DeliveryStatus);
            Assert.Null(task.DriverId);
            Assert.Null(task.RouteId);

            using (var rejectDisabled = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [task.Id],
                           DriverId = disabledDriverId
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectDisabled, ResponseCode.DatabaseError);
            }

            await using (var afterDisabled = fixture.CreateDbContext())
            {
                var persisted = await afterDisabled.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task.Id);
                Assert.Equal(DeliveryTaskStatus.PendingAssign, persisted.DeliveryStatus);
                Assert.Null(persisted.DriverId);
                Assert.Null(persisted.AssignedTime);
            }

            List<DeliveryTaskDto> assignedList;
            using (var assign = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [task.Id],
                           DriverId = managedDriverId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
                assignedList = await ReadApiDataAsync<List<DeliveryTaskDto>>(assign);
            }

            var assigned = Assert.Single(assignedList);
            Assert.Equal(DeliveryTaskStatus.Assigned, assigned.DeliveryStatus);
            Assert.Equal(managedDriverId, assigned.DriverId);
            Assert.Equal(managedDriverName, assigned.DriverName);
            Assert.Equal(managedDriverPhone, assigned.DriverPhone);
            Assert.Equal(managedCarrierId, assigned.CarrierId);
            Assert.Equal(managedCarrierName, assigned.CarrierName);
            Assert.NotNull(assigned.AssignedTime);
            Assert.Null(assigned.RouteId);

            List<DeliveryTaskDto> plannedList;
            using (var plan = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/intelligent-plan",
                       new IntelligentPlanDeliveryTasksDto { TaskIds = [task.Id] }))
            {
                Assert.Equal(HttpStatusCode.OK, plan.StatusCode);
                plannedList = await ReadApiDataAsync<List<DeliveryTaskDto>>(plan);
            }

            var planned = Assert.Single(plannedList);
            Assert.Equal(DeliveryTaskStatus.Assigned, planned.DeliveryStatus);
            Assert.Equal(managedRouteId, planned.RouteId);
            Assert.Equal(managedRouteName, planned.RouteName);
            Assert.Equal(managedRouteSequence, planned.RouteSequence);
            Assert.NotNull(planned.PlannedTime);
            Assert.Equal(managedDriverId, planned.DriverId);

            List<DeliveryTaskDto> reassignedList;
            using (var reassign = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [task.Id],
                           DriverId = managedDriver2Id
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, reassign.StatusCode);
                reassignedList = await ReadApiDataAsync<List<DeliveryTaskDto>>(reassign);
            }

            var reassigned = Assert.Single(reassignedList);
            Assert.Equal(DeliveryTaskStatus.Assigned, reassigned.DeliveryStatus);
            Assert.Equal(managedDriver2Id, reassigned.DriverId);
            Assert.Equal(managedDriver2Name, reassigned.DriverName);
            Assert.Equal(managedRouteId, reassigned.RouteId);
            Assert.Equal(managedRouteName, reassigned.RouteName);

            DeliveryTaskDto started;
            using (var start = await adminClient.PutAsync($"/api/delivery-tasks/{task.Id}/start", null))
            {
                Assert.Equal(HttpStatusCode.OK, start.StatusCode);
                started = await ReadApiDataAsync<DeliveryTaskDto>(start);
            }

            Assert.Equal(DeliveryTaskStatus.Delivering, started.DeliveryStatus);
            Assert.NotNull(started.StartedTime);
            Assert.Equal(managedDriver2Id, started.DriverId);
            Assert.Equal(managedRouteId, started.RouteId);

            await using (var afterStart = fixture.CreateDbContext())
            {
                var persistedTask = await afterStart.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task.Id);
                Assert.Equal(DeliveryTaskStatus.Delivering, persistedTask.DeliveryStatus);
                Assert.Equal(managedDriver2Id, persistedTask.DriverId);
                Assert.Equal(managedRouteId, persistedTask.RouteId);
                Assert.Equal(managedRouteName, persistedTask.RouteNameSnapshot);
                Assert.Equal(managedRouteSequence, persistedTask.RouteSequence);
                Assert.NotNull(persistedTask.AssignedTime);
                Assert.NotNull(persistedTask.PlannedTime);
                Assert.NotNull(persistedTask.StartedTime);

                var persistedOrder = await afterStart.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == saleOrder.Id);
                Assert.Equal(SaleOrderStatus.Delivering, persistedOrder.OrderStatus);
            }

            using (var rejectAssignAfterStart = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [task.Id],
                           DriverId = managedDriverId
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectAssignAfterStart, ResponseCode.DatabaseError);
            }

            using (var rejectPlanAfterStart = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/intelligent-plan",
                       new IntelligentPlanDeliveryTasksDto { TaskIds = [task.Id] }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectPlanAfterStart, ResponseCode.DatabaseError);
            }

            await using (var afterReject = fixture.CreateDbContext())
            {
                var persisted = await afterReject.DeliveryTasks.AsNoTracking()
                    .SingleAsync(item => item.Id == task.Id);
                Assert.Equal(DeliveryTaskStatus.Delivering, persisted.DeliveryStatus);
                Assert.Equal(managedDriver2Id, persisted.DriverId);
                Assert.Equal(managedRouteId, persisted.RouteId);
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
                Assert.DoesNotContain(deliveryUpdatePermission, info.Buttons);
            }

            using (var allowedList = await limitedClient.GetAsync(
                       $"/api/delivery-tasks?current=1&size=20&keyword={Uri.EscapeDataString(task.TaskNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<DeliveryTaskDto>>(allowedList);
                Assert.Contains(page.Records!, item => item.Id == task.Id);
            }

            using (var deniedAssign = await limitedClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [task.Id],
                           DriverId = managedDriverId
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAssign, ResponseCode.Forbidden);
            }

            using (var deniedPlan = await limitedClient.PutAsJsonAsync(
                       "/api/delivery-tasks/intelligent-plan",
                       new IntelligentPlanDeliveryTasksDto { TaskIds = [task.Id] }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedPlan, ResponseCode.Forbidden);
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
            Assert.True(await auditContext.DeliveryRoutes.AnyAsync(item =>
                item.Id == managedRouteId && item.Code == DemoDataStableKeyCatalog.Create("DELIVERY-ROUTE", 1)));
            Assert.True(await auditContext.CustomerRoutes.AnyAsync(item =>
                item.RouteId == managedRouteId && item.CustomerId == managedCustomerId));
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
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

                var residualButtonIds = new List<Guid> { seedReadButtonId, writeUpdateButtonId };
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

                var residualDrivers = await cleanupContext.Drivers
                    .Where(driver => driver.Id == disabledDriverId
                                     || driver.Code == disabledDriverCode
                                     || (driver.Code != null && driver.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualDrivers.Count > 0)
                {
                    cleanupContext.Drivers.RemoveRange(residualDrivers);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await using var residualContext = fixture.CreateDbContext();
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
                || button.Id == writeUpdateButtonId
                || button.CreateName == createName));
            Assert.False(await residualContext.Drivers.AnyAsync(driver =>
                driver.Id == disabledDriverId
                || driver.Code == disabledDriverCode
                || (driver.Code != null && driver.Code.StartsWith(batch.Id))));
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
            Assert.True(await residualContext.DeliveryRoutes.AnyAsync(item => item.Id == managedRouteId));
            Assert.True(await residualContext.CustomerRoutes.AnyAsync(item =>
                item.RouteId == managedRouteId && item.CustomerId == managedCustomerId));
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
