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
///     T10 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证供应商结算单的
///     部分/全部结款、优惠核销、超额拒绝、作废回滚、余额独立重算与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class SupplierSettlementPartialFullVoidPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     采购入库审核生成应付 → 部分结款（付款+优惠）→ 超额/零金额拒绝 →
    ///     尾款结清 → 余额独立重算 → 作废尾款回滚 → 重复作废拒绝 →
    ///     401/403 权限矩阵；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task SupplierSettlement_PartialFullVoidDiscountAndPermissionMatrix_OnPostgreSql()
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
        var partialSerialNo = $"{batch.Id}-BANK-P";
        var finalSerialNo = $"{batch.Id}-BANK-F";
        var partialSettlementRemark = $"{batch.Id}-部分结款优惠";
        var finalSettlementRemark = $"{batch.Id}-尾款结清";
        var voidRemark = $"{batch.Id}-尾款作废回滚";
        var password = "SkyRocSupplierSettlement!2026";
        var userAgent = $"SkyRoc-T10-SupplierSettlement/{batch.Id}";
        var createName = "T10-SupplierSettlement";

        var financeReadPermission = PermissionCodes.Business.Finance.Read;
        var financeCreatePermission = PermissionCodes.Business.Finance.Create;
        var financeDeletePermission = PermissionCodes.Business.Finance.Delete;

        var inboundQuantity = NumericPrecision.RoundQuantity(10m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(6.5m);
        var expectedPayable = NumericPrecision.RoundMoney(inboundQuantity * inboundUnitPrice);
        var partialPayment = NumericPrecision.RoundMoney(30m);
        var partialDiscount = NumericPrecision.RoundMoney(5m);
        var partialApplied = NumericPrecision.RoundMoney(partialPayment + partialDiscount);
        var remainingAfterPartial = NumericPrecision.RoundMoney(expectedPayable - partialApplied);

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
                Desc = "T10 供应商结算最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T10供应商结算操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001041",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T10供应商结算只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900001042",
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
                Title = "T10供应商结算只读菜单",
                Component = "page.t10.supplier.settlement.seed",
                MenuType = MenuType.Menu,
                Order = 9104,
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
        Guid? supplierBillId = null;
        Guid? partialSettlementId = null;
        Guid? finalSettlementId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

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

            using (var anonymousVoid = await anonymousClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, $"/api/supplier-settlements/{Guid.NewGuid()}/void")
                       {
                           Content = JsonContent.Create(new VoidSupplierSettlementDto { Remark = "未认证作废" })
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousVoid, ResponseCode.Unauthorized);
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

            using (var auditInbound = await adminClient.PostAsJsonAsync(
                       $"/api/stock-in/purchase/{purchaseInOrder.Id}/audit",
                       new StockInAuditDto { Remark = $"{purchaseInRemark}-审核" }))
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

                var bill = await afterInbound.SupplierBills
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.StockInOrderId == purchaseInOrder.Id);
                supplierBillId = bill.Id;
                Assert.Equal(managedSupplierId, bill.SupplierId);
                Assert.Equal(managedSupplierName, bill.SupplierNameSnapshot);
                Assert.Equal(expectedPayable, bill.DocumentAmount);
                Assert.Equal(expectedPayable, bill.PayableAmount);
                Assert.Equal(0m, bill.SettledAmount);
                Assert.Equal(SupplierBillStatus.Pending, bill.BillStatus);
                AssertIndependentBillBalance(bill);
            }

            SupplierSettlementDto partialSettlement;
            using (var createPartial = await adminClient.PostAsJsonAsync(
                       "/api/supplier-settlements",
                       new CreateSupplierSettlementDto
                       {
                           SettlementDate = new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc),
                           SerialNo = partialSerialNo,
                           Remark = partialSettlementRemark,
                           Details =
                           [
                               new CreateSupplierSettlementDetailDto
                               {
                                   SupplierBillId = supplierBillId!.Value,
                                   PaymentAmount = partialPayment,
                                   DiscountAmount = partialDiscount,
                                   Remark = $"{batch.Id}-明细优惠"
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createPartial.StatusCode);
                partialSettlement = await ReadApiDataAsync<SupplierSettlementDto>(createPartial);
                partialSettlementId = partialSettlement.Id;
                registry.Register<SupplierSettlement>(
                    partialSettlement.Id,
                    nameof(SupplierSettlement.SerialNo),
                    partialSerialNo);
            }

            Assert.Equal(SupplierSettlementStatus.PartiallySettled, partialSettlement.SettlementStatus);
            Assert.Equal(expectedPayable, partialSettlement.ShouldAmount);
            Assert.Equal(partialPayment, partialSettlement.PaymentAmount);
            Assert.Equal(partialDiscount, partialSettlement.DiscountAmount);
            Assert.Equal(partialApplied, partialSettlement.AppliedAmount);
            Assert.Equal(remainingAfterPartial, partialSettlement.RemainingAmount);
            var partialDetail = Assert.Single(partialSettlement.Details);
            Assert.Equal(partialApplied, partialDetail.AppliedAmount);
            Assert.Equal(remainingAfterPartial, partialDetail.RemainingAmount);

            await using (var afterPartial = fixture.CreateDbContext())
            {
                var bill = await afterPartial.SupplierBills.AsNoTracking()
                    .SingleAsync(item => item.Id == supplierBillId.Value);
                Assert.Equal(partialApplied, bill.SettledAmount);
                Assert.Equal(SupplierBillStatus.PartiallySettled, bill.BillStatus);
                AssertIndependentBillBalance(bill);
            }

            using (var rejectOver = await adminClient.PostAsJsonAsync(
                       "/api/supplier-settlements",
                       new CreateSupplierSettlementDto
                       {
                           SerialNo = $"{batch.Id}-BANK-OVER",
                           Remark = $"{batch.Id}-超额结款",
                           Details =
                           [
                               new CreateSupplierSettlementDetailDto
                               {
                                   SupplierBillId = supplierBillId.Value,
                                   PaymentAmount = NumericPrecision.RoundMoney(remainingAfterPartial + 1m)
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectOver, ResponseCode.DatabaseError);
            }

            using (var rejectZero = await adminClient.PostAsJsonAsync(
                       "/api/supplier-settlements",
                       new CreateSupplierSettlementDto
                       {
                           SerialNo = $"{batch.Id}-BANK-ZERO",
                           Remark = $"{batch.Id}-零金额结款",
                           Details =
                           [
                               new CreateSupplierSettlementDetailDto
                               {
                                   SupplierBillId = supplierBillId.Value,
                                   PaymentAmount = 0m,
                                   DiscountAmount = 0m
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectZero, ResponseCode.ValidationError);
            }

            await using (var afterReject = fixture.CreateDbContext())
            {
                Assert.False(await afterReject.SupplierSettlements.AnyAsync(item =>
                    item.SerialNo == $"{batch.Id}-BANK-OVER"
                    || item.SerialNo == $"{batch.Id}-BANK-ZERO"));
                var bill = await afterReject.SupplierBills.AsNoTracking()
                    .SingleAsync(item => item.Id == supplierBillId.Value);
                Assert.Equal(partialApplied, bill.SettledAmount);
                Assert.Equal(SupplierBillStatus.PartiallySettled, bill.BillStatus);
            }

            SupplierSettlementDto finalSettlement;
            using (var createFinal = await adminClient.PostAsJsonAsync(
                       "/api/supplier-settlements",
                       new CreateSupplierSettlementDto
                       {
                           SettlementDate = new DateTime(2026, 7, 20, 13, 0, 0, DateTimeKind.Utc),
                           SerialNo = finalSerialNo,
                           Remark = finalSettlementRemark,
                           Details =
                           [
                               new CreateSupplierSettlementDetailDto
                               {
                                   SupplierBillId = supplierBillId.Value,
                                   PaymentAmount = remainingAfterPartial,
                                   DiscountAmount = 0m,
                                   Remark = $"{batch.Id}-明细尾款"
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createFinal.StatusCode);
                finalSettlement = await ReadApiDataAsync<SupplierSettlementDto>(createFinal);
                finalSettlementId = finalSettlement.Id;
                registry.Register<SupplierSettlement>(
                    finalSettlement.Id,
                    nameof(SupplierSettlement.SerialNo),
                    finalSerialNo);
            }

            Assert.Equal(SupplierSettlementStatus.Settled, finalSettlement.SettlementStatus);
            Assert.Equal(remainingAfterPartial, finalSettlement.ShouldAmount);
            Assert.Equal(remainingAfterPartial, finalSettlement.PaymentAmount);
            Assert.Equal(0m, finalSettlement.DiscountAmount);
            Assert.Equal(remainingAfterPartial, finalSettlement.AppliedAmount);
            Assert.Equal(0m, finalSettlement.RemainingAmount);

            await using (var afterFinal = fixture.CreateDbContext())
            {
                var bill = await afterFinal.SupplierBills.AsNoTracking()
                    .SingleAsync(item => item.Id == supplierBillId.Value);
                Assert.Equal(expectedPayable, bill.SettledAmount);
                Assert.Equal(SupplierBillStatus.Settled, bill.BillStatus);
                AssertIndependentBillBalance(bill);

                var activeApplied = await afterFinal.SupplierSettlementDetails
                    .AsNoTracking()
                    .Where(detail => detail.SupplierBillId == supplierBillId.Value
                                     && detail.SupplierSettlement.SettlementStatus
                                     != SupplierSettlementStatus.Voided)
                    .SumAsync(detail => detail.AppliedAmount);
                Assert.Equal(expectedPayable, NumericPrecision.RoundMoney(activeApplied));
            }

            SupplierSettlementDto voidedFinal;
            using (var voidFinal = await adminClient.SendAsync(
                       new HttpRequestMessage(
                           HttpMethod.Delete,
                           $"/api/supplier-settlements/{finalSettlement.Id}/void")
                       {
                           Content = JsonContent.Create(new VoidSupplierSettlementDto { Remark = voidRemark })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, voidFinal.StatusCode);
                voidedFinal = await ReadApiDataAsync<SupplierSettlementDto>(voidFinal);
            }

            Assert.Equal(SupplierSettlementStatus.Voided, voidedFinal.SettlementStatus);
            Assert.Equal(voidRemark, voidedFinal.Remark);
            Assert.NotNull(voidedFinal.VoidedTime);
            Assert.False(string.IsNullOrWhiteSpace(voidedFinal.VoidedByName));

            await using (var afterVoid = fixture.CreateDbContext())
            {
                var bill = await afterVoid.SupplierBills.AsNoTracking()
                    .SingleAsync(item => item.Id == supplierBillId.Value);
                Assert.Equal(partialApplied, bill.SettledAmount);
                Assert.Equal(SupplierBillStatus.PartiallySettled, bill.BillStatus);
                AssertIndependentBillBalance(bill);

                var activeApplied = await afterVoid.SupplierSettlementDetails
                    .AsNoTracking()
                    .Where(detail => detail.SupplierBillId == supplierBillId.Value
                                     && detail.SupplierSettlement.SettlementStatus
                                     != SupplierSettlementStatus.Voided)
                    .SumAsync(detail => detail.AppliedAmount);
                Assert.Equal(partialApplied, NumericPrecision.RoundMoney(activeApplied));
            }

            using (var rejectRepeatVoid = await adminClient.SendAsync(
                       new HttpRequestMessage(
                           HttpMethod.Delete,
                           $"/api/supplier-settlements/{finalSettlement.Id}/void")
                       {
                           Content = JsonContent.Create(new VoidSupplierSettlementDto
                           {
                               Remark = $"{batch.Id}-重复作废"
                           })
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectRepeatVoid, ResponseCode.DatabaseError);
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
                Assert.Contains(financeReadPermission, info.Buttons);
                Assert.DoesNotContain(financeCreatePermission, info.Buttons);
                Assert.DoesNotContain(financeDeletePermission, info.Buttons);
            }

            using (var allowedGet = await limitedClient.GetAsync(
                       $"/api/supplier-settlements/{partialSettlement.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedGet.StatusCode);
                var settlement = await ReadApiDataAsync<SupplierSettlementDto>(allowedGet);
                Assert.Equal(partialSettlement.Id, settlement.Id);
            }

            using (var allowedBills = await limitedClient.GetAsync(
                       $"/api/supplier-settlements/bills?current=1&size=20&supplierId={managedSupplierId}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedBills.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<SupplierBillDto>>(allowedBills);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records, item => item.Id == supplierBillId.Value);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/supplier-settlements",
                       new CreateSupplierSettlementDto
                       {
                           SerialNo = $"{batch.Id}-BANK-DENIED",
                           Remark = $"{batch.Id}-无创建权限",
                           Details =
                           [
                               new CreateSupplierSettlementDetailDto
                               {
                                   SupplierBillId = supplierBillId.Value,
                                   PaymentAmount = NumericPrecision.RoundMoney(1m)
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedVoid = await limitedClient.SendAsync(
                       new HttpRequestMessage(
                           HttpMethod.Delete,
                           $"/api/supplier-settlements/{partialSettlement.Id}/void")
                       {
                           Content = JsonContent.Create(new VoidSupplierSettlementDto
                           {
                               Remark = $"{batch.Id}-无作废权限"
                           })
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedVoid, ResponseCode.Forbidden);
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
                var residualSettlementIds = new List<Guid>();
                if (partialSettlementId.HasValue)
                    residualSettlementIds.Add(partialSettlementId.Value);
                if (finalSettlementId.HasValue)
                    residualSettlementIds.Add(finalSettlementId.Value);

                var residualSettlements = await cleanupContext.SupplierSettlements
                    .Where(item => residualSettlementIds.Contains(item.Id)
                                   || (item.SerialNo != null && item.SerialNo.StartsWith(batch.Id))
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualSettlements.Count > 0)
                {
                    cleanupContext.SupplierSettlements.RemoveRange(residualSettlements);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualStockInIds = new List<Guid>();
                if (purchaseInOrderId.HasValue)
                    residualStockInIds.Add(purchaseInOrderId.Value);
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => residualStockInIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == purchaseInRemark
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualOrderIdSet = residualStockInOrders.Select(item => item.Id).ToHashSet();

                var residualBillIds = new List<Guid>();
                if (supplierBillId.HasValue)
                    residualBillIds.Add(supplierBillId.Value);

                var residualBills = await cleanupContext.SupplierBills
                    .Where(item => residualBillIds.Contains(item.Id)
                                   || (item.StockInOrderId.HasValue
                                       && residualOrderIdSet.Contains(item.StockInOrderId.Value)))
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
            Assert.False(await residualContext.SupplierSettlements.AnyAsync(item =>
                (partialSettlementId.HasValue && item.Id == partialSettlementId.Value)
                || (finalSettlementId.HasValue && item.Id == finalSettlementId.Value)
                || (item.SerialNo != null && item.SerialNo.StartsWith(batch.Id))
                || (item.Remark != null && item.Remark.StartsWith(batch.Id))));
            Assert.False(await residualContext.SupplierBills.AnyAsync(item =>
                (supplierBillId.HasValue && item.Id == supplierBillId.Value)
                || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value)));
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
        var pending = NumericPrecision.RoundMoney(
            Math.Max(0m, bill.DocumentAmount - bill.SettledAmount));
        var expectedStatus = pending <= 0m && bill.SettledAmount > 0m
            ? SupplierBillStatus.Settled
            : bill.SettledAmount <= 0m
                ? SupplierBillStatus.Pending
                : SupplierBillStatus.PartiallySettled;
        Assert.Equal(expectedStatus, bill.BillStatus);
        if (expectedStatus == SupplierBillStatus.Settled)
            Assert.Equal(bill.DocumentAmount, bill.SettledAmount);
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
