using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Storage;
using Domain.Entities;
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

namespace SkyRoc.Tests.Storage;

/// <summary>
///     T7 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证出入库并发审核/反审核、库存扣减竞赛、状态幂等与失败更新回滚。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class StockConcurrencyIdempotencyRollbackPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     入库双审核仅一胜；两张超额出库并发审核仅一胜且批次守恒；同单双反审核仅一胜；
    ///     重复动作幂等拒绝；失败更新不落库；无负库存；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task Stock_ConcurrencyIdempotencyAndRollback_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var adminUserId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var batchNo = $"{batch.Id}-BATCH";
        var inboundRemark = $"{batch.Id}-入库";
        var firstOutRemark = $"{batch.Id}-出库1";
        var secondOutRemark = $"{batch.Id}-出库2";
        var failedUpdateRemark = $"{batch.Id}-失败更新";
        var password = "SkyRocStockConc!2026";
        var userAgent = $"SkyRoc-T7-StockConc/{batch.Id}";
        var createName = "T7-StockConc";

        // 入库 50 @ 5.0；两张出库各 30，合计超过可用库存，并发审核必须恰好一胜
        var inboundQuantity = NumericPrecision.RoundQuantity(50m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(5m);
        var outboundQuantity = NumericPrecision.RoundQuantity(30m);
        var outboundUnitPrice = NumericPrecision.RoundMoney(6m);
        var expectedAfterOneOutbound = NumericPrecision.RoundQuantity(20m);

        Guid adminRoleId;
        Guid managedGoodsId;
        Guid managedGoodsUnitId;
        Guid managedWareId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var goodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var goodsUnitCode = DemoDataStableKeyCatalog.Create("GOODS-UNIT", 1);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);

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

            await seedContext.Users.AddAsync(new User
            {
                Id = adminUserId,
                Username = adminUsername,
                NickName = "T7库存并发操作员",
                Gender = GenderType.Male,
                Phone = "13900009821",
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

        Guid? inboundOrderId = null;
        Guid? firstOutOrderId = null;
        Guid? secondOutOrderId = null;
        Guid? createdBatchId = null;

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

            // 创建其他入库草稿
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
                Assert.Equal(StockDocumentStatus.Draft, inboundOrder.BusinessStatus);
                inboundOrderId = inboundOrder.Id;
                registry.Register<StockInOrder>(inboundOrder.Id, nameof(StockInOrder.Remark), inboundRemark);
            }

            // 并发双审核同一入库单：恰好一胜一负，仅生成一条批次与一条增加流水
            using var inboundAuditClient1 = factory.CreateClient();
            inboundAuditClient1.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            inboundAuditClient1.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using var inboundAuditClient2 = factory.CreateClient();
            inboundAuditClient2.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            inboundAuditClient2.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var inboundAuditTask1 = inboundAuditClient1.PostAsJsonAsync(
                $"/api/stock-in/other/{inboundOrder.Id}/audit",
                new StockInAuditDto { Remark = $"{inboundRemark}-并发审核1" });
            var inboundAuditTask2 = inboundAuditClient2.PostAsJsonAsync(
                $"/api/stock-in/other/{inboundOrder.Id}/audit",
                new StockInAuditDto { Remark = $"{inboundRemark}-并发审核2" });
            await Task.WhenAll(inboundAuditTask1, inboundAuditTask2);

            using var inboundAuditResponse1 = await inboundAuditTask1;
            using var inboundAuditResponse2 = await inboundAuditTask2;
            var inboundCode1 = await ApiHttpAssert.ReadBusinessCodeAsync(inboundAuditResponse1);
            var inboundCode2 = await ApiHttpAssert.ReadBusinessCodeAsync(inboundAuditResponse2);
            Assert.Equal(1, new[] { inboundCode1, inboundCode2 }.Count(code => code == ResponseCode.Success));
            Assert.Equal(1, new[] { inboundCode1, inboundCode2 }.Count(code => code == ResponseCode.DatabaseError));

            await using (var afterInbound = fixture.CreateDbContext())
            {
                var inboundEntity = await afterInbound.StockInOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == inboundOrder.Id);
                Assert.Equal(StockDocumentStatus.Audited, inboundEntity.BusinessStatus);

                var stockBatch = await afterInbound.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == batchNo);
                createdBatchId = stockBatch.Id;
                Assert.Equal(inboundQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);
                Assert.Equal(inboundUnitPrice, stockBatch.UnitCost);

                var increaseLedgers = await afterInbound.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == inboundOrder.Id
                                     && ledger.Direction == StockLedgerDirection.Increase)
                    .ToListAsync();
                Assert.Single(increaseLedgers);
                Assert.Equal(inboundQuantity, increaseLedgers[0].ChangeQuantity);
            }

            // 幂等：重复审核入库被拒
            using (var reAuditInbound = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/other/{inboundOrder.Id}/audit",
                       new StockInAuditDto { Remark = "重复审核入库" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reAuditInbound, ResponseCode.DatabaseError);
            }

            // 创建两张其他出库草稿（各 30，合计超库存）
            StockOutOrderDto firstOutOrder;
            using (var createFirstOut = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/other",
                       BuildCreateOtherOutPayload(
                           managedWareId,
                           createdBatchId!.Value,
                           managedGoodsUnitId,
                           outboundQuantity,
                           outboundUnitPrice,
                           firstOutRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createFirstOut.StatusCode);
                firstOutOrder = await ReadApiDataAsync<StockOutOrderDto>(createFirstOut);
                firstOutOrderId = firstOutOrder.Id;
                registry.Register<StockOutOrder>(firstOutOrder.Id, nameof(StockOutOrder.Remark), firstOutRemark);
            }

            StockOutOrderDto secondOutOrder;
            using (var createSecondOut = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/other",
                       BuildCreateOtherOutPayload(
                           managedWareId,
                           createdBatchId.Value,
                           managedGoodsUnitId,
                           outboundQuantity,
                           outboundUnitPrice,
                           secondOutRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createSecondOut.StatusCode);
                secondOutOrder = await ReadApiDataAsync<StockOutOrderDto>(createSecondOut);
                secondOutOrderId = secondOutOrder.Id;
                registry.Register<StockOutOrder>(secondOutOrder.Id, nameof(StockOutOrder.Remark), secondOutRemark);
            }

            // 并发库存扣减：两张超额出库同时审核，恰好一胜一负，批次剩余 20
            using var outAuditClient1 = factory.CreateClient();
            outAuditClient1.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            outAuditClient1.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using var outAuditClient2 = factory.CreateClient();
            outAuditClient2.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            outAuditClient2.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var outAuditTask1 = outAuditClient1.PostAsJsonAsync(
                $"/api/stock-out/other/{firstOutOrder.Id}/audit",
                new StockOutAuditDto { Remark = $"{firstOutRemark}-并发审核" });
            var outAuditTask2 = outAuditClient2.PostAsJsonAsync(
                $"/api/stock-out/other/{secondOutOrder.Id}/audit",
                new StockOutAuditDto { Remark = $"{secondOutRemark}-并发审核" });
            await Task.WhenAll(outAuditTask1, outAuditTask2);

            using var outAuditResponse1 = await outAuditTask1;
            using var outAuditResponse2 = await outAuditTask2;
            var outCode1 = await ApiHttpAssert.ReadBusinessCodeAsync(outAuditResponse1);
            var outCode2 = await ApiHttpAssert.ReadBusinessCodeAsync(outAuditResponse2);
            var firstOutOk = outCode1 == ResponseCode.Success;
            var secondOutOk = outCode2 == ResponseCode.Success;
            Assert.True(firstOutOk ^ secondOutOk, "并发超额出库审核必须恰好一个成功");
            Assert.True(
                (firstOutOk && outCode2 == ResponseCode.DatabaseError)
                || (secondOutOk && outCode1 == ResponseCode.DatabaseError),
                "失败方必须返回业务拒绝 code=502");

            var winnerOutId = firstOutOk ? firstOutOrder.Id : secondOutOrder.Id;
            var loserOutId = firstOutOk ? secondOutOrder.Id : firstOutOrder.Id;

            await using (var afterOutConcurrent = fixture.CreateDbContext())
            {
                var winner = await afterOutConcurrent.StockOutOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == winnerOutId);
                var loser = await afterOutConcurrent.StockOutOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == loserOutId);
                Assert.Equal(StockDocumentStatus.Audited, winner.BusinessStatus);
                Assert.Equal(StockDocumentStatus.Draft, loser.BusinessStatus);

                var stockBatch = await afterOutConcurrent.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId.Value);
                Assert.Equal(expectedAfterOneOutbound, stockBatch.CurrentQuantity);
                Assert.Equal(expectedAfterOneOutbound, stockBatch.AvailableQuantity);
                Assert.True(stockBatch.CurrentQuantity >= 0m);
                Assert.True(stockBatch.AvailableQuantity >= 0m);

                var decreaseLedgers = await afterOutConcurrent.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.StockBatchId == createdBatchId.Value
                                     && ledger.Direction == StockLedgerDirection.Decrease)
                    .ToListAsync();
                Assert.Single(decreaseLedgers);
                Assert.Equal(winnerOutId, decreaseLedgers[0].SourceOrderId);
                Assert.Equal(outboundQuantity, decreaseLedgers[0].ChangeQuantity);
            }

            // 幂等：胜者重复审核拒绝；败者删除草稿后不影响批次
            using (var reAuditWinner = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/other/{winnerOutId}/audit",
                       new StockOutAuditDto { Remark = "重复审核出库" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reAuditWinner, ResponseCode.DatabaseError);
            }

            using (var deleteLoser = await adminClient.DeleteAsync($"/api/stock-out/other/{loserOutId}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteLoser.StatusCode);
                await ReadApiDataAsync<bool>(deleteLoser);
            }

            // 败者已删，仅保留胜者供后续反审核与清理
            firstOutOrderId = firstOutOk ? winnerOutId : null;
            secondOutOrderId = secondOutOk ? winnerOutId : null;

            // 失败更新：已审核出库禁止编辑，状态与批次不变
            using (var failedUpdate = await adminClient.PutAsJsonAsync(
                       "/api/stock-out/other",
                       BuildUpdateOtherOutPayload(
                           winnerOutId,
                           managedWareId,
                           createdBatchId.Value,
                           managedGoodsUnitId,
                           outboundQuantity,
                           outboundUnitPrice,
                           failedUpdateRemark)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(failedUpdate, ResponseCode.DatabaseError);
            }

            await using (var afterFailedUpdate = fixture.CreateDbContext())
            {
                var winner = await afterFailedUpdate.StockOutOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == winnerOutId);
                Assert.Equal(StockDocumentStatus.Audited, winner.BusinessStatus);
                Assert.NotEqual(failedUpdateRemark, winner.Remark);

                var stockBatch = await afterFailedUpdate.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId.Value);
                Assert.Equal(expectedAfterOneOutbound, stockBatch.AvailableQuantity);
            }

            // 并发双反审核同一出库单：恰好一胜，批次恢复到入库量
            using var reverseClient1 = factory.CreateClient();
            reverseClient1.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            reverseClient1.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using var reverseClient2 = factory.CreateClient();
            reverseClient2.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            reverseClient2.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var reverseTask1 = reverseClient1.PostAsJsonAsync(
                $"/api/stock-out/other/{winnerOutId}/reverse-audit",
                new StockOutAuditDto { Remark = $"{batch.Id}-反审核1" });
            var reverseTask2 = reverseClient2.PostAsJsonAsync(
                $"/api/stock-out/other/{winnerOutId}/reverse-audit",
                new StockOutAuditDto { Remark = $"{batch.Id}-反审核2" });
            await Task.WhenAll(reverseTask1, reverseTask2);

            using var reverseResponse1 = await reverseTask1;
            using var reverseResponse2 = await reverseTask2;
            var reverseCode1 = await ApiHttpAssert.ReadBusinessCodeAsync(reverseResponse1);
            var reverseCode2 = await ApiHttpAssert.ReadBusinessCodeAsync(reverseResponse2);
            Assert.Equal(1, new[] { reverseCode1, reverseCode2 }.Count(code => code == ResponseCode.Success));
            Assert.Equal(1, new[] { reverseCode1, reverseCode2 }.Count(code => code == ResponseCode.DatabaseError));

            await using (var afterReverse = fixture.CreateDbContext())
            {
                var winner = await afterReverse.StockOutOrders.AsNoTracking()
                    .SingleAsync(item => item.Id == winnerOutId);
                Assert.Equal(StockDocumentStatus.Reversed, winner.BusinessStatus);

                var stockBatch = await afterReverse.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.Id == createdBatchId.Value);
                Assert.Equal(inboundQuantity, stockBatch.CurrentQuantity);
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);

                var reversalLedgers = await afterReverse.StockLedgers.AsNoTracking()
                    .Where(ledger => ledger.SourceOrderId == winnerOutId
                                     && ledger.Direction == StockLedgerDirection.Increase)
                    .ToListAsync();
                Assert.Single(reversalLedgers);
                Assert.Equal(outboundQuantity, reversalLedgers[0].ChangeQuantity);

                Assert.False(await afterReverse.StockBatches.AnyAsync(item =>
                    item.CurrentQuantity < 0m || item.AvailableQuantity < 0m));
            }

            // 幂等：重复反审核拒绝
            using (var reReverse = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/other/{winnerOutId}/reverse-audit",
                       new StockOutAuditDto { Remark = "重复反审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(reReverse, ResponseCode.DatabaseError);
            }
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualStockOutIds = new List<Guid>();
                if (firstOutOrderId.HasValue)
                    residualStockOutIds.Add(firstOutOrderId.Value);
                if (secondOutOrderId.HasValue)
                    residualStockOutIds.Add(secondOutOrderId.Value);

                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => residualStockOutIds.Contains(item.Id)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();

                var residualStockInIds = new List<Guid>();
                if (inboundOrderId.HasValue)
                    residualStockInIds.Add(inboundOrderId.Value);
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => residualStockInIds.Contains(item.Id)
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
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
            }

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
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
            Assert.False(await residualContext.StockOutOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockInOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockBatches.AnyAsync(item => item.BatchNo == batchNo));
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));
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
            inTime = "2026-07-18T09:00:00Z",
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
                    remark = "T7并发入库明细"
                }
            }
        };
    }

    private static object BuildCreateOtherOutPayload(
        Guid wareId,
        Guid stockBatchId,
        Guid goodsUnitId,
        decimal quantity,
        decimal unitPrice,
        string remark)
    {
        return new
        {
            wareId,
            outTime = "2026-07-18T10:00:00Z",
            remark,
            details = new[]
            {
                new
                {
                    stockBatchId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    remark = "T7并发出库明细"
                }
            }
        };
    }

    private static object BuildUpdateOtherOutPayload(
        Guid id,
        Guid wareId,
        Guid stockBatchId,
        Guid goodsUnitId,
        decimal quantity,
        decimal unitPrice,
        string remark)
    {
        return new
        {
            id,
            wareId,
            outTime = "2026-07-18T10:30:00Z",
            remark,
            details = new[]
            {
                new
                {
                    stockBatchId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    remark = "T7并发出库明细-失败更新"
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
