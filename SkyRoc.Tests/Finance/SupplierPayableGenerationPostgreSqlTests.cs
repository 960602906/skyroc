using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Finance;
using Application.DTOs.Storage;
using Domain.Entities;
using Domain.Entities.Finance;
using Domain.Entities.Purchases;
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
///     T10 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证采购入库/采购退货审核
///     自动生成供应商应付单据、草稿不生单、重复审核幂等、反审核移除、
///     余额独立重算与财务读权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class SupplierPayableGenerationPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     采购入库草稿无应付 → 审核生成正向应付 → 重复审核拒绝且不重复建单 →
    ///     采购退货草稿无应付 → 审核生成负向应付 → 明细独立重算一致 →
    ///     反审核退货移除负向单 → 反审核入库移除正向单 → 401/403 权限矩阵；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task SupplierPayable_PurchaseInReturnGenerationIdempotencyAndPermissionMatrix_OnPostgreSql()
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
        var purchaseInRemark = $"{batch.Id}-采购入库生应付";
        var purchaseReturnRemark = $"{batch.Id}-采购退货冲应付";
        var password = "SkyRocSupplierPayable!2026";
        var userAgent = $"SkyRoc-T10-SupplierPayable/{batch.Id}";
        var createName = "T10-SupplierPayableGeneration";

        var financeReadPermission = PermissionCodes.Business.Finance.Read;
        var financeCreatePermission = PermissionCodes.Business.Finance.Create;

        var inboundQuantity = NumericPrecision.RoundQuantity(10m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(6.5m);
        var returnQuantity = NumericPrecision.RoundQuantity(3m);
        var expectedInboundPayable = NumericPrecision.RoundMoney(inboundQuantity * inboundUnitPrice);
        var expectedReturnPayable = NumericPrecision.RoundMoney(-(returnQuantity * inboundUnitPrice));
        var expectedReturnDocument = NumericPrecision.RoundMoney(returnQuantity * inboundUnitPrice);

        Guid adminRoleId;
        Guid managedSupplierId;
        string managedSupplierName;
        Guid managedGoodsId;
        Guid managedGoodsUnitId;
        Guid managedWareId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var supplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);
            var goodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var goodsUnitCode = DemoDataStableKeyCatalog.Create("GOODS-UNIT", 1);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);

            var supplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == supplierCode);
            Assert.NotNull(supplier);
            managedSupplierId = supplier.Id;
            managedSupplierName = supplier.Name;

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
                Desc = "T10 供应商应付最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T10供应商应付操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001031",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T10供应商应付只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900001032",
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
                Title = "T10供应商应付只读菜单",
                Component = "page.t10.supplier.payable.seed",
                MenuType = MenuType.Menu,
                Order = 9103,
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

        Guid? purchaseInOrderId = null;
        Guid? purchaseReturnOrderId = null;
        Guid? inboundBillId = null;
        Guid? returnBillId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousBills = await anonymousClient.GetAsync(
                       $"/api/supplier-settlements/bills?current=1&size=10&supplierId={managedSupplierId}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousBills, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/supplier-settlements",
                       new CreateSupplierSettlementDto
                       {
                           Details =
                           [
                               new CreateSupplierSettlementDetailDto
                               {
                                   SupplierBillId = Guid.NewGuid(),
                                   PaymentAmount = 1m
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousCreate, ResponseCode.Unauthorized);
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

            StockInOrderDto purchaseInOrder;
            using (var createInbound = await adminClient.PostAsJsonAsync(
                       "/api/stock-in/purchase",
                       BuildCreatePurchaseInPayload(
                           managedWareId,
                           managedSupplierId,
                           managedGoodsId,
                           managedGoodsUnitId,
                           batchNo,
                           inboundQuantity,
                           inboundUnitPrice,
                           purchaseInRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createInbound.StatusCode);
                purchaseInOrder = await ReadApiDataAsync<StockInOrderDto>(createInbound);
                purchaseInOrderId = purchaseInOrder.Id;
                registry.Register<StockInOrder>(purchaseInOrder.Id, nameof(StockInOrder.Remark), purchaseInRemark);
                Assert.Equal(StockDocumentStatus.Draft, purchaseInOrder.BusinessStatus);
            }

            await using (var beforeAudit = fixture.CreateDbContext())
            {
                Assert.False(await beforeAudit.SupplierBills.AnyAsync(bill =>
                    bill.StockInOrderId == purchaseInOrder.Id));
            }

            StockInOrderDto auditedInbound;
            using (var auditInbound = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/purchase/{purchaseInOrder.Id}/audit",
                       new StockInAuditDto { Remark = $"{purchaseInRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditInbound.StatusCode);
                auditedInbound = await ReadApiDataAsync<StockInOrderDto>(auditInbound);
                Assert.Equal(StockDocumentStatus.Audited, auditedInbound.BusinessStatus);
            }

            await using (var afterInboundAudit = fixture.CreateDbContext())
            {
                var stockBatch = await afterInboundAudit.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == batchNo);
                createdBatchId = stockBatch.Id;
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);

                var bill = await afterInboundAudit.SupplierBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.StockInOrderId == purchaseInOrder.Id);
                inboundBillId = bill.Id;
                Assert.Equal(SupplierBillSourceType.PurchaseStockIn, bill.SourceType);
                Assert.Equal(managedSupplierId, bill.SupplierId);
                Assert.Equal(managedSupplierName, bill.SupplierNameSnapshot);
                Assert.Equal(purchaseInOrder.InNo, bill.SourceDocumentNoSnapshot);
                Assert.Equal(expectedInboundPayable, bill.DocumentAmount);
                Assert.Equal(expectedInboundPayable, bill.PayableAmount);
                Assert.Equal(0m, bill.SettledAmount);
                Assert.Equal(SupplierBillStatus.Pending, bill.BillStatus);
                AssertIndependentBillBalance(bill);

                var detail = Assert.Single(bill.Details);
                Assert.Equal(SupplierBillSourceType.PurchaseStockIn, detail.SourceType);
                Assert.Equal(inboundQuantity, detail.Quantity);
                Assert.Equal(inboundUnitPrice, detail.UnitPrice);
                Assert.Equal(expectedInboundPayable, detail.Amount);
            }

            using (var billsAfterInbound = await adminClient.GetAsync(
                       $"/api/supplier-settlements/bills?current=1&size=20&pendingOnly=true&supplierId={managedSupplierId}&sourceType={(int)SupplierBillSourceType.PurchaseStockIn}"))
            {
                Assert.Equal(HttpStatusCode.OK, billsAfterInbound.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<SupplierBillDto>>(billsAfterInbound);
                var matched = Assert.Single(
                    page.Records ?? [],
                    item => item.StockInOrderId == purchaseInOrder.Id);
                Assert.Equal(inboundBillId, matched.Id);
                Assert.Equal(expectedInboundPayable, matched.PayableAmount);
                Assert.Equal(expectedInboundPayable, matched.PendingAmount);
                Assert.Equal(SupplierBillStatus.Pending, matched.BillStatus);
            }

            using (var rejectRepeatAudit = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/purchase/{purchaseInOrder.Id}/audit",
                       new StockInAuditDto { Remark = $"{purchaseInRemark}-重复审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectRepeatAudit, ResponseCode.DatabaseError);
            }

            await using (var afterRepeatAudit = fixture.CreateDbContext())
            {
                Assert.Equal(1, await afterRepeatAudit.SupplierBills.CountAsync(bill =>
                    bill.StockInOrderId == purchaseInOrder.Id));
            }

            StockOutOrderDto purchaseReturnOrder;
            using (var createReturn = await adminClient.PostAsJsonAsync(
                       "/api/stock-out/purchase-return",
                       BuildPurchaseReturnPayload(
                           managedWareId,
                           managedSupplierId,
                           createdBatchId!.Value,
                           managedGoodsUnitId,
                           returnQuantity,
                           inboundUnitPrice,
                           purchaseReturnRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createReturn.StatusCode);
                purchaseReturnOrder = await ReadApiDataAsync<StockOutOrderDto>(createReturn);
                purchaseReturnOrderId = purchaseReturnOrder.Id;
                registry.Register<StockOutOrder>(
                    purchaseReturnOrder.Id,
                    nameof(StockOutOrder.Remark),
                    purchaseReturnRemark);
                Assert.Equal(StockDocumentStatus.Draft, purchaseReturnOrder.BusinessStatus);
            }

            await using (var beforeReturnAudit = fixture.CreateDbContext())
            {
                Assert.False(await beforeReturnAudit.SupplierBills.AnyAsync(bill =>
                    bill.StockOutOrderId == purchaseReturnOrder.Id));
            }

            using (var auditReturn = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/purchase-return/{purchaseReturnOrder.Id}/audit",
                       new StockOutAuditDto { Remark = $"{purchaseReturnRemark}-审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, auditReturn.StatusCode);
                var audited = await ReadApiDataAsync<StockOutOrderDto>(auditReturn);
                Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
            }

            await using (var afterReturnAudit = fixture.CreateDbContext())
            {
                var bill = await afterReturnAudit.SupplierBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.StockOutOrderId == purchaseReturnOrder.Id);
                returnBillId = bill.Id;
                Assert.Equal(SupplierBillSourceType.PurchaseReturnOut, bill.SourceType);
                Assert.Equal(managedSupplierId, bill.SupplierId);
                Assert.Equal(expectedReturnDocument, bill.DocumentAmount);
                Assert.Equal(expectedReturnPayable, bill.PayableAmount);
                Assert.Equal(0m, bill.SettledAmount);
                Assert.Equal(SupplierBillStatus.Pending, bill.BillStatus);
                AssertIndependentBillBalance(bill);

                var detail = Assert.Single(bill.Details);
                Assert.Equal(SupplierBillSourceType.PurchaseReturnOut, detail.SourceType);
                Assert.Equal(-returnQuantity, detail.Quantity);
                Assert.Equal(expectedReturnPayable, detail.Amount);

                Assert.Equal(2, await afterReturnAudit.SupplierBills.CountAsync(item =>
                    item.StockInOrderId == purchaseInOrder.Id
                    || item.StockOutOrderId == purchaseReturnOrder.Id));
            }

            using (var rejectRepeatReturnAudit = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/purchase-return/{purchaseReturnOrder.Id}/audit",
                       new StockOutAuditDto { Remark = $"{purchaseReturnRemark}-重复审核" }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectRepeatReturnAudit, ResponseCode.DatabaseError);
            }

            using (var reverseReturn = await adminClient.PostAsJsonAsync(
                       $"/api/stock-out/purchase-return/{purchaseReturnOrder.Id}/reverse-audit",
                       new StockOutAuditDto { Remark = $"{purchaseReturnRemark}-反审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, reverseReturn.StatusCode);
                var reversed = await ReadApiDataAsync<StockOutOrderDto>(reverseReturn);
                Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);
            }

            await using (var afterReverseReturn = fixture.CreateDbContext())
            {
                Assert.False(await afterReverseReturn.SupplierBills.AnyAsync(bill =>
                    bill.Id == returnBillId.Value
                    || bill.StockOutOrderId == purchaseReturnOrder.Id));
                Assert.True(await afterReverseReturn.SupplierBills.AnyAsync(bill =>
                    bill.Id == inboundBillId.Value));
            }

            returnBillId = null;

            using (var reverseInbound = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/purchase/{purchaseInOrder.Id}/reverse-audit",
                       new StockInAuditDto { Remark = $"{purchaseInRemark}-反审核" }))
            {
                Assert.Equal(HttpStatusCode.OK, reverseInbound.StatusCode);
                var reversed = await ReadApiDataAsync<StockInOrderDto>(reverseInbound);
                Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);
            }

            await using (var afterReverseInbound = fixture.CreateDbContext())
            {
                Assert.False(await afterReverseInbound.SupplierBills.AnyAsync(bill =>
                    bill.Id == inboundBillId.Value
                    || bill.StockInOrderId == purchaseInOrder.Id));
            }

            inboundBillId = null;

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
            }

            using (var allowedBills = await limitedClient.GetAsync(
                       $"/api/supplier-settlements/bills?current=1&size=10&supplierId={managedSupplierId}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedBills.StatusCode);
                await ReadApiDataAsync<PagedResult<SupplierBillDto>>(allowedBills);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/supplier-settlements",
                       new CreateSupplierSettlementDto
                       {
                           Details =
                           [
                               new CreateSupplierSettlementDetailDto
                               {
                                   SupplierBillId = Guid.NewGuid(),
                                   PaymentAmount = 1m
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
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
            Assert.True(await auditContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await auditContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await auditContext.Wares.AnyAsync(item => item.Id == managedWareId));
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualStockOutIds = new List<Guid>();
                if (purchaseReturnOrderId.HasValue)
                    residualStockOutIds.Add(purchaseReturnOrderId.Value);
                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => residualStockOutIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == purchaseReturnRemark
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualStockInIds = new List<Guid>();
                if (purchaseInOrderId.HasValue)
                    residualStockInIds.Add(purchaseInOrderId.Value);
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => residualStockInIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == purchaseInRemark
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualOrderIdSet = residualStockOutOrders.Select(item => item.Id)
                    .Concat(residualStockInOrders.Select(item => item.Id))
                    .ToHashSet();

                var residualBillIds = new List<Guid>();
                if (inboundBillId.HasValue)
                    residualBillIds.Add(inboundBillId.Value);
                if (returnBillId.HasValue)
                    residualBillIds.Add(returnBillId.Value);

                var residualBills = await cleanupContext.SupplierBills
                    .Where(item => residualBillIds.Contains(item.Id)
                                   || (item.StockInOrderId.HasValue
                                       && residualOrderIdSet.Contains(item.StockInOrderId.Value))
                                   || (item.StockOutOrderId.HasValue
                                       && residualOrderIdSet.Contains(item.StockOutOrderId.Value)))
                    .ToListAsync();
                if (residualBills.Count > 0)
                {
                    cleanupContext.SupplierBills.RemoveRange(residualBills);
                    await cleanupContext.SaveChangesAsync();
                }

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
            Assert.False(await residualContext.SupplierBills.AnyAsync(item =>
                (inboundBillId.HasValue && item.Id == inboundBillId.Value)
                || (returnBillId.HasValue && item.Id == returnBillId.Value)
                || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value)
                || (purchaseReturnOrderId.HasValue && item.StockOutOrderId == purchaseReturnOrderId.Value)));
            Assert.False(await residualContext.StockOutOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockInOrders.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.StockBatches.AnyAsync(item => item.BatchNo == batchNo));
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
            Assert.True(await residualContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
        }
    }

    private static void AssertIndependentBillBalance(SupplierBill bill)
    {
        var documentAmount = NumericPrecision.RoundMoney(
            bill.Details.Sum(detail => Math.Abs(detail.Amount)));
        var payableAmount = NumericPrecision.RoundMoney(
            bill.Details.Sum(detail => detail.Amount));
        Assert.Equal(documentAmount, bill.DocumentAmount);
        Assert.Equal(payableAmount, bill.PayableAmount);
        Assert.True(bill.SettledAmount >= 0m);
        Assert.True(bill.SettledAmount <= bill.DocumentAmount);
    }

    private static object BuildCreatePurchaseInPayload(
        Guid wareId,
        Guid supplierId,
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
            supplierId,
            purchasePattern = PurchasePattern.SupplierDirect,
            inTime = "2026-07-20T09:00:00Z",
            expectedArrivalTime = "2026-07-20T15:00:00Z",
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
                    productDate = "2026-07-19",
                    remark = "T10采购入库明细"
                }
            }
        };
    }

    private static object BuildPurchaseReturnPayload(
        Guid wareId,
        Guid supplierId,
        Guid stockBatchId,
        Guid goodsUnitId,
        decimal quantity,
        decimal unitPrice,
        string remark)
    {
        return new
        {
            wareId,
            supplierId,
            outTime = "2026-07-20T11:00:00Z",
            remark,
            details = new[]
            {
                new
                {
                    stockBatchId,
                    goodsUnitId,
                    quantity,
                    unitPrice,
                    remark = "T10采购退货出库明细"
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
