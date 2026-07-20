using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Orders;
using Application.DTOs.Storage;
using Application.DTOs.Traceability;
using Domain.Entities;
using Domain.Entities.Finance;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Entities.System;
using Domain.Entities.Traceability;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Common;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Traceability;

/// <summary>
///     T11 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证检测报告维护、
///     销售订单商品溯源生成、公开二维码匿名边界、外部报送日志读取与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class InspectionReportTraceQrPushPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     采购入库审核 → 可送检入库 → 超数量拒绝 → 可删报告 → 正式报告快照与更新 →
    ///     销售出库审核 → 溯源生成/幂等 → 报告冻结 → 匿名二维码白名单 → 报送日志读取 →
    ///     401/403 权限矩阵；临时批次数据精确清理。
    /// </summary>
    [Fact]
    public async Task InspectionReport_TraceGenerateQrAnonymityPushLogAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedReadButtonId = Guid.NewGuid();
        var pushLogId = Guid.NewGuid();

        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var batchNo = $"{batch.Id}-BATCH";
        var purchaseInRemark = $"{batch.Id}-采购入库溯源";
        var disposableReportRemark = $"{batch.Id}-可删检测报告";
        var reportRemark = $"{batch.Id}-正式检测报告";
        var updatedReportRemark = $"{batch.Id}-正式检测报告已更新";
        var saleOrderInnerRemark = $"{batch.Id}O";
        var saleOutRemark = $"{batch.Id}-销售出库溯源";
        var traceRemark = $"{batch.Id}-溯源记录";
        var pushPlatformCode = $"{batch.Id}-PLATFORM";
        var pushCreateName = $"{batch.Id}-PUSH";
        var password = "SkyRocT11Trace!2026";
        var userAgent = $"SkyRoc-T11-Trace/{batch.Id}";
        var createName = "T11-InspectionTraceQr";
        var contactName = "溯源联调王老师";
        var contactPhone = "13900001101";
        var deliveryAddress = "杭州市西湖区溯源联调路 11 号食堂";

        var traceReadPermission = PermissionCodes.Business.Traceability.Read;
        var traceCreatePermission = PermissionCodes.Business.Traceability.Create;
        var traceUpdatePermission = PermissionCodes.Business.Traceability.Update;
        var traceDeletePermission = PermissionCodes.Business.Traceability.Delete;

        var inboundQuantity = NumericPrecision.RoundQuantity(10m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(4.5m);
        var sampleQuantity = NumericPrecision.RoundQuantity(2.5m);
        var saleQuantity = NumericPrecision.RoundQuantity(3m);
        var saleUnitPrice = NumericPrecision.RoundMoney(9m);

        Guid adminRoleId;
        Guid managedCustomerId;
        string managedCustomerName = null!;
        Guid managedSupplierId;
        string managedSupplierName = null!;
        Guid managedGoodsId;
        string managedGoodsName = null!;
        string managedGoodsCode = null!;
        Guid managedGoodsUnitId;
        Guid managedWareId;
        string managedWareName = null!;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var customerCode = DemoDataStableKeyCatalog.Create("CUSTOMER", 1);
            var supplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);
            var goodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var goodsUnitCode = DemoDataStableKeyCatalog.Create("GOODS-UNIT", 1);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);

            var customer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == customerCode);
            Assert.NotNull(customer);
            managedCustomerId = customer.Id;
            managedCustomerName = customer.Name;

            var supplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == supplierCode);
            Assert.NotNull(supplier);
            managedSupplierId = supplier.Id;
            managedSupplierName = supplier.Name;

            var goods = await seedContext.Set<GoodsEntity>().AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsCode);
            Assert.NotNull(goods);
            managedGoodsId = goods.Id;
            managedGoodsName = goods.Name;
            managedGoodsCode = goods.Code;

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

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T11 溯源只读临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T11溯源操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001111",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T11溯源只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900001112",
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
                Title = "T11溯源只读菜单",
                Component = "page.t11.traceability.seed",
                MenuType = MenuType.Menu,
                Order = 9111,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedReadButtonId,
                Code = traceReadPermission,
                Desc = "T11 溯源读取权限按钮",
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
        Guid? inboundBillId = null;
        Guid? saleOrderId = null;
        Guid? saleOutOrderId = null;
        Guid? inspectionReportId = null;
        Guid? traceRecordId = null;
        Guid? createdBatchId = null;
        string? generatedTraceNo = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousReports = await anonymousClient.GetAsync(
                       "/api/traceability/inspection-reports?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousReports, ResponseCode.Unauthorized);
            }

            using (var anonymousGenerate = await anonymousClient.PostAsync(
                       $"/api/traceability/traces/sale-orders/{Guid.NewGuid()}/generate",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousGenerate, ResponseCode.Unauthorized);
            }

            using (var anonymousPushLogs = await anonymousClient.GetAsync(
                       "/api/traceability/push-logs?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousPushLogs, ResponseCode.Unauthorized);
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

            Guid stockInDetailId;
            await using (var afterInbound = fixture.CreateDbContext())
            {
                var stockBatch = await afterInbound.StockBatches.AsNoTracking()
                    .SingleAsync(item => item.WareId == managedWareId
                                         && item.GoodsId == managedGoodsId
                                         && item.BatchNo == batchNo);
                createdBatchId = stockBatch.Id;
                Assert.Equal(inboundQuantity, stockBatch.AvailableQuantity);

                var detail = await afterInbound.StockInDetails.AsNoTracking()
                    .SingleAsync(item => item.StockInOrderId == purchaseInOrder.Id);
                stockInDetailId = detail.Id;

                var bill = await afterInbound.SupplierBills.AsNoTracking()
                    .SingleAsync(item => item.StockInOrderId == purchaseInOrder.Id);
                inboundBillId = bill.Id;
            }

            using (var eligibleOrders = await adminClient.GetAsync(
                       "/api/traceability/inspection-reports/eligible-stock-ins?current=1&size=100"))
            {
                Assert.Equal(HttpStatusCode.OK, eligibleOrders.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<InspectionStockInOrderDto>>(eligibleOrders);
                Assert.Contains(page.Records!, item => item.Id == purchaseInOrder.Id);
            }

            IReadOnlyList<InspectionStockInDetailDto> eligibleDetails;
            using (var detailsResponse = await adminClient.GetAsync(
                       $"/api/traceability/inspection-reports/eligible-stock-ins/{purchaseInOrder.Id}/details"))
            {
                Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
                eligibleDetails = await ReadApiDataAsync<IReadOnlyList<InspectionStockInDetailDto>>(detailsResponse);
            }

            var eligibleDetail = Assert.Single(eligibleDetails, item => item.Id == stockInDetailId);
            Assert.Equal(managedGoodsId, eligibleDetail.GoodsId);
            Assert.Equal(batchNo, eligibleDetail.BatchNo);
            Assert.Equal(inboundQuantity, eligibleDetail.Quantity);

            using (var rejectOverflow = await adminClient.PostAsJsonAsync(
                       "/api/traceability/inspection-reports",
                       BuildSaveInspectionReportPayload(
                           purchaseInOrder.Id,
                           stockInDetailId,
                           NumericPrecision.RoundQuantity(inboundQuantity + 0.000001m),
                           $"{batch.Id}-超数量")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectOverflow, ResponseCode.DatabaseError);
            }

            await using (var afterOverflow = fixture.CreateDbContext())
            {
                Assert.False(await afterOverflow.InspectionReports.AnyAsync(item =>
                    item.StockInOrderId == purchaseInOrder.Id
                    && item.Remark != null
                    && item.Remark.StartsWith(batch.Id)));
            }

            InspectionReportDto disposableReport;
            using (var createDisposable = await adminClient.PostAsJsonAsync(
                       "/api/traceability/inspection-reports",
                       BuildSaveInspectionReportPayload(
                           purchaseInOrder.Id,
                           stockInDetailId,
                           sampleQuantity,
                           disposableReportRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, createDisposable.StatusCode);
                disposableReport = await ReadApiDataAsync<InspectionReportDto>(createDisposable);
                registry.Register<InspectionReport>(
                    disposableReport.Id,
                    nameof(InspectionReport.Remark),
                    disposableReportRemark);
            }

            using (var deleteDisposable = await adminClient.DeleteAsync(
                       $"/api/traceability/inspection-reports/{disposableReport.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteDisposable.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteDisposable));
            }

            await using (var afterDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterDelete.InspectionReports.AnyAsync(item => item.Id == disposableReport.Id));
            }

            InspectionReportDto createdReport;
            using (var createReport = await adminClient.PostAsJsonAsync(
                       "/api/traceability/inspection-reports",
                       BuildSaveInspectionReportPayload(
                           purchaseInOrder.Id,
                           stockInDetailId,
                           sampleQuantity,
                           reportRemark,
                           withAttachment: true)))
            {
                Assert.Equal(HttpStatusCode.OK, createReport.StatusCode);
                createdReport = await ReadApiDataAsync<InspectionReportDto>(createReport);
                inspectionReportId = createdReport.Id;
            }

            Assert.StartsWith("IR", createdReport.InspectionNo);
            Assert.Equal(purchaseInOrder.Id, createdReport.StockInOrderId);
            Assert.Equal(purchaseInOrder.InNo, createdReport.InNo);
            Assert.Equal(managedWareId, createdReport.WareId);
            Assert.Equal(managedWareName, createdReport.WareName);
            Assert.Equal(managedSupplierId, createdReport.SupplierId);
            Assert.Equal(managedSupplierName, createdReport.SupplierName);
            Assert.Equal(InspectionConclusion.Qualified, createdReport.Conclusion);
            Assert.Equal(reportRemark, createdReport.Remark);
            var reportGoods = Assert.Single(createdReport.Goods);
            Assert.Equal(stockInDetailId, reportGoods.StockInDetailId);
            Assert.Equal(sampleQuantity, reportGoods.SampleQuantity);
            Assert.Equal(batchNo, reportGoods.BatchNo);
            Assert.Single(createdReport.Attachments);

            InspectionReportDto updatedReport;
            using (var updateReport = await adminClient.PutAsJsonAsync(
                       $"/api/traceability/inspection-reports/{createdReport.Id}",
                       BuildSaveInspectionReportPayload(
                           purchaseInOrder.Id,
                           stockInDetailId,
                           NumericPrecision.RoundQuantity(3m),
                           updatedReportRemark,
                           withAttachment: true)))
            {
                Assert.Equal(HttpStatusCode.OK, updateReport.StatusCode);
                updatedReport = await ReadApiDataAsync<InspectionReportDto>(updateReport);
            }

            Assert.Equal(updatedReportRemark, updatedReport.Remark);
            Assert.Equal(NumericPrecision.RoundQuantity(3m), Assert.Single(updatedReport.Goods).SampleQuantity);
            registry.Register<InspectionReport>(
                createdReport.Id,
                nameof(InspectionReport.Remark),
                updatedReportRemark);

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
                           remark = "T11溯源切片销售订单",
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

            IReadOnlyList<TraceRecordDto> firstTraces;
            using (var generate = await adminClient.PostAsync(
                       $"/api/traceability/traces/sale-orders/{saleOrder.Id}/generate",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, generate.StatusCode);
                firstTraces = await ReadApiDataAsync<IReadOnlyList<TraceRecordDto>>(generate);
            }

            var firstTrace = Assert.Single(firstTraces);
            generatedTraceNo = firstTrace.TraceNo;
            Assert.StartsWith("TR", firstTrace.TraceNo);
            Assert.Equal(saleOrder.OrderNo, firstTrace.SaleOrderNo);
            Assert.Equal(batchNo, firstTrace.BatchNo);
            Assert.Equal(managedSupplierName, firstTrace.SupplierName);
            Assert.Equal(createdReport.Id, firstTrace.InspectionReportId);

            await using (var stampTrace = fixture.CreateDbContext())
            {
                var tracked = await stampTrace.TraceRecords
                    .SingleAsync(item => item.TraceNo == firstTrace.TraceNo);
                tracked.Remark = traceRemark;
                await stampTrace.SaveChangesAsync();
                traceRecordId = tracked.Id;
                registry.Register<TraceRecord>(tracked.Id, nameof(TraceRecord.Remark), traceRemark);
            }

            IReadOnlyList<TraceRecordDto> secondTraces;
            using (var generateAgain = await adminClient.PostAsync(
                       $"/api/traceability/traces/sale-orders/{saleOrder.Id}/generate",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, generateAgain.StatusCode);
                secondTraces = await ReadApiDataAsync<IReadOnlyList<TraceRecordDto>>(generateAgain);
            }

            var secondTrace = Assert.Single(secondTraces);
            Assert.Equal(firstTrace.TraceNo, secondTrace.TraceNo);
            Assert.Equal(firstTrace.Id, secondTrace.Id);

            await using (var afterGenerate = fixture.CreateDbContext())
            {
                Assert.Equal(1, await afterGenerate.TraceRecords.CountAsync(item =>
                    item.SaleOrderId == saleOrder.Id));
            }

            using (var rejectUpdate = await adminClient.PutAsJsonAsync(
                       $"/api/traceability/inspection-reports/{createdReport.Id}",
                       BuildSaveInspectionReportPayload(
                           purchaseInOrder.Id,
                           stockInDetailId,
                           sampleQuantity,
                           $"{batch.Id}-冻结后修改")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectUpdate, ResponseCode.DatabaseError);
            }

            using (var rejectDelete = await adminClient.DeleteAsync(
                       $"/api/traceability/inspection-reports/{createdReport.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(rejectDelete, ResponseCode.DatabaseError);
            }

            await using (var afterFreeze = fixture.CreateDbContext())
            {
                var frozen = await afterFreeze.InspectionReports.AsNoTracking()
                    .SingleAsync(item => item.Id == createdReport.Id);
                Assert.Equal(updatedReportRemark, frozen.Remark);
                Assert.Equal(1, await afterFreeze.TraceRecords.CountAsync(item =>
                    item.InspectionReportId == createdReport.Id));
            }

            using (var qrResponse = await anonymousClient.GetAsync(
                       $"/api/traceability/traces/qr/{Uri.EscapeDataString(generatedTraceNo!)}"))
            {
                Assert.Equal(HttpStatusCode.OK, qrResponse.StatusCode);
                var raw = await qrResponse.Content.ReadAsStringAsync();
                Assert.DoesNotContain("customerName", raw, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("saleOrderId", raw, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("stockInOrderId", raw, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("\"id\"", raw, StringComparison.OrdinalIgnoreCase);

                await using var qrStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(raw));
                var qrPayload = await JsonSerializer.DeserializeAsync<ApiResponse<TraceQrCodeDto>>(qrStream, JsonOptions);
                Assert.NotNull(qrPayload);
                Assert.Equal(ResponseCode.Success, qrPayload!.Code);
                var qr = qrPayload.Data!;
                Assert.Equal(generatedTraceNo, qr.TraceRecord.TraceNo);
                Assert.Equal(managedGoodsName, qr.TraceRecord.GoodsName);
                Assert.Equal(managedGoodsCode, qr.TraceRecord.GoodsCode);
                Assert.Equal(managedSupplierName, qr.TraceRecord.SupplierName);
                Assert.Equal(managedWareName, qr.TraceRecord.WareName);
                Assert.Equal(batchNo, qr.TraceRecord.BatchNo);
                Assert.NotNull(qr.InspectionReport);
                Assert.Equal(createdReport.InspectionNo, qr.InspectionReport!.InspectionNo);
                Assert.Equal(InspectionConclusion.Qualified, qr.InspectionReport.Conclusion);
            }

            await using (var seedPush = fixture.CreateDbContext())
            {
                await seedPush.ExternalPushLogs.AddAsync(new ExternalPushLog
                {
                    Id = pushLogId,
                    BusinessType = ExternalPushBusinessType.InspectionReport,
                    BusinessId = createdReport.Id,
                    BusinessNoSnapshot = createdReport.InspectionNo,
                    PlatformCode = pushPlatformCode,
                    PushStatus = ExternalPushStatus.Success,
                    PushTime = DateTime.UtcNow,
                    ResponseTime = DateTime.UtcNow,
                    RequestContent = "{\"masked\":true}",
                    ResponseContent = "{\"ok\":true}",
                    RetryCount = 0,
                    CreateName = pushCreateName,
                    CreateTime = DateTime.UtcNow
                });
                await seedPush.SaveChangesAsync();
                registry.Register<ExternalPushLog>(pushLogId, nameof(ExternalPushLog.CreateName), pushCreateName);
            }

            using (var pushLogs = await adminClient.GetAsync(
                       $"/api/traceability/push-logs?current=1&size=20&keyword={Uri.EscapeDataString(pushPlatformCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, pushLogs.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<ExternalPushLogDto>>(pushLogs);
                var matched = Assert.Single(page.Records!, item => item.Id == pushLogId);
                Assert.Equal(ExternalPushBusinessType.InspectionReport, matched.BusinessType);
                Assert.Equal(createdReport.Id, matched.BusinessId);
                Assert.Equal(createdReport.InspectionNo, matched.BusinessNo);
                Assert.Equal(ExternalPushStatus.Success, matched.PushStatus);
                Assert.Equal(pushPlatformCode, matched.PlatformCode);
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
                Assert.Contains(traceReadPermission, info.Buttons);
                Assert.DoesNotContain(traceCreatePermission, info.Buttons);
                Assert.DoesNotContain(traceUpdatePermission, info.Buttons);
                Assert.DoesNotContain(traceDeletePermission, info.Buttons);
            }

            using (var allowedReports = await limitedClient.GetAsync(
                       $"/api/traceability/inspection-reports?current=1&size=20&keyword={Uri.EscapeDataString(createdReport.InspectionNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedReports.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<InspectionReportDto>>(allowedReports);
                Assert.Contains(page.Records!, item => item.Id == createdReport.Id);
            }

            using (var allowedTraces = await limitedClient.GetAsync(
                       $"/api/traceability/traces?current=1&size=20&keyword={Uri.EscapeDataString(generatedTraceNo!)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedTraces.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<TraceRecordDto>>(allowedTraces);
                Assert.Contains(page.Records!, item => item.TraceNo == generatedTraceNo);
            }

            using (var allowedPush = await limitedClient.GetAsync(
                       $"/api/traceability/push-logs?current=1&size=20&keyword={Uri.EscapeDataString(pushPlatformCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedPush.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<ExternalPushLogDto>>(allowedPush);
                Assert.Contains(page.Records!, item => item.Id == pushLogId);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/traceability/inspection-reports",
                       BuildSaveInspectionReportPayload(
                           purchaseInOrder.Id,
                           stockInDetailId,
                           sampleQuantity,
                           $"{batch.Id}-越权创建")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       $"/api/traceability/inspection-reports/{createdReport.Id}",
                       BuildSaveInspectionReportPayload(
                           purchaseInOrder.Id,
                           stockInDetailId,
                           sampleQuantity,
                           $"{batch.Id}-越权更新")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdate, ResponseCode.Forbidden);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync(
                       $"/api/traceability/inspection-reports/{createdReport.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDelete, ResponseCode.Forbidden);
            }

            using (var deniedGenerate = await limitedClient.PostAsync(
                       $"/api/traceability/traces/sale-orders/{saleOrder.Id}/generate",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedGenerate, ResponseCode.Forbidden);
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
                var residualPushLogs = await cleanupContext.ExternalPushLogs
                    .Where(item => item.Id == pushLogId
                                   || item.CreateName == pushCreateName
                                   || item.PlatformCode == pushPlatformCode
                                   || (item.CreateName != null && item.CreateName.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualPushLogs.Count > 0)
                {
                    cleanupContext.ExternalPushLogs.RemoveRange(residualPushLogs);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualTraceIds = new List<Guid>();
                if (traceRecordId.HasValue)
                    residualTraceIds.Add(traceRecordId.Value);
                var residualTraces = await cleanupContext.TraceRecords
                    .Where(item => residualTraceIds.Contains(item.Id)
                                   || item.Remark == traceRemark
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id))
                                   || (saleOrderId.HasValue && item.SaleOrderId == saleOrderId.Value)
                                   || (generatedTraceNo != null && item.TraceNo == generatedTraceNo))
                    .ToListAsync();
                if (residualTraces.Count > 0)
                {
                    cleanupContext.TraceRecords.RemoveRange(residualTraces);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualReportIds = new List<Guid>();
                if (inspectionReportId.HasValue)
                    residualReportIds.Add(inspectionReportId.Value);
                var residualReports = await cleanupContext.InspectionReports
                    .Where(item => residualReportIds.Contains(item.Id)
                                   || item.Remark == reportRemark
                                   || item.Remark == updatedReportRemark
                                   || item.Remark == disposableReportRemark
                                   || (item.Remark != null && item.Remark.StartsWith(batch.Id))
                                   || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value))
                    .ToListAsync();
                if (residualReports.Count > 0)
                {
                    cleanupContext.InspectionReports.RemoveRange(residualReports);
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
                if (purchaseInOrderId.HasValue)
                    residualStockInIds.Add(purchaseInOrderId.Value);
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => residualStockInIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (item.Remark == purchaseInRemark || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();

                var residualOrderIdSet = residualStockOutOrders.Select(item => item.Id)
                    .Concat(residualStockInOrders.Select(item => item.Id))
                    .ToHashSet();

                var residualBillIds = new List<Guid>();
                if (inboundBillId.HasValue)
                    residualBillIds.Add(inboundBillId.Value);
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

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.ExternalPushLogs.AnyAsync(item =>
                item.Id == pushLogId
                || item.PlatformCode == pushPlatformCode
                || (item.CreateName != null && item.CreateName.StartsWith(batch.Id))));
            Assert.False(await residualContext.TraceRecords.AnyAsync(item =>
                (item.Remark != null && item.Remark.StartsWith(batch.Id))
                || (generatedTraceNo != null && item.TraceNo == generatedTraceNo)
                || (saleOrderId.HasValue && item.SaleOrderId == saleOrderId.Value)));
            Assert.False(await residualContext.InspectionReports.AnyAsync(item =>
                item.Remark != null && item.Remark.StartsWith(batch.Id)));
            Assert.False(await residualContext.SupplierBills.AnyAsync(item =>
                (inboundBillId.HasValue && item.Id == inboundBillId.Value)
                || (purchaseInOrderId.HasValue && item.StockInOrderId == purchaseInOrderId.Value)));
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
            Assert.True(await residualContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await residualContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
        }
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
                    remark = "T11采购入库明细"
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
                    remark = "T11销售出库明细"
                }
            }
        };
    }

    private static object BuildSaveInspectionReportPayload(
        Guid stockInOrderId,
        Guid stockInDetailId,
        decimal sampleQuantity,
        string remark,
        bool withAttachment = false)
    {
        return new
        {
            stockInOrderId,
            inspectionOrg = "浙北农产品检测中心",
            sampleTime = "2026-07-20T07:30:00Z",
            inspectTime = "2026-07-20T08:30:00Z",
            conclusion = (int)InspectionConclusion.Qualified,
            remark,
            goods = new[]
            {
                new
                {
                    stockInDetailId,
                    sampleQuantity,
                    conclusion = (int)InspectionConclusion.Qualified
                }
            },
            attachments = withAttachment
                ?
                [
                    new
                    {
                        attachmentType = (int)InspectionAttachmentType.Report,
                        fileName = "t11-inspection.pdf",
                        fileUrl = "/files/t11-inspection.pdf",
                        fileSize = 2048L,
                        sort = 0
                    }
                ]
                : Array.Empty<object>()
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
