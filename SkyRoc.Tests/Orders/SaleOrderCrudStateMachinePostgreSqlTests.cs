using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Orders;
using Application.DTOs.Role;
using Domain.Entities;
using Domain.Entities.Orders;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Orders;

/// <summary>
///     T5 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证销售订单 CRUD、金额与快照、审核状态机及权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class SaleOrderCrudStateMachinePostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/查询/更新/删除订单并走驳回→重提→通过状态机；金额与客户/商品快照正确；最小权限仅读；写/审核拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task SaleOrder_CrudSnapshotStateMachineAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedOrderReadButtonId = Guid.NewGuid();
        var writeMenuId = Guid.NewGuid();
        var writeCreateButtonId = Guid.NewGuid();
        var writeUpdateButtonId = Guid.NewGuid();
        var writeDeleteButtonId = Guid.NewGuid();
        var auditMenuId = Guid.NewGuid();
        var auditButtonId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username/role/code ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var writeMenuName = $"{batch.Id}W";
        var auditMenuName = $"{batch.Id}U";
        var targetInnerRemark = $"{batch.Id}O";
        var deleteInnerRemark = $"{batch.Id}D";
        var expandedInnerRemark = $"{batch.Id}E";
        var targetDetailInnerRemark = $"{batch.Id}L1";
        var password = "SkyRocOrderPerm!2026";
        var userAgent = $"SkyRoc-T5-Order/{batch.Id}";
        var createName = "T5-SaleOrderCrud";

        var orderReadPermission = PermissionCodes.Business.Orders.Read;
        var orderCreatePermission = PermissionCodes.Business.Orders.Create;
        var orderUpdatePermission = PermissionCodes.Business.Orders.Update;
        var orderDeletePermission = PermissionCodes.Business.Orders.Delete;
        var orderAuditPermission = PermissionCodes.Business.Orders.Audit;

        var createQuantity = NumericPrecision.RoundQuantity(2m);
        var createFixedPrice = NumericPrecision.RoundMoney(15.5m);
        var updateQuantity = NumericPrecision.RoundQuantity(3m);
        var updateFixedPrice = NumericPrecision.RoundMoney(10m);

        Guid adminRoleId;
        Guid managedCustomerId;
        string managedCustomerCode = null!;
        string managedCustomerName = null!;
        Guid managedGoodsId;
        string managedGoodsCode = null!;
        string managedGoodsName = null!;
        Guid managedGoodsUnitId;
        string managedGoodsUnitName = null!;
        decimal managedUnitConversion;
        Guid managedWareId;
        decimal expectedCreateBaseQuantity;
        decimal expectedCreateTotalPrice;
        decimal expectedUpdateBaseQuantity;
        decimal expectedUpdateTotalPrice;

        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            // 复用 T2 受管基础资料，不修改非受管主数据
            var customerCode = DemoDataStableKeyCatalog.Create("CUSTOMER", 1);
            var goodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var goodsUnitCode = DemoDataStableKeyCatalog.Create("GOODS-UNIT", 1);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);

            var customer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == customerCode);
            Assert.NotNull(customer);
            managedCustomerId = customer.Id;
            managedCustomerCode = customer.Code;
            managedCustomerName = customer.Name;

            var goods = await seedContext.Set<GoodsEntity>().AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsCode);
            Assert.NotNull(goods);
            managedGoodsId = goods.Id;
            managedGoodsCode = goods.Code;
            managedGoodsName = goods.Name;

            var goodsUnit = await seedContext.GoodsUnits.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsUnitCode);
            Assert.NotNull(goodsUnit);
            Assert.Equal(goods.Id, goodsUnit.GoodsId);
            managedGoodsUnitId = goodsUnit.Id;
            managedGoodsUnitName = goodsUnit.Name;
            managedUnitConversion = goodsUnit.ConversionRate;
            Assert.True(managedUnitConversion > 0);

            var ware = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == wareCode);
            Assert.NotNull(ware);
            managedWareId = ware.Id;

            expectedCreateBaseQuantity = NumericPrecision.RoundQuantity(createQuantity * managedUnitConversion);
            expectedCreateTotalPrice = NumericPrecision.RoundMoney(
                expectedCreateBaseQuantity / managedUnitConversion * createFixedPrice);
            expectedUpdateBaseQuantity = NumericPrecision.RoundQuantity(updateQuantity * managedUnitConversion);
            expectedUpdateTotalPrice = NumericPrecision.RoundMoney(
                expectedUpdateBaseQuantity / managedUnitConversion * updateFixedPrice);

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T5 销售订单权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T5订单操作员",
                    Gender = GenderType.Male,
                    Phone = "13900009501",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T5订单只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900009502",
                    Email = $"{batch.Id}-l@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.UserRoles.AddRangeAsync(
                new UserRole
                {
                    UserId = adminUserId,
                    RoleId = adminRoleId
                },
                new UserRole
                {
                    UserId = limitedUserId,
                    RoleId = limitedRoleId
                });

            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = seedMenuId,
                    Name = seedMenuName,
                    Path = $"/{batch.Id}s",
                    Title = "T5订单只读菜单",
                    Component = "page.t5.order.seed",
                    MenuType = MenuType.Menu,
                    Order = 9701,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T5订单写权限菜单",
                    Component = "page.t5.order.write",
                    MenuType = MenuType.Menu,
                    Order = 9702,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = auditMenuId,
                    Name = auditMenuName,
                    Path = $"/{batch.Id}u",
                    Title = "T5订单审核权限菜单",
                    Component = "page.t5.order.audit",
                    MenuType = MenuType.Menu,
                    Order = 9703,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedOrderReadButtonId,
                    Code = orderReadPermission,
                    Desc = "T5 订单读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = orderCreatePermission,
                    Desc = "T5 订单创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = orderUpdatePermission,
                    Desc = "T5 订单更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = orderDeletePermission,
                    Desc = "T5 订单删除权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = auditButtonId,
                    Code = orderAuditPermission,
                    Desc = "T5 订单审核权限按钮",
                    MenuId = auditMenuId,
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
        registry.Register<Menu>(auditMenuId, nameof(Menu.Name), auditMenuName);

        Guid? targetOrderId = null;
        Guid? deleteOrderId = null;
        Guid? expandedOrderId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问订单接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/orders/list?current=1&size=10"))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousList.StatusCode);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/orders",
                       new CreateSaleOrderDto
                       {
                           CustomerId = managedCustomerId,
                           OrderDate = new DateTime(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc),
                           Details =
                           [
                               new CreateSaleOrderDetailDto
                               {
                                   GoodsId = managedGoodsId,
                                   GoodsUnitId = managedGoodsUnitId,
                                   Quantity = createQuantity,
                                   FixedPrice = createFixedPrice,
                                   FixedGoodsUnitId = managedGoodsUnitId
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousCreate.StatusCode);
            }

            // 操作员登录（Admin → *:*:*）
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

            // 操作员创建目标订单：金额、客户/商品快照、提交审核轨迹
            // HTTP JSON 日期需带 Z 后缀，避免 FixedDateTimeJsonConverter 写出无时区导致 timestamptz 写入失败
            SaleOrderDto targetOrder;
            using (var createTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-17T08:30:00Z",
                           receiveDate = "2026-07-18T06:00:00Z",
                           contactName = "张食堂",
                           contactPhone = "13800139501",
                           deliveryAddress = "上海市浦东新区联调路 95 号食堂后门",
                           remark = "T5销售订单CRUD切片目标订单",
                           innerRemark = targetInnerRemark,
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = createQuantity,
                                   fixedPrice = createFixedPrice,
                                   fixedGoodsUnitId = managedGoodsUnitId,
                                   remark = "目标订单明细",
                                   innerRemark = targetDetailInnerRemark
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createTargetResponse.StatusCode);
                targetOrder = await ReadApiDataAsync<SaleOrderDto>(createTargetResponse);
                Assert.StartsWith("SO", targetOrder.OrderNo);
                Assert.Equal(managedCustomerId, targetOrder.CustomerId);
                Assert.Equal(managedCustomerName, targetOrder.CustomerName);
                Assert.Equal(managedCustomerCode, targetOrder.CustomerCode);
                Assert.Equal(managedWareId, targetOrder.WareId);
                Assert.Equal(SaleOrderStatus.PendingAudit, targetOrder.OrderStatus);
                Assert.Equal(expectedCreateTotalPrice, targetOrder.OrderPrice);
                Assert.Equal(expectedCreateTotalPrice, targetOrder.SettlementPrice);
                Assert.Equal("张食堂", targetOrder.ContactName);
                Assert.Equal(targetInnerRemark, targetOrder.InnerRemark);
                var detail = Assert.Single(targetOrder.Details);
                Assert.Equal(managedGoodsId, detail.GoodsId);
                Assert.Equal(managedGoodsName, detail.GoodsName);
                Assert.Equal(managedGoodsCode, detail.GoodsCode);
                Assert.Equal(managedGoodsUnitId, detail.GoodsUnitId);
                Assert.Equal(managedGoodsUnitName, detail.GoodsUnitName);
                Assert.Equal(createQuantity, detail.Quantity);
                Assert.Equal(expectedCreateBaseQuantity, detail.BaseQuantity);
                Assert.Equal(createFixedPrice, detail.FixedPrice);
                Assert.Equal(expectedCreateTotalPrice, detail.TotalPrice);
                Assert.Equal(targetDetailInnerRemark, detail.InnerRemark);
                var submitLog = Assert.Single(targetOrder.AuditLogs);
                Assert.Equal(OrderAuditAction.Submit, submitLog.Action);
                Assert.Equal(SaleOrderStatus.PendingAudit, submitLog.PreviousStatus);
                Assert.Equal(SaleOrderStatus.PendingAudit, submitLog.CurrentStatus);
                Assert.Equal(adminUserId, submitLog.AuditUserId);
                targetOrderId = targetOrder.Id;
                registry.Register<SaleOrder>(targetOrder.Id, nameof(SaleOrder.InnerRemark), targetInnerRemark);
            }

            // 库侧核对主单/明细/审核日志副作用
            await using (var afterCreate = fixture.CreateDbContext())
            {
                var orderEntity = await afterCreate.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == targetOrder.Id);
                Assert.Equal(managedCustomerId, orderEntity.CustomerId);
                Assert.Equal(managedCustomerName, orderEntity.CustomerNameSnapshot);
                Assert.Equal(managedCustomerCode, orderEntity.CustomerCodeSnapshot);
                Assert.Equal(expectedCreateTotalPrice, orderEntity.OrderPrice);
                Assert.Equal(SaleOrderStatus.PendingAudit, orderEntity.OrderStatus);
                Assert.Equal(targetInnerRemark, orderEntity.InnerRemark);

                var detailEntity = await afterCreate.SaleOrderDetails.AsNoTracking()
                    .SingleAsync(item => item.SaleOrderId == targetOrder.Id);
                Assert.Equal(managedGoodsName, detailEntity.GoodsNameSnapshot);
                Assert.Equal(managedGoodsCode, detailEntity.GoodsCodeSnapshot);
                Assert.Equal(expectedCreateBaseQuantity, detailEntity.BaseQuantity);
                Assert.Equal(expectedCreateTotalPrice, detailEntity.TotalPrice);

                var auditLogs = await afterCreate.OrderAuditLogs.AsNoTracking()
                    .Where(log => log.SaleOrderId == targetOrder.Id)
                    .ToListAsync();
                Assert.Single(auditLogs);
                Assert.Equal(OrderAuditAction.Submit, auditLogs[0].Action);
            }

            // 分页筛选 + 详情
            using (var listResponse = await adminClient.GetAsync(
                       $"/api/orders/list?current=1&size=20&keyword={Uri.EscapeDataString(targetOrder.OrderNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<SaleOrderDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == targetOrder.Id);
            }

            using (var detailResponse = await adminClient.GetAsync($"/api/orders/{targetOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
                var detail = await ReadApiDataAsync<SaleOrderDto>(detailResponse);
                Assert.Equal(targetOrder.OrderNo, detail.OrderNo);
                Assert.Equal(expectedCreateTotalPrice, detail.OrderPrice);
                Assert.Single(detail.Details);
                Assert.Single(detail.AuditLogs);
            }

            // 更新明细并重算金额
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           id = targetOrder.Id,
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-17T08:30:00Z",
                           receiveDate = "2026-07-18T06:00:00Z",
                           contactName = "张食堂-已更新",
                           contactPhone = "13800139599",
                           deliveryAddress = "上海市浦东新区联调路 99 号食堂",
                           remark = "T5销售订单CRUD切片目标订单-已更新",
                           innerRemark = targetInnerRemark,
                           details = new[]
                           {
                               new
                               {
                                   id = targetOrder.Details[0].Id,
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = updateQuantity,
                                   fixedPrice = updateFixedPrice,
                                   fixedGoodsUnitId = managedGoodsUnitId,
                                   remark = "目标订单明细-已更新",
                                   innerRemark = targetDetailInnerRemark
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var updated = await ReadApiDataAsync<SaleOrderDto>(updateResponse);
                Assert.Equal("张食堂-已更新", updated.ContactName);
                Assert.Equal("13800139599", updated.ContactPhone);
                Assert.Equal(expectedUpdateTotalPrice, updated.OrderPrice);
                Assert.Equal(expectedUpdateTotalPrice, updated.SettlementPrice);
                Assert.True(updated.UpdateStatus);
                var detail = Assert.Single(updated.Details);
                Assert.Equal(updateQuantity, detail.Quantity);
                Assert.Equal(expectedUpdateBaseQuantity, detail.BaseQuantity);
                Assert.Equal(updateFixedPrice, detail.FixedPrice);
                Assert.Equal(expectedUpdateTotalPrice, detail.TotalPrice);
                targetOrder = updated;
            }

            // 非法状态：待审核不能重提
            using (var illegalResubmit = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{targetOrder.Id}/resubmit",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-非法重提" }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, illegalResubmit.StatusCode);
            }

            // 驳回
            using (var rejectResponse = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{targetOrder.Id}/reject",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-价格需确认" }))
            {
                Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
                var rejected = await ReadApiDataAsync<SaleOrderDto>(rejectResponse);
                Assert.Equal(SaleOrderStatus.Rejected, rejected.OrderStatus);
                Assert.Contains(rejected.AuditLogs, log =>
                    log.Action == OrderAuditAction.Reject
                    && log.PreviousStatus == SaleOrderStatus.PendingAudit
                    && log.CurrentStatus == SaleOrderStatus.Rejected
                    && log.Remark == $"{batch.Id}-价格需确认");
                targetOrder = rejected;
            }

            // 非法状态：已驳回不能直接通过
            using (var illegalApprove = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{targetOrder.Id}/approve",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-非法通过" }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, illegalApprove.StatusCode);
            }

            // 重提
            using (var resubmitResponse = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{targetOrder.Id}/resubmit",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-价格已确认" }))
            {
                Assert.Equal(HttpStatusCode.OK, resubmitResponse.StatusCode);
                var resubmitted = await ReadApiDataAsync<SaleOrderDto>(resubmitResponse);
                Assert.Equal(SaleOrderStatus.PendingAudit, resubmitted.OrderStatus);
                Assert.Contains(resubmitted.AuditLogs, log =>
                    log.Action == OrderAuditAction.Resubmit
                    && log.PreviousStatus == SaleOrderStatus.Rejected
                    && log.CurrentStatus == SaleOrderStatus.PendingAudit);
                targetOrder = resubmitted;
            }

            // 审核通过
            using (var approveResponse = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{targetOrder.Id}/approve",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-资料完整通过" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
                var approved = await ReadApiDataAsync<SaleOrderDto>(approveResponse);
                Assert.Equal(SaleOrderStatus.SortingPending, approved.OrderStatus);
                Assert.Equal(4, approved.AuditLogs.Count);
                Assert.Equal(
                    [
                        OrderAuditAction.Submit,
                        OrderAuditAction.Reject,
                        OrderAuditAction.Resubmit,
                        OrderAuditAction.Approve
                    ],
                    approved.AuditLogs.OrderBy(log => log.AuditTime).Select(log => log.Action).ToArray());
                targetOrder = approved;
            }

            // 非法状态：已通过不能重复通过/驳回
            using (var doubleApprove = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{targetOrder.Id}/approve",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-重复通过" }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, doubleApprove.StatusCode);
            }

            using (var rejectAfterApprove = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{targetOrder.Id}/reject",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-通过后驳回" }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, rejectAfterApprove.StatusCode);
            }

            await using (var afterStateMachine = fixture.CreateDbContext())
            {
                var orderEntity = await afterStateMachine.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == targetOrder.Id);
                Assert.Equal(SaleOrderStatus.SortingPending, orderEntity.OrderStatus);
                Assert.Equal(expectedUpdateTotalPrice, orderEntity.OrderPrice);

                var auditCount = await afterStateMachine.OrderAuditLogs.AsNoTracking()
                    .CountAsync(log => log.SaleOrderId == targetOrder.Id);
                Assert.Equal(4, auditCount);

                // 受管客户/商品未被改写
                var managedCustomer = await afterStateMachine.Customers.AsNoTracking()
                    .SingleAsync(item => item.Id == managedCustomerId);
                Assert.Equal(managedCustomerCode, managedCustomer.Code);
                Assert.Equal(managedCustomerName, managedCustomer.Name);

                var managedGoods = await afterStateMachine.Set<GoodsEntity>().AsNoTracking()
                    .SingleAsync(item => item.Id == managedGoodsId);
                Assert.Equal(managedGoodsCode, managedGoods.Code);
                Assert.Equal(managedGoodsName, managedGoods.Name);
            }

            // 创建待删除草稿订单（删除级联明细与审核日志）
            SaleOrderDto deleteOrder;
            using (var createDeleteResponse = await adminClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-17T09:00:00Z",
                           contactName = "赵删除",
                           contactPhone = "13800139503",
                           deliveryAddress = "上海市闵行区删除路 1 号",
                           remark = "T5销售订单CRUD切片删除订单",
                           innerRemark = deleteInnerRemark,
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = NumericPrecision.RoundQuantity(1m),
                                   fixedPrice = NumericPrecision.RoundMoney(8m),
                                   fixedGoodsUnitId = managedGoodsUnitId,
                                   remark = "删除订单明细",
                                   innerRemark = $"{batch.Id}LD"
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createDeleteResponse.StatusCode);
                deleteOrder = await ReadApiDataAsync<SaleOrderDto>(createDeleteResponse);
                deleteOrderId = deleteOrder.Id;
                registry.Register<SaleOrder>(deleteOrder.Id, nameof(SaleOrder.InnerRemark), deleteInnerRemark);
            }

            using (var deleteResponse = await adminClient.DeleteAsync($"/api/orders/{deleteOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteResponse));
            }

            await using (var afterDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterDelete.SaleOrders.AnyAsync(item => item.Id == deleteOrder.Id));
                Assert.False(await afterDelete.SaleOrderDetails.AnyAsync(item => item.SaleOrderId == deleteOrder.Id));
                Assert.False(await afterDelete.OrderAuditLogs.AnyAsync(item => item.SaleOrderId == deleteOrder.Id));
                deleteOrderId = null;
            }

            // 最小权限用户：仅订单读
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

            using (var limitedInfoResponse = await limitedClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, limitedInfoResponse.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(limitedInfoResponse);
                Assert.Equal(limitedUserId, info.UserId);
                Assert.Contains(limitedRoleCode, info.Roles);
                Assert.DoesNotContain(PermissionCodes.All, info.Permissions);
                Assert.Contains(orderReadPermission, info.Permissions);
                Assert.DoesNotContain(orderCreatePermission, info.Permissions);
                Assert.DoesNotContain(orderUpdatePermission, info.Permissions);
                Assert.DoesNotContain(orderDeletePermission, info.Permissions);
                Assert.DoesNotContain(orderAuditPermission, info.Permissions);
            }

            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
                Assert.DoesNotContain($"/{batch.Id}u", paths);
            }

            using (var allowedList = await limitedClient.GetAsync(
                       $"/api/orders/list?current=1&size=10&keyword={Uri.EscapeDataString(targetOrder.OrderNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/orders/{targetOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           orderDate = "2026-07-17T10:00:00Z",
                           innerRemark = $"{batch.Id}X",
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = 1m,
                                   fixedPrice = 1m,
                                   fixedGoodsUnitId = managedGoodsUnitId
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedCreate.StatusCode);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           id = targetOrder.Id,
                           customerId = managedCustomerId,
                           orderDate = "2026-07-17T08:30:00Z",
                           innerRemark = targetInnerRemark,
                           details = new[]
                           {
                               new
                               {
                                   id = targetOrder.Details[0].Id,
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = updateQuantity,
                                   fixedPrice = updateFixedPrice,
                                   fixedGoodsUnitId = managedGoodsUnitId
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedUpdate.StatusCode);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/orders/{targetOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedDelete.StatusCode);
            }

            using (var deniedReject = await limitedClient.PostAsJsonAsync(
                       $"/api/orders/{targetOrder.Id}/reject",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-无审核权限" }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedReject.StatusCode);
            }

            // 扩权写：分配写菜单后重新登录
            using (var expandWriteMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId, writeMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, expandWriteMenus.StatusCode);
            }

            LoginResDto limitedWriteLogin;
            using (var expandWriteLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, expandWriteLoginResponse.StatusCode);
                limitedWriteLogin = await ReadApiDataAsync<LoginResDto>(expandWriteLoginResponse);
            }

            using var limitedWriteClient = factory.CreateClient();
            limitedWriteClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedWriteLogin.Token);
            limitedWriteClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterWriteExpand = await limitedWriteClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterWriteExpand.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterWriteExpand);
                Assert.Contains(orderReadPermission, info.Permissions);
                Assert.Contains(orderCreatePermission, info.Permissions);
                Assert.Contains(orderUpdatePermission, info.Permissions);
                Assert.Contains(orderDeletePermission, info.Permissions);
                Assert.DoesNotContain(orderAuditPermission, info.Permissions);
            }

            // 有写无审核时审核仍 403
            using (var deniedAuditAfterWrite = await limitedWriteClient.PostAsJsonAsync(
                       $"/api/orders/{targetOrder.Id}/approve",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-有写无审核" }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedAuditAfterWrite.StatusCode);
            }

            SaleOrderDto expandedOrder;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-17T11:00:00Z",
                           contactName = "扩权联系人",
                           contactPhone = "13800139611",
                           deliveryAddress = "上海市徐汇区扩权路 1 号",
                           remark = "T5扩权订单",
                           innerRemark = expandedInnerRemark,
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = NumericPrecision.RoundQuantity(1m),
                                   fixedPrice = NumericPrecision.RoundMoney(12m),
                                   fixedGoodsUnitId = managedGoodsUnitId,
                                   remark = "扩权明细",
                                   innerRemark = $"{batch.Id}LE"
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedOrder = await ReadApiDataAsync<SaleOrderDto>(createExpanded);
                Assert.Equal(expandedInnerRemark, expandedOrder.InnerRemark);
                Assert.Equal(SaleOrderStatus.PendingAudit, expandedOrder.OrderStatus);
                expandedOrderId = expandedOrder.Id;
                registry.Register<SaleOrder>(expandedOrder.Id, nameof(SaleOrder.InnerRemark), expandedInnerRemark);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           id = expandedOrder.Id,
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-17T11:00:00Z",
                           contactName = "扩权联系人-已更新",
                           contactPhone = "13800139612",
                           deliveryAddress = "上海市徐汇区扩权路 2 号",
                           remark = "T5扩权订单-已更新",
                           innerRemark = expandedInnerRemark,
                           details = new[]
                           {
                               new
                               {
                                   id = expandedOrder.Details[0].Id,
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = NumericPrecision.RoundQuantity(2m),
                                   fixedPrice = NumericPrecision.RoundMoney(12m),
                                   fixedGoodsUnitId = managedGoodsUnitId,
                                   remark = "扩权明细-已更新",
                                   innerRemark = $"{batch.Id}LE"
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
                var updatedExpanded = await ReadApiDataAsync<SaleOrderDto>(updateExpanded);
                Assert.Equal("扩权联系人-已更新", updatedExpanded.ContactName);
                expandedOrder = updatedExpanded;
            }

            // 扩权审核：分配审核菜单后重新登录
            using (var expandAuditMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId, writeMenuId, auditMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, expandAuditMenus.StatusCode);
            }

            LoginResDto limitedAuditLogin;
            using (var expandAuditLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, expandAuditLoginResponse.StatusCode);
                limitedAuditLogin = await ReadApiDataAsync<LoginResDto>(expandAuditLoginResponse);
            }

            using var limitedAuditClient = factory.CreateClient();
            limitedAuditClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedAuditLogin.Token);
            limitedAuditClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterAuditExpand = await limitedAuditClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterAuditExpand.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterAuditExpand);
                Assert.Contains(orderAuditPermission, info.Permissions);
            }

            using (var rejectExpanded = await limitedAuditClient.PostAsJsonAsync(
                       $"/api/orders/{expandedOrder.Id}/reject",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-扩权驳回" }))
            {
                Assert.Equal(HttpStatusCode.OK, rejectExpanded.StatusCode);
                var rejectedExpanded = await ReadApiDataAsync<SaleOrderDto>(rejectExpanded);
                Assert.Equal(SaleOrderStatus.Rejected, rejectedExpanded.OrderStatus);
            }

            using (var deleteExpanded = await limitedAuditClient.DeleteAsync($"/api/orders/{expandedOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.SaleOrders.AnyAsync(item => item.Id == expandedOrder.Id));
                Assert.False(await afterExpandedDelete.SaleOrderDetails.AnyAsync(item =>
                    item.SaleOrderId == expandedOrder.Id));
                Assert.False(await afterExpandedDelete.OrderAuditLogs.AnyAsync(item =>
                    item.SaleOrderId == expandedOrder.Id));
                expandedOrderId = null;
            }

            // 缩权后写/审核权限与菜单路由收口
            using (var shrinkMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, shrinkMenus.StatusCode);
            }

            LoginResDto limitedRelogin;
            using (var reloginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, reloginResponse.StatusCode);
                limitedRelogin = await ReadApiDataAsync<LoginResDto>(reloginResponse);
            }

            using var limitedReloginClient = factory.CreateClient();
            limitedReloginClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedRelogin.Token);
            limitedReloginClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterShrink = await limitedReloginClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterShrink.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterShrink);
                Assert.Contains(orderReadPermission, info.Permissions);
                Assert.DoesNotContain(orderCreatePermission, info.Permissions);
                Assert.DoesNotContain(orderUpdatePermission, info.Permissions);
                Assert.DoesNotContain(orderDeletePermission, info.Permissions);
                Assert.DoesNotContain(orderAuditPermission, info.Permissions);
            }

            using (var routesAfterShrink = await limitedReloginClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesAfterShrink.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesAfterShrink);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
                Assert.DoesNotContain($"/{batch.Id}u", paths);
            }

            using (var deniedCreateAfterShrink = await limitedReloginClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           orderDate = "2026-07-17T12:00:00Z",
                           innerRemark = $"{batch.Id}F",
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = 1m,
                                   fixedPrice = 1m,
                                   fixedGoodsUnitId = managedGoodsUnitId
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.Forbidden, deniedCreateAfterShrink.StatusCode);
            }

            // 删除目标订单（明细与审核日志级联清理）
            using (var deleteTarget = await adminClient.DeleteAsync($"/api/orders/{targetOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteTarget.StatusCode);
            }

            await using (var afterDeleteTarget = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteTarget.SaleOrders.AnyAsync(item => item.Id == targetOrder.Id));
                Assert.False(await afterDeleteTarget.SaleOrderDetails.AnyAsync(item =>
                    item.SaleOrderId == targetOrder.Id));
                Assert.False(await afterDeleteTarget.OrderAuditLogs.AnyAsync(item =>
                    item.SaleOrderId == targetOrder.Id));
                targetOrderId = null;
            }

            await using var auditContext = fixture.CreateDbContext();
            var loginLogs = await auditContext.LoginLogs.AsNoTracking()
                .Where(log => log.Username == adminUsername || log.Username == limitedUsername)
                .ToListAsync();
            Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
            Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
            Assert.All(loginLogs, log =>
            {
                Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
            });
            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(
                fixture,
                registry,
                adminUsername,
                limitedUsername);
            await RegisterBatchOperationLogsAsync(
                fixture,
                registry,
                adminUsername,
                limitedUsername);

            // 先清本轮批次登记实体；UserRole 随用户级联，RoleMenu 随角色/菜单级联；订单明细/审核日志随订单级联
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUsernames = new[]
                {
                    adminUsername,
                    limitedUsername
                };

                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };

                // 兜底清理可能残留的临时订单及其明细/审核日志
                var residualInnerRemarks = new List<string>
                {
                    targetInnerRemark,
                    deleteInnerRemark,
                    expandedInnerRemark,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualOrderIds = new List<Guid>();
                if (targetOrderId.HasValue)
                    residualOrderIds.Add(targetOrderId.Value);
                if (deleteOrderId.HasValue)
                    residualOrderIds.Add(deleteOrderId.Value);
                if (expandedOrderId.HasValue)
                    residualOrderIds.Add(expandedOrderId.Value);

                var residualOrders = await cleanupContext.SaleOrders
                    .Where(item => residualOrderIds.Contains(item.Id)
                                   || (item.InnerRemark != null
                                       && (residualInnerRemarks.Contains(item.InnerRemark)
                                           || item.InnerRemark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualOrders.Count > 0)
                {
                    residualOrderIds = residualOrders.Select(item => item.Id).Distinct().ToList();
                    var residualDetails = await cleanupContext.SaleOrderDetails
                        .Where(detail => residualOrderIds.Contains(detail.SaleOrderId))
                        .ToListAsync();
                    if (residualDetails.Count > 0)
                    {
                        cleanupContext.SaleOrderDetails.RemoveRange(residualDetails);
                        await cleanupContext.SaveChangesAsync();
                    }

                    var residualAuditLogs = await cleanupContext.OrderAuditLogs
                        .Where(log => residualOrderIds.Contains(log.SaleOrderId))
                        .ToListAsync();
                    if (residualAuditLogs.Count > 0)
                    {
                        cleanupContext.OrderAuditLogs.RemoveRange(residualAuditLogs);
                        await cleanupContext.SaveChangesAsync();
                    }

                    cleanupContext.SaleOrders.RemoveRange(residualOrders);
                    await cleanupContext.SaveChangesAsync();
                }

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

                var residualRoleCodes = new List<string>
                {
                    limitedRoleCode
                };
                var residualRoleIds = new List<Guid> { limitedRoleId };

                var residualRoleMenus = await cleanupContext.RoleMenus
                    .Where(relation => residualRoleIds.Contains(relation.RoleId)
                                       || relation.MenuId == seedMenuId
                                       || relation.MenuId == writeMenuId
                                       || relation.MenuId == auditMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoles = await cleanupContext.Roles
                    .Where(role => residualRoleIds.Contains(role.Id)
                                   || residualRoleCodes.Contains(role.Code)
                                   || (role.Code != null && role.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualRoles.Count > 0)
                {
                    cleanupContext.Roles.RemoveRange(residualRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtonIds = new List<Guid>
                {
                    seedOrderReadButtonId,
                    writeCreateButtonId,
                    writeUpdateButtonId,
                    writeDeleteButtonId,
                    auditButtonId
                };
                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => residualButtonIds.Contains(button.Id)
                                     || button.MenuId == seedMenuId
                                     || button.MenuId == writeMenuId
                                     || button.MenuId == auditMenuId
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
                                   || menu.Id == auditMenuId
                                   || menu.Name == seedMenuName
                                   || menu.Name == writeMenuName
                                   || menu.Name == auditMenuName)
                    .ToListAsync();
                if (residualMenus.Count > 0)
                {
                    cleanupContext.Menus.RemoveRange(residualMenus);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await using var residualContext = fixture.CreateDbContext();
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
                || menu.Id == auditMenuId
                || menu.Name == seedMenuName
                || menu.Name == writeMenuName
                || menu.Name == auditMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedOrderReadButtonId
                || button.Id == writeCreateButtonId
                || button.Id == writeUpdateButtonId
                || button.Id == writeDeleteButtonId
                || button.Id == auditButtonId
                || button.CreateName == createName));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == adminUserId || relation.UserId == limitedUserId));
            Assert.False(await residualContext.RoleMenus.AnyAsync(relation =>
                relation.RoleId == limitedRoleId
                || relation.MenuId == seedMenuId
                || relation.MenuId == writeMenuId
                || relation.MenuId == auditMenuId));
            Assert.False(await residualContext.SaleOrders.AnyAsync(item =>
                item.InnerRemark == targetInnerRemark
                || item.InnerRemark == deleteInnerRemark
                || item.InnerRemark == expandedInnerRemark
                || (item.InnerRemark != null && item.InnerRemark.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            // 既有 Admin 角色与受管主数据必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
            Assert.True(await residualContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
        }
    }

    private static IEnumerable<string> FlattenRoutePaths(IEnumerable<RoutesDto> routes)
    {
        foreach (var route in routes)
        {
            if (!string.IsNullOrWhiteSpace(route.Path))
                yield return route.Path!;
            if (route.Children is null)
                continue;
            foreach (var childPath in FlattenRoutePaths(route.Children))
                yield return childPath;
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
