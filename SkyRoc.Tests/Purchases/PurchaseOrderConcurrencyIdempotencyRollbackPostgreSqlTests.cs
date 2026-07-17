using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Orders;
using Application.DTOs.Purchases;
using Domain.Entities;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Common;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Purchases;

/// <summary>
///     T6 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证采购单完成/取消并发、状态幂等与事务回滚副作用边界。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class PurchaseOrderConcurrencyIdempotencyRollbackPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     并发完成/取消与双完成仅允许一胜一负且计划占用一致；重复动作幂等拒绝；失败更新与失败生成不写入部分状态；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task PurchaseOrder_ConcurrencyIdempotencyAndRollback_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var adminUserId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var concurrentSaleRemark = $"{batch.Id}C";
        var concurrentPlanRemark = $"{batch.Id}CP";
        var concurrentPoRemark = $"{batch.Id}CO";
        var doubleSaleRemark = $"{batch.Id}D";
        var doublePlanRemark = $"{batch.Id}DP";
        var doublePoRemark = $"{batch.Id}DO";
        var updateSaleRemark = $"{batch.Id}U";
        var updatePlanRemark = $"{batch.Id}UP";
        var updatePoRemark = $"{batch.Id}UO";
        var manualPoRemark = $"{batch.Id}M";
        var password = "SkyRocPurchaseConc!2026";
        var userAgent = $"SkyRoc-T6-PurchaseConc/{batch.Id}";
        var createName = "T6-PurchaseOrderConc";

        var orderQuantity = NumericPrecision.RoundQuantity(2m);
        var orderFixedPrice = NumericPrecision.RoundMoney(12.5m);
        var createPurchasePrice = NumericPrecision.RoundMoney(8.5m);
        var updatePurchaseQuantity = NumericPrecision.RoundQuantity(3m);
        var updatePurchasePrice = NumericPrecision.RoundMoney(9.25m);

        Guid adminRoleId;
        Guid managedCustomerId;
        Guid managedGoodsId;
        Guid managedGoodsUnitId;
        decimal managedUnitConversion;
        Guid managedWareId;
        Guid managedSupplierId;
        Guid managedPurchaserId;
        decimal expectedBaseQuantity;
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
            var supplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);
            var purchaserCode = DemoDataStableKeyCatalog.Create("PURCHASER", 1);

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
            expectedBaseQuantity = NumericPrecision.RoundQuantity(orderQuantity * managedUnitConversion);
            expectedUpdateTotalPrice = NumericPrecision.RoundMoney(updatePurchaseQuantity * updatePurchasePrice);

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

            await seedContext.Users.AddAsync(new User
            {
                Id = adminUserId,
                Username = adminUsername,
                NickName = "T6采购并发操作员",
                Gender = GenderType.Male,
                Phone = "13900009701",
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

        Guid? concurrentSaleOrderId = null;
        Guid? concurrentPlanId = null;
        Guid? concurrentPoId = null;
        Guid? doubleSaleOrderId = null;
        Guid? doublePlanId = null;
        Guid? doublePoId = null;
        Guid? updateSaleOrderId = null;
        Guid? updatePlanId = null;
        Guid? updatePoId = null;
        Guid? manualPoId = null;

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

            // 准备三张来源计划采购单草稿 + 一张手工草稿
            var concurrentDraft = await CreatePlanSourcedDraftAsync(
                adminClient,
                managedCustomerId,
                managedWareId,
                managedGoodsId,
                managedGoodsUnitId,
                managedSupplierId,
                managedPurchaserId,
                orderQuantity,
                orderFixedPrice,
                expectedBaseQuantity,
                concurrentSaleRemark,
                concurrentPlanRemark,
                concurrentPoRemark,
                "2026-07-17T09:10:00Z",
                "T6并发完成取消目标");
            concurrentSaleOrderId = concurrentDraft.SaleOrderId;
            concurrentPlanId = concurrentDraft.PlanId;
            concurrentPoId = concurrentDraft.PurchaseOrder.Id;
            registry.Register<SaleOrder>(
                concurrentDraft.SaleOrderId,
                nameof(SaleOrder.InnerRemark),
                concurrentSaleRemark);
            registry.Register<PurchasePlan>(
                concurrentDraft.PlanId,
                nameof(PurchasePlan.Remark),
                concurrentPlanRemark);
            registry.Register<PurchaseOrder>(
                concurrentDraft.PurchaseOrder.Id,
                nameof(PurchaseOrder.Remark),
                concurrentPoRemark);

            var doubleDraft = await CreatePlanSourcedDraftAsync(
                adminClient,
                managedCustomerId,
                managedWareId,
                managedGoodsId,
                managedGoodsUnitId,
                managedSupplierId,
                managedPurchaserId,
                orderQuantity,
                orderFixedPrice,
                expectedBaseQuantity,
                doubleSaleRemark,
                doublePlanRemark,
                doublePoRemark,
                "2026-07-17T09:20:00Z",
                "T6并发双完成目标");
            doubleSaleOrderId = doubleDraft.SaleOrderId;
            doublePlanId = doubleDraft.PlanId;
            doublePoId = doubleDraft.PurchaseOrder.Id;
            registry.Register<SaleOrder>(doubleDraft.SaleOrderId, nameof(SaleOrder.InnerRemark), doubleSaleRemark);
            registry.Register<PurchasePlan>(doubleDraft.PlanId, nameof(PurchasePlan.Remark), doublePlanRemark);
            registry.Register<PurchaseOrder>(
                doubleDraft.PurchaseOrder.Id,
                nameof(PurchaseOrder.Remark),
                doublePoRemark);

            var updateDraft = await CreatePlanSourcedDraftAsync(
                adminClient,
                managedCustomerId,
                managedWareId,
                managedGoodsId,
                managedGoodsUnitId,
                managedSupplierId,
                managedPurchaserId,
                orderQuantity,
                orderFixedPrice,
                expectedBaseQuantity,
                updateSaleRemark,
                updatePlanRemark,
                updatePoRemark,
                "2026-07-17T09:30:00Z",
                "T6失败生成回滚目标");
            updateSaleOrderId = updateDraft.SaleOrderId;
            updatePlanId = updateDraft.PlanId;
            updatePoId = updateDraft.PurchaseOrder.Id;
            registry.Register<SaleOrder>(updateDraft.SaleOrderId, nameof(SaleOrder.InnerRemark), updateSaleRemark);
            registry.Register<PurchasePlan>(updateDraft.PlanId, nameof(PurchasePlan.Remark), updatePlanRemark);
            registry.Register<PurchaseOrder>(
                updateDraft.PurchaseOrder.Id,
                nameof(PurchaseOrder.Remark),
                updatePoRemark);

            PurchaseOrderDto manualDraft;
            using (var createManual = await adminClient.PostAsJsonAsync(
                       "/api/purchase-orders",
                       new
                       {
                           supplierId = managedSupplierId,
                           purchaserId = managedPurchaserId,
                           purchasePattern = PurchasePattern.SupplierDirect,
                           receiveTime = "2026-07-18T08:00:00Z",
                           remark = manualPoRemark,
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   purchaseUnitId = managedGoodsUnitId,
                                   purchaseQuantity = orderQuantity,
                                   purchasePrice = createPurchasePrice,
                                   remark = "T6手工草稿明细"
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createManual.StatusCode);
                manualDraft = await ReadApiDataAsync<PurchaseOrderDto>(createManual);
                Assert.Equal(PurchaseOrderStatus.Draft, manualDraft.BusinessStatus);
                manualPoId = manualDraft.Id;
                registry.Register<PurchaseOrder>(manualDraft.Id, nameof(PurchaseOrder.Remark), manualPoRemark);
            }

            // 并发完成 vs 取消：恰好一胜一负，计划占用与终态一致
            using var completeClient = factory.CreateClient();
            completeClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            completeClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using var cancelClient = factory.CreateClient();
            cancelClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            cancelClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var completeTask = completeClient.PostAsync(
                $"/api/purchase-orders/{concurrentDraft.PurchaseOrder.Id}/complete",
                null);
            var cancelTask = cancelClient.PostAsync(
                $"/api/purchase-orders/{concurrentDraft.PurchaseOrder.Id}/cancel",
                null);
            await Task.WhenAll(completeTask, cancelTask);

            using var completeResponse = await completeTask;
            using var cancelResponse = await cancelTask;
            var completeCode = await ApiHttpAssert.ReadBusinessCodeAsync(completeResponse);
            var cancelCode = await ApiHttpAssert.ReadBusinessCodeAsync(cancelResponse);
            var completeOk = completeCode == ResponseCode.Success;
            var cancelOk = cancelCode == ResponseCode.Success;
            var completeRejected = completeCode == ResponseCode.DatabaseError;
            var cancelRejected = cancelCode == ResponseCode.DatabaseError;

            Assert.True(completeOk ^ cancelOk, "并发完成与取消必须恰好一个成功");
            Assert.True(
                (completeOk && cancelRejected) || (cancelOk && completeRejected),
                "失败方必须返回业务拒绝 code=502，不允许双方成功或双方失败");

            await using (var afterConcurrent = fixture.CreateDbContext())
            {
                var orderEntity = await afterConcurrent.PurchaseOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == concurrentDraft.PurchaseOrder.Id);
                var planEntity = await afterConcurrent.PurchasePlans.AsNoTracking()
                    .Include(item => item.Details)
                    .SingleAsync(item => item.Id == concurrentDraft.PlanId);
                var planDetail = Assert.Single(planEntity.Details);

                if (completeOk)
                {
                    Assert.Equal(PurchaseOrderStatus.Completed, orderEntity.BusinessStatus);
                    Assert.Equal(PurchasePlanStatus.Generated, planEntity.PurchaseStatus);
                    Assert.Equal(expectedBaseQuantity, planDetail.PurchasedQuantity);
                }
                else
                {
                    Assert.Equal(PurchaseOrderStatus.Cancelled, orderEntity.BusinessStatus);
                    Assert.Equal(PurchasePlanStatus.Unpublished, planEntity.PurchaseStatus);
                    Assert.Equal(0m, planDetail.PurchasedQuantity);
                }
            }

            // 幂等：重复完成/取消/删除均业务拒绝，终态不变
            var expectedConcurrentStatus = completeOk
                ? PurchaseOrderStatus.Completed
                : PurchaseOrderStatus.Cancelled;

            using (var reComplete = await adminClient.PostAsync(
                       $"/api/purchase-orders/{concurrentDraft.PurchaseOrder.Id}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reComplete, ResponseCode.DatabaseError);
            }

            using (var reCancel = await adminClient.PostAsync(
                       $"/api/purchase-orders/{concurrentDraft.PurchaseOrder.Id}/cancel",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reCancel, ResponseCode.DatabaseError);
            }

            using (var reDelete = await adminClient.DeleteAsync(
                       $"/api/purchase-orders/{concurrentDraft.PurchaseOrder.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reDelete, ResponseCode.DatabaseError);
            }

            await using (var afterIdempotent = fixture.CreateDbContext())
            {
                var orderEntity = await afterIdempotent.PurchaseOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == concurrentDraft.PurchaseOrder.Id);
                Assert.Equal(expectedConcurrentStatus, orderEntity.BusinessStatus);
            }

            // 并发双完成：恰好一胜一负，最终已完成
            using var firstCompleteClient = factory.CreateClient();
            firstCompleteClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            firstCompleteClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using var secondCompleteClient = factory.CreateClient();
            secondCompleteClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            secondCompleteClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var firstCompleteTask = firstCompleteClient.PostAsync(
                $"/api/purchase-orders/{doubleDraft.PurchaseOrder.Id}/complete",
                null);
            var secondCompleteTask = secondCompleteClient.PostAsync(
                $"/api/purchase-orders/{doubleDraft.PurchaseOrder.Id}/complete",
                null);
            await Task.WhenAll(firstCompleteTask, secondCompleteTask);

            using var firstCompleteResponse = await firstCompleteTask;
            using var secondCompleteResponse = await secondCompleteTask;
            var firstCode = await ApiHttpAssert.ReadBusinessCodeAsync(firstCompleteResponse);
            var secondCode = await ApiHttpAssert.ReadBusinessCodeAsync(secondCompleteResponse);
            var successCount = new[] { firstCode, secondCode }.Count(code => code == ResponseCode.Success);
            var failureCount = new[] { firstCode, secondCode }.Count(code => code == ResponseCode.DatabaseError);
            Assert.Equal(1, successCount);
            Assert.Equal(1, failureCount);

            await using (var afterDouble = fixture.CreateDbContext())
            {
                var orderEntity = await afterDouble.PurchaseOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == doubleDraft.PurchaseOrder.Id);
                Assert.Equal(PurchaseOrderStatus.Completed, orderEntity.BusinessStatus);

                var planEntity = await afterDouble.PurchasePlans.AsNoTracking()
                    .Include(item => item.Details)
                    .SingleAsync(item => item.Id == doubleDraft.PlanId);
                Assert.Equal(PurchasePlanStatus.Generated, planEntity.PurchaseStatus);
                Assert.Equal(expectedBaseQuantity, Assert.Single(planEntity.Details).PurchasedQuantity);
            }

            // 失败生成：无剩余数量时拒绝，不新增采购单，计划占用不变
            var planPoCountBefore = 0;
            await using (var beforeRegen = fixture.CreateDbContext())
            {
                planPoCountBefore = await beforeRegen.PurchaseOrders.CountAsync(item =>
                    item.Remark != null && item.Remark.StartsWith(batch.Id));
                var planEntity = await beforeRegen.PurchasePlans.AsNoTracking()
                    .Include(item => item.Details)
                    .SingleAsync(item => item.Id == updateDraft.PlanId);
                Assert.Equal(PurchasePlanStatus.Generated, planEntity.PurchaseStatus);
                Assert.Equal(expectedBaseQuantity, Assert.Single(planEntity.Details).PurchasedQuantity);
            }

            using (var illegalRegen = await adminClient.PostAsJsonAsync(
                       "/api/purchase-orders/generate-from-plans",
                       new
                       {
                           planIds = new[] { updateDraft.PlanId },
                           receiveTime = "2026-07-18T10:00:00Z",
                           remark = $"{batch.Id}RG"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(illegalRegen, ResponseCode.DatabaseError);
            }

            await using (var afterRegen = fixture.CreateDbContext())
            {
                var planPoCountAfter = await afterRegen.PurchaseOrders.CountAsync(item =>
                    item.Remark != null && item.Remark.StartsWith(batch.Id));
                Assert.Equal(planPoCountBefore, planPoCountAfter);
                Assert.False(await afterRegen.PurchaseOrders.AnyAsync(item => item.Remark == $"{batch.Id}RG"));

                var planEntity = await afterRegen.PurchasePlans.AsNoTracking()
                    .Include(item => item.Details)
                    .SingleAsync(item => item.Id == updateDraft.PlanId);
                Assert.Equal(PurchasePlanStatus.Generated, planEntity.PurchaseStatus);
                Assert.Equal(expectedBaseQuantity, Assert.Single(planEntity.Details).PurchasedQuantity);
                Assert.True(await afterRegen.PurchaseOrders.AnyAsync(item =>
                    item.Id == updateDraft.PurchaseOrder.Id
                    && item.BusinessStatus == PurchaseOrderStatus.Draft));
            }

            // 失败更新：不存在商品引用被拒绝，草稿金额/明细保持原值
            var originalDetail = Assert.Single(manualDraft.Details);
            var originalTotal = NumericPrecision.RoundMoney(orderQuantity * createPurchasePrice);

            using (var failedUpdate = await adminClient.PutAsJsonAsync(
                       "/api/purchase-orders",
                       new
                       {
                           id = manualDraft.Id,
                           supplierId = managedSupplierId,
                           purchaserId = managedPurchaserId,
                           purchasePattern = PurchasePattern.SupplierDirect,
                           receiveTime = "2026-07-18T08:00:00Z",
                           remark = "不应落库备注",
                           details = new[]
                           {
                               new
                               {
                                   id = originalDetail.Id,
                                   goodsId = Guid.NewGuid(),
                                   purchaseUnitId = managedGoodsUnitId,
                                   purchaseQuantity = updatePurchaseQuantity,
                                   purchasePrice = updatePurchasePrice,
                                   remark = "失败明细"
                               }
                           }
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(failedUpdate, ResponseCode.DatabaseError);
            }

            await using (var afterFailedUpdate = fixture.CreateDbContext())
            {
                var orderEntity = await afterFailedUpdate.PurchaseOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == manualDraft.Id);
                Assert.Equal(PurchaseOrderStatus.Draft, orderEntity.BusinessStatus);
                Assert.Equal(manualPoRemark, orderEntity.Remark);
                Assert.NotEqual("不应落库备注", orderEntity.Remark);

                var detailEntity = await afterFailedUpdate.PurchaseOrderDetails.AsNoTracking()
                    .SingleAsync(item => item.PurchaseOrderId == manualDraft.Id);
                Assert.Equal(originalDetail.Id, detailEntity.Id);
                Assert.Equal(managedGoodsId, detailEntity.GoodsId);
                Assert.Equal(orderQuantity, detailEntity.PurchaseQuantity);
                Assert.Equal(createPurchasePrice, detailEntity.PurchasePrice);
                Assert.Equal(originalTotal, detailEntity.PurchaseTotalPrice);
            }

            // 合法更新仍可成功
            using (var successUpdate = await adminClient.PutAsJsonAsync(
                       "/api/purchase-orders",
                       new
                       {
                           id = manualDraft.Id,
                           supplierId = managedSupplierId,
                           purchaserId = managedPurchaserId,
                           purchasePattern = PurchasePattern.SupplierDirect,
                           receiveTime = "2026-07-18T08:30:00Z",
                           remark = manualPoRemark,
                           details = new[]
                           {
                               new
                               {
                                   id = originalDetail.Id,
                                   goodsId = managedGoodsId,
                                   purchaseUnitId = managedGoodsUnitId,
                                   purchaseQuantity = updatePurchaseQuantity,
                                   purchasePrice = updatePurchasePrice,
                                   remark = "合法明细"
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, successUpdate.StatusCode);
                var updated = await ReadApiDataAsync<PurchaseOrderDto>(successUpdate);
                Assert.Equal(PurchaseOrderStatus.Draft, updated.BusinessStatus);
                var detail = Assert.Single(updated.Details);
                Assert.Equal(updatePurchaseQuantity, detail.PurchaseQuantity);
                Assert.Equal(updatePurchasePrice, detail.PurchasePrice);
                Assert.Equal(expectedUpdateTotalPrice, detail.PurchaseTotalPrice);
            }

            // 草稿可删除；已终结单据保留至 finally 精确清理
            using (var deleteManual = await adminClient.DeleteAsync($"/api/purchase-orders/{manualDraft.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteManual.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteManual));
            }

            manualPoId = null;

            await using (var afterDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterDelete.PurchaseOrders.AnyAsync(item => item.Remark == manualPoRemark));
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

            Assert.True(await auditContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await auditContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
            Assert.True(await auditContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await auditContext.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));
            Assert.True(await auditContext.Wares.AnyAsync(item => item.Id == managedWareId));
            Assert.True(await auditContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await auditContext.Purchasers.AnyAsync(item => item.Id == managedPurchaserId));
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername);
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                // 兜底：采购单 → 计划 → 销售订单（外键逆序）
                var residualPoRemarks = new[]
                {
                    concurrentPoRemark,
                    doublePoRemark,
                    updatePoRemark,
                    manualPoRemark,
                    $"{batch.Id}RG"
                };
                var residualPoIds = new List<Guid>();
                if (concurrentPoId.HasValue)
                    residualPoIds.Add(concurrentPoId.Value);
                if (doublePoId.HasValue)
                    residualPoIds.Add(doublePoId.Value);
                if (updatePoId.HasValue)
                    residualPoIds.Add(updatePoId.Value);
                if (manualPoId.HasValue)
                    residualPoIds.Add(manualPoId.Value);

                var residualPurchaseOrders = await cleanupContext.PurchaseOrders
                    .Where(item => residualPoIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (residualPoRemarks.Contains(item.Remark)
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualPurchaseOrders.Count > 0)
                {
                    cleanupContext.PurchaseOrders.RemoveRange(residualPurchaseOrders);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualPlanRemarks = new[]
                {
                    concurrentPlanRemark,
                    doublePlanRemark,
                    updatePlanRemark
                };
                var residualPlanIds = new List<Guid>();
                if (concurrentPlanId.HasValue)
                    residualPlanIds.Add(concurrentPlanId.Value);
                if (doublePlanId.HasValue)
                    residualPlanIds.Add(doublePlanId.Value);
                if (updatePlanId.HasValue)
                    residualPlanIds.Add(updatePlanId.Value);

                var residualPlans = await cleanupContext.PurchasePlans
                    .Where(item => residualPlanIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (residualPlanRemarks.Contains(item.Remark)
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualPlans.Count > 0)
                {
                    cleanupContext.PurchasePlans.RemoveRange(residualPlans);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualInnerRemarks = new[]
                {
                    concurrentSaleRemark,
                    doubleSaleRemark,
                    updateSaleRemark
                };
                var residualOrderIds = new List<Guid>();
                if (concurrentSaleOrderId.HasValue)
                    residualOrderIds.Add(concurrentSaleOrderId.Value);
                if (doubleSaleOrderId.HasValue)
                    residualOrderIds.Add(doubleSaleOrderId.Value);
                if (updateSaleOrderId.HasValue)
                    residualOrderIds.Add(updateSaleOrderId.Value);

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
            Assert.False(await residualContext.PurchaseOrders.AnyAsync(item =>
                item.Remark == concurrentPoRemark
                || item.Remark == doublePoRemark
                || item.Remark == updatePoRemark
                || item.Remark == manualPoRemark
                || (item.Remark != null && item.Remark.StartsWith(batch.Id))));
            Assert.False(await residualContext.PurchasePlans.AnyAsync(item =>
                item.Remark == concurrentPlanRemark
                || item.Remark == doublePlanRemark
                || item.Remark == updatePlanRemark
                || (item.Remark != null && item.Remark.StartsWith(batch.Id))));
            Assert.False(await residualContext.SaleOrders.AnyAsync(item =>
                item.InnerRemark == concurrentSaleRemark
                || item.InnerRemark == doubleSaleRemark
                || item.InnerRemark == updateSaleRemark
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
            Assert.True(await residualContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await residualContext.Purchasers.AnyAsync(item => item.Id == managedPurchaserId));
        }
    }

    private static async Task<PlanSourcedDraft> CreatePlanSourcedDraftAsync(
        HttpClient client,
        Guid customerId,
        Guid wareId,
        Guid goodsId,
        Guid goodsUnitId,
        Guid supplierId,
        Guid purchaserId,
        decimal quantity,
        decimal fixedPrice,
        decimal expectedBaseQuantity,
        string saleInnerRemark,
        string planRemark,
        string poRemark,
        string orderDateUtc,
        string saleRemark)
    {
        SaleOrderDto saleOrder;
        using (var createResponse = await client.PostAsJsonAsync(
                   "/api/orders",
                   new
                   {
                       customerId,
                       wareId,
                       orderDate = orderDateUtc,
                       receiveDate = "2026-07-18T06:00:00Z",
                       contactName = "并发采购食堂",
                       contactPhone = "13800139701",
                       deliveryAddress = "上海市浦东新区采购并发路 1 号",
                       remark = saleRemark,
                       innerRemark = saleInnerRemark,
                       details = new[]
                       {
                           new
                           {
                               goodsId,
                               goodsUnitId,
                               quantity,
                               fixedPrice,
                               fixedGoodsUnitId = goodsUnitId,
                               remark = "T6并发来源明细",
                               innerRemark = $"{saleInnerRemark}L"
                           }
                       }
                   }))
        {
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
            saleOrder = await ReadApiDataAsync<SaleOrderDto>(createResponse);
        }

        using (var approveResponse = await client.PostAsJsonAsync(
                   $"/api/orders/{saleOrder.Id}/approve",
                   new SaleOrderAuditDto { Remark = $"{saleInnerRemark}-审核通过" }))
        {
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
            saleOrder = await ReadApiDataAsync<SaleOrderDto>(approveResponse);
            Assert.Equal(SaleOrderStatus.SortingPending, saleOrder.OrderStatus);
        }

        PurchasePlanDto plan;
        using (var generatePlan = await client.PostAsJsonAsync(
                   "/api/purchase-plans/generate",
                   new GeneratePurchasePlanFromOrdersDto
                   {
                       OrderIds = [saleOrder.Id],
                       Remark = planRemark
                   }))
        {
            Assert.Equal(HttpStatusCode.OK, generatePlan.StatusCode);
            plan = Assert.Single(await ReadApiDataAsync<List<PurchasePlanDto>>(generatePlan));
            Assert.Equal(PurchasePlanStatus.Unpublished, plan.PurchaseStatus);
            Assert.Equal(expectedBaseQuantity, Assert.Single(plan.Details).PlannedQuantity);
        }

        using (var assignSupplier = await client.PutAsJsonAsync(
                   "/api/purchase-plans/supplier",
                   new AssignPurchasePlanSupplierDto
                   {
                       PlanIds = [plan.Id],
                       SupplierId = supplierId
                   }))
        {
            Assert.Equal(HttpStatusCode.OK, assignSupplier.StatusCode);
        }

        using (var assignPurchaser = await client.PutAsJsonAsync(
                   "/api/purchase-plans/purchaser",
                   new AssignPurchasePlanPurchaserDto
                   {
                       PlanIds = [plan.Id],
                       PurchaserId = purchaserId
                   }))
        {
            Assert.Equal(HttpStatusCode.OK, assignPurchaser.StatusCode);
        }

        PurchaseOrderDto purchaseOrder;
        using (var generatePo = await client.PostAsJsonAsync(
                   "/api/purchase-orders/generate-from-plans",
                   new
                   {
                       planIds = new[] { plan.Id },
                       receiveTime = "2026-07-18T09:00:00Z",
                       remark = poRemark
                   }))
        {
            Assert.Equal(HttpStatusCode.OK, generatePo.StatusCode);
            purchaseOrder = Assert.Single(await ReadApiDataAsync<List<PurchaseOrderDto>>(generatePo));
            Assert.Equal(PurchaseOrderStatus.Draft, purchaseOrder.BusinessStatus);
            Assert.Equal(supplierId, purchaseOrder.SupplierId);
            Assert.Equal(purchaserId, purchaseOrder.PurchaserId);
            Assert.Equal(expectedBaseQuantity, Assert.Single(purchaseOrder.Details).PurchaseQuantity);
        }

        using (var planAfterGenerate = await client.GetAsync($"/api/purchase-plans/{plan.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, planAfterGenerate.StatusCode);
            var reloaded = await ReadApiDataAsync<PurchasePlanDto>(planAfterGenerate);
            Assert.Equal(PurchasePlanStatus.Generated, reloaded.PurchaseStatus);
            Assert.Equal(expectedBaseQuantity, Assert.Single(reloaded.Details).PurchasedQuantity);
        }

        return new PlanSourcedDraft(saleOrder.Id, plan.Id, purchaseOrder);
    }

    private sealed record PlanSourcedDraft(Guid SaleOrderId, Guid PlanId, PurchaseOrderDto PurchaseOrder);

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
