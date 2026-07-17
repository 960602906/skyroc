using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Orders;
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
///     T5 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证销售订单审核并发、状态幂等与事务回滚副作用边界。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class SaleOrderConcurrencyIdempotencyRollbackPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     并发通过/驳回仅允许一胜一负且轨迹一致；重复动作幂等拒绝；非法流转与失败更新不写入部分状态；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task SaleOrder_ConcurrencyIdempotencyAndRollback_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var adminUserId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var concurrentInnerRemark = $"{batch.Id}C";
        var doubleApproveInnerRemark = $"{batch.Id}D";
        var rollbackInnerRemark = $"{batch.Id}R";
        var failedUpdateInnerRemark = $"{batch.Id}U";
        var password = "SkyRocOrderConc!2026";
        var userAgent = $"SkyRoc-T5-OrderConc/{batch.Id}";
        var createName = "T5-SaleOrderConc";

        var createQuantity = NumericPrecision.RoundQuantity(2m);
        var createFixedPrice = NumericPrecision.RoundMoney(15.5m);
        var updateQuantity = NumericPrecision.RoundQuantity(4m);
        var updateFixedPrice = NumericPrecision.RoundMoney(9.25m);

        Guid adminRoleId;
        Guid managedCustomerId;
        Guid managedGoodsId;
        Guid managedGoodsUnitId;
        Guid managedWareId;
        decimal managedUnitConversion;
        decimal expectedCreateTotalPrice;
        decimal expectedUpdateTotalPrice;

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
            managedUnitConversion = goodsUnit.ConversionRate;
            Assert.True(managedUnitConversion > 0);

            var ware = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == wareCode);
            Assert.NotNull(ware);
            managedWareId = ware.Id;

            var expectedCreateBaseQuantity = NumericPrecision.RoundQuantity(createQuantity * managedUnitConversion);
            expectedCreateTotalPrice = NumericPrecision.RoundMoney(
                expectedCreateBaseQuantity / managedUnitConversion * createFixedPrice);
            var expectedUpdateBaseQuantity = NumericPrecision.RoundQuantity(updateQuantity * managedUnitConversion);
            expectedUpdateTotalPrice = NumericPrecision.RoundMoney(
                expectedUpdateBaseQuantity / managedUnitConversion * updateFixedPrice);

            await seedContext.Users.AddAsync(new User
            {
                Id = adminUserId,
                Username = adminUsername,
                NickName = "T5订单并发操作员",
                Gender = GenderType.Male,
                Phone = "13900009601",
                Email = $"{batch.Id}-a@skyroc-autotest.example",
                PasswordHash = PasswordHasher.Hash(password),
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.UserRoles.AddAsync(new UserRole
            {
                UserId = adminUserId,
                RoleId = adminRoleId
            });

            await seedContext.SaveChangesAsync();
        }

        registry.Register<User>(adminUserId, nameof(User.Username), adminUsername);

        Guid? concurrentOrderId = null;
        Guid? doubleApproveOrderId = null;
        Guid? rollbackOrderId = null;
        Guid? failedUpdateOrderId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

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

            // 并发目标：待审核订单
            var concurrentOrder = await CreatePendingOrderAsync(
                adminClient,
                managedCustomerId,
                managedWareId,
                managedGoodsId,
                managedGoodsUnitId,
                createQuantity,
                createFixedPrice,
                concurrentInnerRemark,
                "T5并发审核目标订单",
                "2026-07-17T08:10:00Z");
            concurrentOrderId = concurrentOrder.Id;
            registry.Register<SaleOrder>(concurrentOrder.Id, nameof(SaleOrder.InnerRemark), concurrentInnerRemark);
            Assert.Equal(SaleOrderStatus.PendingAudit, concurrentOrder.OrderStatus);
            Assert.Equal(expectedCreateTotalPrice, concurrentOrder.OrderPrice);

            // 并发通过 vs 驳回：恰好一胜一负，最终状态与审核轨迹一致
            using var approveClient = factory.CreateClient();
            approveClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            approveClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using var rejectClient = factory.CreateClient();
            rejectClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            rejectClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var approveTask = approveClient.PostAsJsonAsync(
                $"/api/orders/{concurrentOrder.Id}/approve",
                new SaleOrderAuditDto { Remark = $"{batch.Id}-并发通过" });
            var rejectTask = rejectClient.PostAsJsonAsync(
                $"/api/orders/{concurrentOrder.Id}/reject",
                new SaleOrderAuditDto { Remark = $"{batch.Id}-并发驳回" });
            await Task.WhenAll(approveTask, rejectTask);

            using var approveResponse = await approveTask;
            using var rejectResponse = await rejectTask;
            var approveOk = approveResponse.StatusCode == HttpStatusCode.OK;
            var rejectOk = rejectResponse.StatusCode == HttpStatusCode.OK;
            var approveRejected = approveResponse.StatusCode == HttpStatusCode.BadGateway;
            var rejectRejected = rejectResponse.StatusCode == HttpStatusCode.BadGateway;

            Assert.True(approveOk ^ rejectOk, "并发通过与驳回必须恰好一个成功");
            Assert.True(
                (approveOk && rejectRejected) || (rejectOk && approveRejected),
                "失败方必须返回业务拒绝 502，不允许双方成功或双方失败");

            SaleOrderStatus expectedConcurrentStatus;
            OrderAuditAction expectedWinningAction;
            if (approveOk)
            {
                expectedConcurrentStatus = SaleOrderStatus.SortingPending;
                expectedWinningAction = OrderAuditAction.Approve;
                var approved = await ReadApiDataAsync<SaleOrderDto>(approveResponse);
                Assert.Equal(SaleOrderStatus.SortingPending, approved.OrderStatus);
            }
            else
            {
                expectedConcurrentStatus = SaleOrderStatus.Rejected;
                expectedWinningAction = OrderAuditAction.Reject;
                var rejected = await ReadApiDataAsync<SaleOrderDto>(rejectResponse);
                Assert.Equal(SaleOrderStatus.Rejected, rejected.OrderStatus);
            }

            await using (var afterConcurrent = fixture.CreateDbContext())
            {
                var orderEntity = await afterConcurrent.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == concurrentOrder.Id);
                Assert.Equal(expectedConcurrentStatus, orderEntity.OrderStatus);
                Assert.Equal(expectedCreateTotalPrice, orderEntity.OrderPrice);

                var auditLogs = await afterConcurrent.OrderAuditLogs.AsNoTracking()
                    .Where(log => log.SaleOrderId == concurrentOrder.Id)
                    .OrderBy(log => log.AuditTime)
                    .ThenBy(log => log.Id)
                    .ToListAsync();
                Assert.Equal(2, auditLogs.Count);
                Assert.Equal(OrderAuditAction.Submit, auditLogs[0].Action);
                Assert.Equal(expectedWinningAction, auditLogs[1].Action);
                Assert.Equal(SaleOrderStatus.PendingAudit, auditLogs[1].PreviousStatus);
                Assert.Equal(expectedConcurrentStatus, auditLogs[1].CurrentStatus);
                Assert.DoesNotContain(
                    auditLogs,
                    log => log.Action is OrderAuditAction.Approve or OrderAuditAction.Reject
                           && log.Action != expectedWinningAction);
            }

            // 幂等：重复执行胜方与败方动作均业务拒绝，状态与轨迹不变
            var postConcurrencyAuditCount = 2;
            using (var reApprove = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{concurrentOrder.Id}/approve",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-重复通过" }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, reApprove.StatusCode);
            }

            using (var reReject = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{concurrentOrder.Id}/reject",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-重复驳回" }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, reReject.StatusCode);
            }

            using (var illegalResubmit = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{concurrentOrder.Id}/resubmit",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-非法重提" }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, illegalResubmit.StatusCode);
            }

            await using (var afterIdempotent = fixture.CreateDbContext())
            {
                var orderEntity = await afterIdempotent.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == concurrentOrder.Id);
                Assert.Equal(expectedConcurrentStatus, orderEntity.OrderStatus);

                var auditCount = await afterIdempotent.OrderAuditLogs.AsNoTracking()
                    .CountAsync(log => log.SaleOrderId == concurrentOrder.Id);
                Assert.Equal(postConcurrencyAuditCount, auditCount);
            }

            // 并发双通过：恰好一胜一负，最终待分拣且仅一条通过轨迹
            var doubleApproveOrder = await CreatePendingOrderAsync(
                adminClient,
                managedCustomerId,
                managedWareId,
                managedGoodsId,
                managedGoodsUnitId,
                createQuantity,
                createFixedPrice,
                doubleApproveInnerRemark,
                "T5并发双通过目标订单",
                "2026-07-17T08:20:00Z");
            doubleApproveOrderId = doubleApproveOrder.Id;
            registry.Register<SaleOrder>(
                doubleApproveOrder.Id,
                nameof(SaleOrder.InnerRemark),
                doubleApproveInnerRemark);

            using var firstApproveClient = factory.CreateClient();
            firstApproveClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            firstApproveClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using var secondApproveClient = factory.CreateClient();
            secondApproveClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            secondApproveClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var firstApproveTask = firstApproveClient.PostAsJsonAsync(
                $"/api/orders/{doubleApproveOrder.Id}/approve",
                new SaleOrderAuditDto { Remark = $"{batch.Id}-双通过A" });
            var secondApproveTask = secondApproveClient.PostAsJsonAsync(
                $"/api/orders/{doubleApproveOrder.Id}/approve",
                new SaleOrderAuditDto { Remark = $"{batch.Id}-双通过B" });
            await Task.WhenAll(firstApproveTask, secondApproveTask);

            using var firstApproveResponse = await firstApproveTask;
            using var secondApproveResponse = await secondApproveTask;
            var successCount = new[] { firstApproveResponse.StatusCode, secondApproveResponse.StatusCode }
                .Count(code => code == HttpStatusCode.OK);
            var failureCount = new[] { firstApproveResponse.StatusCode, secondApproveResponse.StatusCode }
                .Count(code => code == HttpStatusCode.BadGateway);
            Assert.Equal(1, successCount);
            Assert.Equal(1, failureCount);

            await using (var afterDoubleApprove = fixture.CreateDbContext())
            {
                var orderEntity = await afterDoubleApprove.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == doubleApproveOrder.Id);
                Assert.Equal(SaleOrderStatus.SortingPending, orderEntity.OrderStatus);

                var auditLogs = await afterDoubleApprove.OrderAuditLogs.AsNoTracking()
                    .Where(log => log.SaleOrderId == doubleApproveOrder.Id)
                    .OrderBy(log => log.AuditTime)
                    .ThenBy(log => log.Id)
                    .ToListAsync();
                Assert.Equal(2, auditLogs.Count);
                Assert.Equal(OrderAuditAction.Submit, auditLogs[0].Action);
                Assert.Equal(OrderAuditAction.Approve, auditLogs[1].Action);
                Assert.Equal(
                    1,
                    auditLogs.Count(log => log.Action == OrderAuditAction.Approve));
            }

            // 事务/副作用回滚：非法重提不写入审核轨迹，状态保持待审核
            var rollbackOrder = await CreatePendingOrderAsync(
                adminClient,
                managedCustomerId,
                managedWareId,
                managedGoodsId,
                managedGoodsUnitId,
                createQuantity,
                createFixedPrice,
                rollbackInnerRemark,
                "T5回滚非法流转订单",
                "2026-07-17T08:30:00Z");
            rollbackOrderId = rollbackOrder.Id;
            registry.Register<SaleOrder>(rollbackOrder.Id, nameof(SaleOrder.InnerRemark), rollbackInnerRemark);

            using (var illegalResubmitPending = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{rollbackOrder.Id}/resubmit",
                       new SaleOrderAuditDto { Remark = $"{batch.Id}-待审核非法重提" }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, illegalResubmitPending.StatusCode);
            }

            await using (var afterIllegal = fixture.CreateDbContext())
            {
                var orderEntity = await afterIllegal.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == rollbackOrder.Id);
                Assert.Equal(SaleOrderStatus.PendingAudit, orderEntity.OrderStatus);
                Assert.Equal(expectedCreateTotalPrice, orderEntity.OrderPrice);

                var auditLogs = await afterIllegal.OrderAuditLogs.AsNoTracking()
                    .Where(log => log.SaleOrderId == rollbackOrder.Id)
                    .ToListAsync();
                Assert.Single(auditLogs);
                Assert.Equal(OrderAuditAction.Submit, auditLogs[0].Action);
            }

            // 失败更新不改写主单金额与明细：不存在的商品引用被拒绝
            var failedUpdateOrder = await CreatePendingOrderAsync(
                adminClient,
                managedCustomerId,
                managedWareId,
                managedGoodsId,
                managedGoodsUnitId,
                createQuantity,
                createFixedPrice,
                failedUpdateInnerRemark,
                "T5失败更新回滚订单",
                "2026-07-17T08:40:00Z");
            failedUpdateOrderId = failedUpdateOrder.Id;
            registry.Register<SaleOrder>(
                failedUpdateOrder.Id,
                nameof(SaleOrder.InnerRemark),
                failedUpdateInnerRemark);
            var originalDetailId = Assert.Single(failedUpdateOrder.Details).Id;

            using (var failedUpdate = await adminClient.PutAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           id = failedUpdateOrder.Id,
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-17T08:40:00Z",
                           contactName = "不应落库联系人",
                           contactPhone = "13800139999",
                           deliveryAddress = "不应落库地址",
                           remark = "T5失败更新不应落库",
                           innerRemark = failedUpdateInnerRemark,
                           details = new[]
                           {
                               new
                               {
                                   id = originalDetailId,
                                   goodsId = Guid.NewGuid(),
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = updateQuantity,
                                   fixedPrice = updateFixedPrice,
                                   fixedGoodsUnitId = managedGoodsUnitId,
                                   remark = "失败明细",
                                   innerRemark = $"{batch.Id}UF"
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.BadGateway, failedUpdate.StatusCode);
            }

            await using (var afterFailedUpdate = fixture.CreateDbContext())
            {
                var orderEntity = await afterFailedUpdate.SaleOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == failedUpdateOrder.Id);
                Assert.Equal(SaleOrderStatus.PendingAudit, orderEntity.OrderStatus);
                Assert.Equal(expectedCreateTotalPrice, orderEntity.OrderPrice);
                Assert.NotEqual("不应落库联系人", orderEntity.ContactNameSnapshot);
                Assert.Equal("T5失败更新回滚订单", orderEntity.Remark);
                Assert.Equal(failedUpdateInnerRemark, orderEntity.InnerRemark);

                var detailEntity = await afterFailedUpdate.SaleOrderDetails.AsNoTracking()
                    .SingleAsync(item => item.SaleOrderId == failedUpdateOrder.Id);
                Assert.Equal(originalDetailId, detailEntity.Id);
                Assert.Equal(managedGoodsId, detailEntity.GoodsId);
                Assert.Equal(createQuantity, detailEntity.Quantity);
                Assert.Equal(createFixedPrice, detailEntity.FixedPrice);
                Assert.Equal(expectedCreateTotalPrice, detailEntity.TotalPrice);
            }

            // 合法更新仍可成功，确认服务在失败路径后仍可用
            using (var successUpdate = await adminClient.PutAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           id = failedUpdateOrder.Id,
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-17T08:40:00Z",
                           contactName = "合法更新联系人",
                           contactPhone = "13800139602",
                           deliveryAddress = "上海市浦东新区回滚路 2 号",
                           remark = "T5失败更新后合法更新",
                           innerRemark = failedUpdateInnerRemark,
                           details = new[]
                           {
                               new
                               {
                                   id = originalDetailId,
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = updateQuantity,
                                   fixedPrice = updateFixedPrice,
                                   fixedGoodsUnitId = managedGoodsUnitId,
                                   remark = "合法明细",
                                   innerRemark = $"{batch.Id}US"
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, successUpdate.StatusCode);
                var updated = await ReadApiDataAsync<SaleOrderDto>(successUpdate);
                Assert.Equal("合法更新联系人", updated.ContactName);
                Assert.Equal(expectedUpdateTotalPrice, updated.OrderPrice);
                Assert.Equal(expectedUpdateTotalPrice, Assert.Single(updated.Details).TotalPrice);
            }

            // 清理本轮订单
            foreach (var orderId in new[]
                     {
                         concurrentOrderId,
                         doubleApproveOrderId,
                         rollbackOrderId,
                         failedUpdateOrderId
                     })
            {
                if (!orderId.HasValue)
                    continue;

                using var deleteResponse = await adminClient.DeleteAsync($"/api/orders/{orderId.Value}");
                Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
            }

            concurrentOrderId = null;
            doubleApproveOrderId = null;
            rollbackOrderId = null;
            failedUpdateOrderId = null;

            await using (var afterDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterDelete.SaleOrders.AnyAsync(item =>
                    item.InnerRemark == concurrentInnerRemark
                    || item.InnerRemark == doubleApproveInnerRemark
                    || item.InnerRemark == rollbackInnerRemark
                    || item.InnerRemark == failedUpdateInnerRemark));
            }

            await using var auditContext = fixture.CreateDbContext();
            var loginLogs = await auditContext.LoginLogs.AsNoTracking()
                .Where(log => log.Username == adminUsername)
                .ToListAsync();
            Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
            Assert.All(loginLogs, log =>
            {
                Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
            });
            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername);

            // 受管主数据与 Admin 角色保持不变
            Assert.True(await auditContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await auditContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
            Assert.True(await auditContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await auditContext.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));
            Assert.True(await auditContext.Wares.AnyAsync(item => item.Id == managedWareId));
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername);
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualInnerRemarks = new[]
                {
                    concurrentInnerRemark,
                    doubleApproveInnerRemark,
                    rollbackInnerRemark,
                    failedUpdateInnerRemark
                };
                var residualOrderIds = new List<Guid>();
                if (concurrentOrderId.HasValue)
                    residualOrderIds.Add(concurrentOrderId.Value);
                if (doubleApproveOrderId.HasValue)
                    residualOrderIds.Add(doubleApproveOrderId.Value);
                if (rollbackOrderId.HasValue)
                    residualOrderIds.Add(rollbackOrderId.Value);
                if (failedUpdateOrderId.HasValue)
                    residualOrderIds.Add(failedUpdateOrderId.Value);

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
                    .Where(relation => relation.UserId == adminUserId)
                    .ToListAsync();
                if (residualUserRoles.Count > 0)
                {
                    cleanupContext.UserRoles.RemoveRange(residualUserRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualUsers = await cleanupContext.Users
                    .Where(user => user.Id == adminUserId
                                   || user.Username == adminUsername
                                   || (user.Username != null && user.Username.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualUsers.Count > 0)
                {
                    cleanupContext.Users.RemoveRange(residualUsers);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.SaleOrders.AnyAsync(item =>
                item.InnerRemark == concurrentInnerRemark
                || item.InnerRemark == doubleApproveInnerRemark
                || item.InnerRemark == rollbackInnerRemark
                || item.InnerRemark == failedUpdateInnerRemark
                || (item.InnerRemark != null && item.InnerRemark.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
            Assert.True(await residualContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
        }
    }

    private static async Task<SaleOrderDto> CreatePendingOrderAsync(
        HttpClient client,
        Guid customerId,
        Guid wareId,
        Guid goodsId,
        Guid goodsUnitId,
        decimal quantity,
        decimal fixedPrice,
        string innerRemark,
        string remark,
        string orderDateUtc)
    {
        using var createResponse = await client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                customerId,
                wareId,
                orderDate = orderDateUtc,
                receiveDate = "2026-07-18T06:00:00Z",
                contactName = "并发食堂",
                contactPhone = "13800139601",
                deliveryAddress = "上海市浦东新区并发路 1 号",
                remark,
                innerRemark,
                details = new[]
                {
                    new
                    {
                        goodsId,
                        goodsUnitId,
                        quantity,
                        fixedPrice,
                        fixedGoodsUnitId = goodsUnitId,
                        remark = "并发明细",
                        innerRemark = $"{innerRemark}L"
                    }
                }
            });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        return await ReadApiDataAsync<SaleOrderDto>(createResponse);
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
