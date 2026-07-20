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
///     T9 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证售后类型（仅退款/退货退款/补货/换货）
///     审核状态机、取货任务生成边界、数量占用与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class AfterSaleTypeStateMachinePostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库建批次 → 销售订单审核 → 仅退款草稿提交/驳回/重提/审核/反审/完成 →
    ///     退货退款审核生成取货任务且不可反审 → 补货/换货进入待退货 →
    ///     累计数量超限拒绝 → 未审核订单拒绝建单 → 401/403 权限矩阵；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task AfterSale_TypeStateMachinePermissionAndReservation_OnPostgreSql()
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
        var pendingOrderInnerRemark = $"{batch.Id}P";
        var refundRemark = $"{batch.Id}-仅退款";
        var returnRemark = $"{batch.Id}-退货退款";
        var replenishRemark = $"{batch.Id}-补货处理";
        var exchangeRemark = $"{batch.Id}-换货处理";
        var draftDeleteRemark = $"{batch.Id}-待删草稿";
        var source = batch.Id;
        var password = "SkyRocAfterSaleType!2026";
        var userAgent = $"SkyRoc-T9-AfterSaleType/{batch.Id}";
        var createName = "T9-AfterSaleTypeStateMachine";
        var contactName = "售后状态机李老师";
        var contactPhone = "13900009901";
        var pickupAddress = $"{batch.Id}-上海市浦东新区售后取货路 9 号食堂后门";
        var deliveryAddress = "上海市浦东新区售后联调路 18 号食堂西门";

        var afterSalesReadPermission = PermissionCodes.Business.AfterSales.Read;
        var afterSalesCreatePermission = PermissionCodes.Business.AfterSales.Create;
        var afterSalesAuditPermission = PermissionCodes.Business.AfterSales.Audit;

        var inboundQuantity = NumericPrecision.RoundQuantity(20m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(5m);
        var saleQuantity = NumericPrecision.RoundQuantity(10m);
        var saleUnitPrice = NumericPrecision.RoundMoney(8m);
        var expectedOrderAmount = NumericPrecision.RoundMoney(saleQuantity * saleUnitPrice);

        Guid adminRoleId;
        Guid managedCustomerId;
        Guid managedGoodsId;
        Guid managedGoodsUnitId;
        Guid managedWareId;

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

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T9 售后状态机最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T9售后状态机操作员",
                    Gender = GenderType.Male,
                    Phone = "13900009911",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T9售后只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900009912",
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
                Title = "T9售后只读菜单",
                Component = "page.t9.aftersale.seed",
                MenuType = MenuType.Menu,
                Order = 9941,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedReadButtonId,
                Code = afterSalesReadPermission,
                Desc = "T9 售后读取权限按钮",
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
        Guid? pendingOrderId = null;
        Guid? refundAfterSaleId = null;
        Guid? returnAfterSaleId = null;
        Guid? replenishAfterSaleId = null;
        Guid? exchangeAfterSaleId = null;
        Guid? createdBatchId = null;
        var pickupTaskIds = new List<Guid>();

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousList = await anonymousClient.GetAsync("/api/after-sales?current=1&size=10"))
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
                           "T9售后状态机销售订单",
                           saleOrderInnerRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createOrder.StatusCode);
                saleOrder = await ReadApiDataAsync<SaleOrderDto>(createOrder);
                saleOrderId = saleOrder.Id;
                registry.Register<SaleOrder>(saleOrder.Id, nameof(SaleOrder.InnerRemark), saleOrderInnerRemark);
            }

            var saleOrderDetail = Assert.Single(saleOrder.Details);
            Assert.Equal(expectedOrderAmount, saleOrder.OrderPrice);

            using (var approveOrder = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{saleOrder.Id}/approve",
                       new SaleOrderAuditDto { Remark = $"{saleOrderInnerRemark}-审核通过" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveOrder.StatusCode);
                var approved = await ReadApiDataAsync<SaleOrderDto>(approveOrder);
                Assert.Equal(SaleOrderStatus.SortingPending, approved.OrderStatus);
            }

            SaleOrderDto pendingOrder;
            using (var createPending = await adminClient.PostAsJsonAsync(
                       "/api/orders",
                       BuildCreateSaleOrderPayload(
                           managedCustomerId,
                           managedWareId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           NumericPrecision.RoundQuantity(1m),
                           saleUnitPrice,
                           contactName,
                           contactPhone,
                           deliveryAddress,
                           "T9售后未审核订单",
                           pendingOrderInnerRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createPending.StatusCode);
                pendingOrder = await ReadApiDataAsync<SaleOrderDto>(createPending);
                pendingOrderId = pendingOrder.Id;
                registry.Register<SaleOrder>(pendingOrder.Id, nameof(SaleOrder.InnerRemark), pendingOrderInnerRemark);
            }

            var pendingDetail = Assert.Single(pendingOrder.Details);
            using (var rejectPendingAfterSale = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           pendingOrder.Id,
                           managedCustomerId,
                           pendingDetail.Id,
                           NumericPrecision.RoundQuantity(1m),
                           AfterSaleType.RefundOnly,
                           AfterSaleHandleType.GoodsDiscount,
                           source,
                           contactName,
                           contactPhone,
                           pickupAddress,
                           $"{batch.Id}-未审核拒绝")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectPendingAfterSale, ResponseCode.DatabaseError);
            }

            AfterSaleDto deletableDraft;
            using (var createDraft = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetail.Id,
                           NumericPrecision.RoundQuantity(1m),
                           AfterSaleType.RefundOnly,
                           AfterSaleHandleType.GoodsDiscount,
                           source,
                           contactName,
                           contactPhone,
                           pickupAddress,
                           draftDeleteRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createDraft.StatusCode);
                deletableDraft = await ReadApiDataAsync<AfterSaleDto>(createDraft);
                Assert.Equal(AfterSaleStatus.Draft, deletableDraft.AfterStatus);
            }

            using (var deleteDraft = await adminClient.DeleteAsync($"/api/after-sales/{deletableDraft.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteDraft.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteDraft));
            }

            Assert.False(await fixture.CreateDbContext().AfterSales.AnyAsync(item => item.Id == deletableDraft.Id));

            AfterSaleDto refundDraft;
            using (var createRefund = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetail.Id,
                           NumericPrecision.RoundQuantity(1m),
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

            Assert.Equal(AfterSaleStatus.Draft, refundDraft.AfterStatus);
            Assert.Equal(expectedOrderAmount, refundDraft.OrderPrice);
            Assert.Equal(NumericPrecision.RoundMoney(expectedOrderAmount - saleUnitPrice), refundDraft.SettlementPrice);
            Assert.Empty(refundDraft.PickupTasks);

            using (var prematureComplete = await adminClient.PostAsync(
                       $"/api/after-sales/{refundDraft.Id}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(prematureComplete, ResponseCode.DatabaseError);
            }

            AfterSaleDto submitted;
            using (var submit = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/submit",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-提交" }))
            {
                Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
                submitted = await ReadApiDataAsync<AfterSaleDto>(submit);
                Assert.Equal(AfterSaleStatus.PendingAudit, submitted.AfterStatus);
            }

            using (var doubleSubmit = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/submit",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-重复提交" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(doubleSubmit, ResponseCode.DatabaseError);
            }

            AfterSaleDto rejected;
            using (var reject = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/reject",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-数量需核实" }))
            {
                Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
                rejected = await ReadApiDataAsync<AfterSaleDto>(reject);
                Assert.Equal(AfterSaleStatus.Draft, rejected.AfterStatus);
            }

            using (var submitAfterReject = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/submit",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-驳回后提交" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(submitAfterReject, ResponseCode.DatabaseError);
            }

            AfterSaleDto updated;
            using (var update = await adminClient.PutAsJsonAsync(
                       "/api/after-sales",
                       new UpdateAfterSaleDto
                       {
                           Id = refundDraft.Id,
                           ContactName = contactName,
                           ContactPhone = contactPhone,
                           PickupAddress = pickupAddress,
                           Remark = refundRemark,
                           Goods =
                           [
                               new CreateAfterSaleGoodsDto
                               {
                                   SaleOrderDetailId = saleOrderDetail.Id,
                                   ActualRefundQuantity = NumericPrecision.RoundQuantity(2m),
                                   AfterSaleType = AfterSaleType.RefundOnly,
                                   ReasonType = AfterSaleReasonType.QualityIssue,
                                   HandleType = AfterSaleHandleType.GoodsDiscount,
                                   Remark = $"{refundRemark}-明细"
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, update.StatusCode);
                updated = await ReadApiDataAsync<AfterSaleDto>(update);
                Assert.Equal(NumericPrecision.RoundQuantity(2m), Assert.Single(updated.Goods).ActualRefundQuantity);
                Assert.Equal(
                    NumericPrecision.RoundMoney(expectedOrderAmount - saleUnitPrice * 2m),
                    updated.SettlementPrice);
            }

            AfterSaleDto resubmitted;
            using (var resubmit = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/resubmit",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-已核实重提" }))
            {
                Assert.Equal(HttpStatusCode.OK, resubmit.StatusCode);
                resubmitted = await ReadApiDataAsync<AfterSaleDto>(resubmit);
                Assert.Equal(AfterSaleStatus.PendingAudit, resubmitted.AfterStatus);
            }

            AfterSaleDto approvedRefund;
            using (var approve = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-同意仅退款" }))
            {
                Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
                approvedRefund = await ReadApiDataAsync<AfterSaleDto>(approve);
                Assert.Equal(AfterSaleStatus.RefundPending, approvedRefund.AfterStatus);
                Assert.Empty(approvedRefund.PickupTasks);
            }

            AfterSaleDto reversed;
            using (var reverse = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/reverse",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-财务信息有误反审" }))
            {
                Assert.Equal(HttpStatusCode.OK, reverse.StatusCode);
                reversed = await ReadApiDataAsync<AfterSaleDto>(reverse);
                Assert.Equal(AfterSaleStatus.PendingAudit, reversed.AfterStatus);
            }

            using (var approveAgain = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-再次审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveAgain.StatusCode);
                approvedRefund = await ReadApiDataAsync<AfterSaleDto>(approveAgain);
                Assert.Equal(AfterSaleStatus.RefundPending, approvedRefund.AfterStatus);
            }

            using (var approveIdempotent = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-幂等审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveIdempotent.StatusCode);
                var repeated = await ReadApiDataAsync<AfterSaleDto>(approveIdempotent);
                Assert.Equal(AfterSaleStatus.RefundPending, repeated.AfterStatus);
                Assert.Empty(repeated.PickupTasks);
            }

            AfterSaleDto completedRefund;
            using (var complete = await adminClient.PostAsync(
                       $"/api/after-sales/{refundDraft.Id}/complete",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
                completedRefund = await ReadApiDataAsync<AfterSaleDto>(complete);
                Assert.Equal(AfterSaleStatus.Completed, completedRefund.AfterStatus);
            }

            using (var reverseCompleted = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{refundDraft.Id}/reverse",
                       new AfterSaleActionDto { Remark = $"{refundRemark}-完成后反审" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reverseCompleted, ResponseCode.DatabaseError);
            }

            Assert.Equal(
                [
                    AfterSaleAuditAction.Submit,
                    AfterSaleAuditAction.Reject,
                    AfterSaleAuditAction.Resubmit,
                    AfterSaleAuditAction.Approve,
                    AfterSaleAuditAction.Reverse,
                    AfterSaleAuditAction.Approve
                ],
                completedRefund.AuditLogs.Select(log => log.Action).Take(6));

            AfterSaleDto returnDraft;
            using (var createReturn = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetail.Id,
                           NumericPrecision.RoundQuantity(1m),
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
                var pickup = Assert.Single(approvedReturn.PickupTasks);
                Assert.Equal(PickupTaskStatus.PendingAssign, pickup.PickupStatus);
                Assert.Equal(pickupAddress, pickup.PickupAddress);
                pickupTaskIds.Add(pickup.Id);
                registry.Register<PickupTask>(pickup.Id, nameof(PickupTask.PickupAddressSnapshot), pickupAddress);
            }

            using (var reverseReturn = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{returnDraft.Id}/reverse",
                       new AfterSaleActionDto { Remark = $"{returnRemark}-已有取货不可反审" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reverseReturn, ResponseCode.DatabaseError);
            }

            using (var approveReturnAgain = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{returnDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{returnRemark}-幂等审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveReturnAgain.StatusCode);
                var repeated = await ReadApiDataAsync<AfterSaleDto>(approveReturnAgain);
                Assert.Equal(AfterSaleStatus.ReturnPending, repeated.AfterStatus);
                Assert.Single(repeated.PickupTasks);
            }

            using (var completeReturnEarly = await adminClient.PostAsync(
                       $"/api/after-sales/{returnDraft.Id}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(completeReturnEarly, ResponseCode.DatabaseError);
            }

            AfterSaleDto replenishDraft;
            using (var createReplenish = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetail.Id,
                           NumericPrecision.RoundQuantity(1m),
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
                Assert.Equal(expectedOrderAmount, replenishDraft.SettlementPrice);
            }

            using (var submitReplenish = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{replenishDraft.Id}/submit",
                       new AfterSaleActionDto { Remark = $"{replenishRemark}-提交" }))
            {
                Assert.Equal(HttpStatusCode.OK, submitReplenish.StatusCode);
            }

            using (var approveReplenish = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{replenishDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{replenishRemark}-同意补货" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveReplenish.StatusCode);
                var approved = await ReadApiDataAsync<AfterSaleDto>(approveReplenish);
                Assert.Equal(AfterSaleStatus.ReturnPending, approved.AfterStatus);
                Assert.Empty(approved.PickupTasks);
            }

            AfterSaleDto exchangeDraft;
            using (var createExchange = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetail.Id,
                           NumericPrecision.RoundQuantity(1m),
                           AfterSaleType.ReturnAndRefund,
                           AfterSaleHandleType.Exchange,
                           source,
                           contactName,
                           contactPhone,
                           pickupAddress,
                           exchangeRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createExchange.StatusCode);
                exchangeDraft = await ReadApiDataAsync<AfterSaleDto>(createExchange);
                exchangeAfterSaleId = exchangeDraft.Id;
                registry.Register<AfterSale>(exchangeDraft.Id, nameof(AfterSale.Remark), exchangeRemark);
                Assert.Equal(expectedOrderAmount, exchangeDraft.SettlementPrice);
            }

            using (var submitExchange = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{exchangeDraft.Id}/submit",
                       new AfterSaleActionDto { Remark = $"{exchangeRemark}-提交" }))
            {
                Assert.Equal(HttpStatusCode.OK, submitExchange.StatusCode);
            }

            using (var approveExchange = await adminClient.PostAsJsonAsync(
                       $"/api/after-sales/{exchangeDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{exchangeRemark}-同意换货" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveExchange.StatusCode);
                var approved = await ReadApiDataAsync<AfterSaleDto>(approveExchange);
                Assert.Equal(AfterSaleStatus.ReturnPending, approved.AfterStatus);
                var pickup = Assert.Single(approved.PickupTasks);
                pickupTaskIds.Add(pickup.Id);
                registry.Register<PickupTask>(pickup.Id, nameof(PickupTask.PickupAddressSnapshot), pickupAddress);
            }

            // 已占用：仅退款 2 + 退货 1 + 补货 1 + 换货 1 = 5；剩余 5，申请 6 应拒绝
            using (var overflow = await adminClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetail.Id,
                           NumericPrecision.RoundQuantity(6m),
                           AfterSaleType.RefundOnly,
                           AfterSaleHandleType.GoodsDiscount,
                           source,
                           contactName,
                           contactPhone,
                           pickupAddress,
                           $"{batch.Id}-超额拒绝")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(overflow, ResponseCode.DatabaseError);
            }

            await using (var assertContext = fixture.CreateDbContext())
            {
                Assert.Equal(4, await assertContext.AfterSales.CountAsync(item =>
                    item.SaleOrderId == saleOrder.Id
                    && item.Remark != null
                    && item.Remark.StartsWith(batch.Id)));
                Assert.Equal(2, await assertContext.PickupTasks.CountAsync(item =>
                    pickupTaskIds.Contains(item.Id)));
                Assert.Equal(
                    AfterSaleStatus.Completed,
                    (await assertContext.AfterSales.AsNoTracking().SingleAsync(item => item.Id == refundAfterSaleId)).AfterStatus);
                Assert.Equal(
                    AfterSaleStatus.ReturnPending,
                    (await assertContext.AfterSales.AsNoTracking().SingleAsync(item => item.Id == returnAfterSaleId)).AfterStatus);
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
                Assert.DoesNotContain(afterSalesCreatePermission, info.Buttons);
                Assert.DoesNotContain(afterSalesAuditPermission, info.Buttons);
            }

            using (var allowedList = await limitedClient.GetAsync(
                       $"/api/after-sales?current=1&size=20&keyword={Uri.EscapeDataString(completedRefund.AfterSaleNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedList.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<AfterSaleListItemDto>>(allowedList);
                Assert.Contains(page.Records!, item => item.Id == completedRefund.Id);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/after-sales",
                       BuildCreateAfterSalePayload(
                           saleOrder.Id,
                           managedCustomerId,
                           saleOrderDetail.Id,
                           NumericPrecision.RoundQuantity(1m),
                           AfterSaleType.RefundOnly,
                           AfterSaleHandleType.GoodsDiscount,
                           source,
                           contactName,
                           contactPhone,
                           pickupAddress,
                           $"{batch.Id}-越权创建")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedApprove = await limitedClient.PostAsJsonAsync(
                       $"/api/after-sales/{returnDraft.Id}/approve",
                       new AfterSaleActionDto { Remark = $"{returnRemark}-越权审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedApprove, ResponseCode.Forbidden);
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
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualAfterSaleIds = new List<Guid>();
                if (refundAfterSaleId.HasValue)
                    residualAfterSaleIds.Add(refundAfterSaleId.Value);
                if (returnAfterSaleId.HasValue)
                    residualAfterSaleIds.Add(returnAfterSaleId.Value);
                if (replenishAfterSaleId.HasValue)
                    residualAfterSaleIds.Add(replenishAfterSaleId.Value);
                if (exchangeAfterSaleId.HasValue)
                    residualAfterSaleIds.Add(exchangeAfterSaleId.Value);

                var residualPickupTasks = await cleanupContext.PickupTasks
                    .Where(item => pickupTaskIds.Contains(item.Id)
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
                if (pendingOrderId.HasValue)
                    residualSaleOrderIds.Add(pendingOrderId.Value);
                var residualSaleOrders = await cleanupContext.SaleOrders
                    .Where(item => residualSaleOrderIds.Contains(item.Id)
                                   || item.InnerRemark == saleOrderInnerRemark
                                   || item.InnerRemark == pendingOrderInnerRemark
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
            Assert.False(await residualContext.PickupTasks.AnyAsync(item =>
                item.PickupAddressSnapshot != null && item.PickupAddressSnapshot.StartsWith(batch.Id)));
            Assert.False(await residualContext.AfterSales.AnyAsync(item =>
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
            Assert.True(await residualContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
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
                    remark = "T9其他入库明细"
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
