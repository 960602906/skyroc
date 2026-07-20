using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Delivery;
using Application.DTOs.Orders;
using Application.DTOs.Reports;
using Application.DTOs.Storage;
using Domain.Entities;
using Domain.Entities.AfterSales;
using Domain.Entities.Delivery;
using Domain.Entities.Finance;
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

namespace SkyRoc.Tests.Reports;

/// <summary>
///     T11 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证销售多维报表与驾驶舱
///     已签收验收口径、独立期望值核对、未签收排除与 401/403 权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class SalesReportAndDashboardPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     其他入库→销售订单审核→销售出库→配送签收→销售四维汇总与驾驶舱口径独立重算→
    ///     未签收排除→账单对账与取货状态独立核对→401/403 权限矩阵；临时批次精确清理。
    /// </summary>
    [Fact]
    public async Task SalesReport_DashboardCaliberIndependentExpectationAndPermissionMatrix_OnPostgreSql()
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
        var saleOutRemark = $"{batch.Id}-销售出库报表";
        var signRemark = $"{batch.Id}-签收生报表";
        var password = "SkyRocT11SalesDash!2026";
        var userAgent = $"SkyRoc-T11-SalesDashboard/{batch.Id}";
        var createName = "T11-SalesDashboard";
        var contactName = "报表联调李老师";
        var contactPhone = "13900001121";
        var deliveryAddress = $"杭州市滨江区报表联调路 {batch.Id} 号食堂";
        var areaKeyword = batch.Id;

        // 联调销售订单集中在 2026-07/08/09，选用 3 月隔离驾驶舱日期窗口。
        var orderDate = new DateTime(2026, 3, 18, 8, 0, 0, DateTimeKind.Utc);
        var receiveDate = new DateTime(2026, 3, 19, 6, 0, 0, DateTimeKind.Utc);
        var reportDateStart = new DateTime(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc);
        var reportDateEnd = new DateTime(2026, 3, 18, 23, 59, 59, DateTimeKind.Utc);
        var expectedReportDate = DateOnly.FromDateTime(orderDate);

        var reportsReadPermission = PermissionCodes.Business.Reports.Read;
        var financeReadPermission = PermissionCodes.Business.Finance.Read;

        var inboundQuantity = NumericPrecision.RoundQuantity(12m);
        var inboundUnitPrice = NumericPrecision.RoundMoney(5m);
        var saleQuantity = NumericPrecision.RoundQuantity(7.5m);
        var saleUnitPrice = NumericPrecision.RoundMoney(8.4m);
        var expectedSaleAmount = NumericPrecision.RoundMoney(saleQuantity * saleUnitPrice);

        Guid adminRoleId;
        Guid managedCustomerId;
        string managedCustomerName = null!;
        string managedCustomerCode = null!;
        Guid managedGoodsId;
        string managedGoodsName = null!;
        string managedGoodsCode = null!;
        string managedGoodsTypeName = null!;
        string managedBaseUnitName = null!;
        Guid managedGoodsUnitId;
        Guid managedWareId;
        Guid managedDriverId;

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

            var customer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == customerCode);
            Assert.NotNull(customer);
            managedCustomerId = customer.Id;
            managedCustomerName = customer.Name;
            managedCustomerCode = customer.Code!;

            var goods = await seedContext.Set<GoodsEntity>()
                .Include(item => item.GoodsType)
                .Include(item => item.BaseUnit)
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsCode);
            Assert.NotNull(goods);
            managedGoodsId = goods.Id;
            managedGoodsName = goods.Name;
            managedGoodsCode = goods.Code!;
            managedGoodsTypeName = goods.GoodsType?.Name ?? "未分类";
            managedBaseUnitName = goods.BaseUnit?.Name ?? string.Empty;

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
            managedDriverId = driver.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T11 报表驾驶舱相邻权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T11销售报表操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001131",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T11财务只读无报表权限",
                    Gender = GenderType.Female,
                    Phone = "13900001132",
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
                Title = "T11财务只读菜单",
                Component = "page.t11.sales.dashboard.seed",
                MenuType = MenuType.Menu,
                Order = 9111,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedReadButtonId,
                Code = financeReadPermission,
                Desc = "T11 财务读取权限按钮（故意不含报表）",
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
        Guid? saleOutOrderId = null;
        Guid? deliveryTaskId = null;
        Guid? receiptId = null;
        Guid? customerBillId = null;
        Guid? createdBatchId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousSales = await anonymousClient.GetAsync(
                       BuildSalesGoodsUrl(reportDateStart, reportDateEnd, managedCustomerId, areaKeyword)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousSales, ResponseCode.Unauthorized);
            }

            using (var anonymousDashboard = await anonymousClient.GetAsync(
                       BuildDashboardBriefUrl(reportDateStart, reportDateEnd)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousDashboard, ResponseCode.Unauthorized);
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
                           orderDate = orderDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                           receiveDate = receiveDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                           contactName,
                           contactPhone,
                           deliveryAddress,
                           remark = "T11销售报表驾驶舱切片销售订单",
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

            var saleOrderDetailId = Assert.Single(saleOrder.Details).Id;

            using (var approveOrder = await adminClient.PostAsJsonAsync(
                       $"/api/orders/{saleOrder.Id}/approve",
                       new SaleOrderAuditDto { Remark = $"{saleOrderInnerRemark}-审核通过" }))
            {
                Assert.Equal(HttpStatusCode.OK, approveOrder.StatusCode);
                saleOrder = await ReadApiDataAsync<SaleOrderDto>(approveOrder);
            }

            using (var beforeSignSales = await adminClient.GetAsync(
                       BuildSalesGoodsUrl(reportDateStart, reportDateEnd, managedCustomerId, areaKeyword)))
            {
                Assert.Equal(HttpStatusCode.OK, beforeSignSales.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<SalesGoodsSummaryDto>>(beforeSignSales);
                Assert.Equal(0, page.Total);
                Assert.Empty(page.Records ?? []);
            }

            using (var beforeSignBrief = await adminClient.GetAsync(
                       BuildDashboardBriefUrl(reportDateStart, reportDateEnd)))
            {
                Assert.Equal(HttpStatusCode.OK, beforeSignBrief.StatusCode);
                var brief = await ReadApiDataAsync<DashboardBriefDto>(beforeSignBrief);
                Assert.Equal(0m, brief.SaleAmount);
                Assert.Equal(0, brief.OrderCount);
                Assert.Equal(0, brief.CustomerCount);
            }

            var (task, stockOutDetailId) = await CreateAuditedSaleOutAndTaskAsync(
                adminClient,
                registry,
                managedWareId,
                saleOrder.Id,
                managedCustomerId,
                createdBatchId!.Value,
                managedGoodsUnitId,
                saleOrderDetailId,
                saleQuantity,
                saleUnitPrice,
                saleOutRemark);
            deliveryTaskId = task.Id;
            saleOutOrderId = task.StockOutOrderId;

            using (var assign = await adminClient.PutAsJsonAsync(
                       "/api/delivery-tasks/driver",
                       new AssignDeliveryDriverDto
                       {
                           TaskIds = [task.Id],
                           DriverId = managedDriverId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
                var assigned = Assert.Single(await ReadApiDataAsync<List<DeliveryTaskDto>>(assign));
                Assert.Equal(DeliveryTaskStatus.Assigned, assigned.DeliveryStatus);
            }

            using (var start = await adminClient.PutAsync($"/api/delivery-tasks/{task.Id}/start", null))
            {
                Assert.Equal(HttpStatusCode.OK, start.StatusCode);
                var started = await ReadApiDataAsync<DeliveryTaskDto>(start);
                Assert.Equal(DeliveryTaskStatus.Delivering, started.DeliveryStatus);
            }

            var billWindowStart = DateTime.UtcNow.AddSeconds(-1);
            using (var sign = await adminClient.PutAsJsonAsync(
                       $"/api/delivery-tasks/{task.Id}/sign",
                       BuildSignPayload("报表李老师", stockOutDetailId, saleQuantity, signRemark)))
            {
                Assert.Equal(HttpStatusCode.OK, sign.StatusCode);
                var receipt = await ReadApiDataAsync<OrderReceiptDto>(sign);
                receiptId = receipt.Id;
                registry.Register<OrderReceipt>(receipt.Id, nameof(OrderReceipt.SignRemark), signRemark);
            }

            var billWindowEnd = DateTime.UtcNow.AddSeconds(1);

            await using (var afterSign = fixture.CreateDbContext())
            {
                var persistedOrder = await afterSign.SaleOrders
                    .Include(item => item.Details)
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == saleOrder.Id);
                Assert.Equal(SaleOrderStatus.Signed, persistedOrder.OrderStatus);

                var detail = Assert.Single(persistedOrder.Details);
                Assert.Equal(saleQuantity, detail.CustomerCheckBaseQuantity);
                Assert.Equal(expectedSaleAmount, detail.CustomerCheckPrice);

                var bill = await afterSign.CustomerBills.AsNoTracking()
                    .SingleAsync(item => item.SaleOrderId == saleOrder.Id);
                customerBillId = bill.Id;
                Assert.Equal(expectedSaleAmount, bill.ReceivableAmount);
                Assert.Equal(0m, bill.SettledAmount);
                Assert.True(bill.BillDate >= billWindowStart && bill.BillDate <= billWindowEnd);
            }

            using (var goodsSummary = await adminClient.GetAsync(
                       BuildSalesGoodsUrl(reportDateStart, reportDateEnd, managedCustomerId, areaKeyword)))
            {
                Assert.Equal(HttpStatusCode.OK, goodsSummary.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<SalesGoodsSummaryDto>>(goodsSummary);
                Assert.Equal(1, page.Total);
                var item = Assert.Single(page.Records!);
                Assert.Equal(managedGoodsId, item.GoodsId);
                Assert.Equal(managedGoodsName, item.GoodsName);
                Assert.Equal(managedGoodsCode, item.GoodsCode);
                Assert.Equal(managedGoodsTypeName, item.GoodsTypeName);
                Assert.Equal(managedBaseUnitName, item.BaseUnitName);
                Assert.Equal(saleQuantity, item.SaleBaseQuantity);
                Assert.Equal(expectedSaleAmount, item.SaleAmount);
                Assert.Equal(1, item.OrderCount);
                Assert.Equal(1, item.CustomerCount);
            }

            using (var categorySummary = await adminClient.GetAsync(
                       $"/api/reports/sales/categories?current=1&size=20&dateStart={Uri.EscapeDataString(reportDateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(reportDateEnd.ToString("O"))}&customerId={managedCustomerId}&areaKeyword={Uri.EscapeDataString(areaKeyword)}"))
            {
                Assert.Equal(HttpStatusCode.OK, categorySummary.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<SalesCategorySummaryDto>>(categorySummary);
                var item = Assert.Single(page.Records!);
                Assert.Equal(managedGoodsTypeName, item.GoodsTypeName);
                Assert.Equal(saleQuantity, item.SaleBaseQuantity);
                Assert.Equal(expectedSaleAmount, item.SaleAmount);
                Assert.Equal(1, item.OrderCount);
                Assert.Equal(1, item.CustomerCount);
            }

            using (var customerSummary = await adminClient.GetAsync(
                       $"/api/reports/sales/customers?current=1&size=20&dateStart={Uri.EscapeDataString(reportDateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(reportDateEnd.ToString("O"))}&customerId={managedCustomerId}&areaKeyword={Uri.EscapeDataString(areaKeyword)}"))
            {
                Assert.Equal(HttpStatusCode.OK, customerSummary.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<SalesCustomerSummaryDto>>(customerSummary);
                var item = Assert.Single(page.Records!);
                Assert.Equal(managedCustomerId, item.CustomerId);
                Assert.Equal(managedCustomerName, item.CustomerName);
                Assert.Equal(managedCustomerCode, item.CustomerCode);
                Assert.Equal(saleQuantity, item.SaleBaseQuantity);
                Assert.Equal(expectedSaleAmount, item.SaleAmount);
                Assert.Equal(1, item.OrderCount);
                Assert.Equal(1, item.GoodsCount);
            }

            using (var areaSummary = await adminClient.GetAsync(
                       $"/api/reports/sales/areas?current=1&size=20&dateStart={Uri.EscapeDataString(reportDateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(reportDateEnd.ToString("O"))}&customerId={managedCustomerId}&areaKeyword={Uri.EscapeDataString(areaKeyword)}"))
            {
                Assert.Equal(HttpStatusCode.OK, areaSummary.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<SalesAreaSummaryDto>>(areaSummary);
                var item = Assert.Single(page.Records!);
                Assert.Equal(deliveryAddress, item.AreaName);
                Assert.Equal(saleQuantity, item.SaleBaseQuantity);
                Assert.Equal(expectedSaleAmount, item.SaleAmount);
                Assert.Equal(1, item.OrderCount);
                Assert.Equal(1, item.CustomerCount);
            }

            using (var brief = await adminClient.GetAsync(
                       BuildDashboardBriefUrl(reportDateStart, reportDateEnd)))
            {
                Assert.Equal(HttpStatusCode.OK, brief.StatusCode);
                var data = await ReadApiDataAsync<DashboardBriefDto>(brief);
                Assert.Equal(expectedSaleAmount, data.SaleAmount);
                Assert.Equal(1, data.OrderCount);
                Assert.Equal(1, data.CustomerCount);
            }

            using (var trend = await adminClient.GetAsync(
                       $"/api/dashboard/sales-trend?dateStart={Uri.EscapeDataString(reportDateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(reportDateEnd.ToString("O"))}"))
            {
                Assert.Equal(HttpStatusCode.OK, trend.StatusCode);
                var rows = await ReadApiDataAsync<List<DashboardSalesTrendDto>>(trend);
                var item = Assert.Single(rows);
                Assert.Equal(expectedReportDate, item.ReportDate);
                Assert.Equal(expectedSaleAmount, item.SaleAmount);
                Assert.Equal(1, item.OrderCount);
                Assert.Equal(1, item.CustomerCount);
            }

            using (var customerRank = await adminClient.GetAsync(
                       $"/api/dashboard/customer-sales-rank?dateStart={Uri.EscapeDataString(reportDateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(reportDateEnd.ToString("O"))}&rankSize=5"))
            {
                Assert.Equal(HttpStatusCode.OK, customerRank.StatusCode);
                var rows = await ReadApiDataAsync<List<DashboardCustomerSalesRankDto>>(customerRank);
                var item = Assert.Single(rows);
                Assert.Equal(managedCustomerId, item.CustomerId);
                Assert.Equal(managedCustomerName, item.CustomerName);
                Assert.Equal(expectedSaleAmount, item.SaleAmount);
                Assert.Equal(1, item.OrderCount);
            }

            using (var goodsTypeRank = await adminClient.GetAsync(
                       $"/api/dashboard/goods-type-sales-rank?dateStart={Uri.EscapeDataString(reportDateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(reportDateEnd.ToString("O"))}&rankSize=5"))
            {
                Assert.Equal(HttpStatusCode.OK, goodsTypeRank.StatusCode);
                var rows = await ReadApiDataAsync<List<DashboardGoodsTypeSalesRankDto>>(goodsTypeRank);
                var item = Assert.Single(rows);
                Assert.Equal(managedGoodsTypeName, item.GoodsTypeName);
                Assert.Equal(expectedSaleAmount, item.SaleAmount);
                Assert.Equal(1, item.OrderCount);
            }

            await using (var expectContext = fixture.CreateDbContext())
            {
                var expectedBills = await expectContext.CustomerBills.AsNoTracking()
                    .Where(item => item.BillDate >= billWindowStart && item.BillDate <= billWindowEnd)
                    .ToListAsync();
                Assert.Contains(expectedBills, item => item.Id == customerBillId);

                var expectedReceivable = NumericPrecision.RoundMoney(expectedBills.Sum(item => item.ReceivableAmount));
                var expectedSettled = NumericPrecision.RoundMoney(expectedBills.Sum(item => item.SettledAmount));
                var expectedPending = NumericPrecision.RoundMoney(
                    expectedBills.Sum(item => item.ReceivableAmount > item.SettledAmount
                        ? item.ReceivableAmount - item.SettledAmount
                        : 0m));

                using (var reconciliation = await adminClient.GetAsync(
                           $"/api/dashboard/reconciliation?dateStart={Uri.EscapeDataString(billWindowStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(billWindowEnd.ToString("O"))}"))
                {
                    Assert.Equal(HttpStatusCode.OK, reconciliation.StatusCode);
                    var data = await ReadApiDataAsync<DashboardReconciliationDto>(reconciliation);
                    Assert.Equal(expectedReceivable, data.ReceivableAmount);
                    Assert.Equal(expectedSettled, data.SettledAmount);
                    Assert.Equal(expectedPending, data.PendingAmount);
                    Assert.Equal(expectedBills.Count, data.BillCount);
                }

                var pickupWindowStart = billWindowStart;
                var pickupWindowEnd = billWindowEnd;
                var expectedPickupCounts = await expectContext.PickupTasks.AsNoTracking()
                    .Where(item => item.CreateTime.HasValue
                                   && item.CreateTime.Value >= pickupWindowStart
                                   && item.CreateTime.Value <= pickupWindowEnd)
                    .GroupBy(item => item.PickupStatus)
                    .Select(group => new { Status = group.Key, Count = group.Count() })
                    .ToDictionaryAsync(item => item.Status, item => item.Count);

                using (var pickupStatuses = await adminClient.GetAsync(
                           $"/api/dashboard/pickup-statuses?dateStart={Uri.EscapeDataString(pickupWindowStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(pickupWindowEnd.ToString("O"))}"))
                {
                    Assert.Equal(HttpStatusCode.OK, pickupStatuses.StatusCode);
                    var rows = await ReadApiDataAsync<List<DashboardPickupStatusDto>>(pickupStatuses);
                    Assert.Equal(Enum.GetValues<PickupTaskStatus>().Length, rows.Count);
                    foreach (PickupTaskStatus status in Enum.GetValues<PickupTaskStatus>())
                    {
                        var expectedCount = expectedPickupCounts.GetValueOrDefault(status);
                        Assert.Equal(
                            expectedCount,
                            Assert.Single(rows, item => item.PickupStatus == status).TaskCount);
                    }
                }
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
                Assert.DoesNotContain(reportsReadPermission, info.Buttons);
            }

            using (var deniedSales = await limitedClient.GetAsync(
                       BuildSalesGoodsUrl(reportDateStart, reportDateEnd, managedCustomerId, areaKeyword)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedSales, ResponseCode.Forbidden);
            }

            using (var deniedDashboard = await limitedClient.GetAsync(
                       BuildDashboardBriefUrl(reportDateStart, reportDateEnd)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDashboard, ResponseCode.Forbidden);
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
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualSaleOrderIds = new List<Guid>();
                if (saleOrderId.HasValue)
                    residualSaleOrderIds.Add(saleOrderId.Value);

                var residualBillIds = new List<Guid>();
                if (customerBillId.HasValue)
                    residualBillIds.Add(customerBillId.Value);

                var residualBills = await cleanupContext.CustomerBills
                    .Where(item => residualBillIds.Contains(item.Id)
                                   || residualSaleOrderIds.Contains(item.SaleOrderId))
                    .ToListAsync();
                if (residualBills.Count > 0)
                {
                    cleanupContext.CustomerBills.RemoveRange(residualBills);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualReceiptIds = new List<Guid>();
                if (receiptId.HasValue)
                    residualReceiptIds.Add(receiptId.Value);
                var residualReceipts = await cleanupContext.OrderReceipts
                    .Where(item => residualReceiptIds.Contains(item.Id)
                                   || (item.SignRemark != null
                                       && (item.SignRemark == signRemark
                                           || item.SignRemark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualReceipts.Count > 0)
                {
                    cleanupContext.OrderReceipts.RemoveRange(residualReceipts);
                    await cleanupContext.SaveChangesAsync();
                }

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

                var residualStockOutOrders = await cleanupContext.StockOutOrders
                    .Where(item => (saleOutOrderId.HasValue && item.Id == saleOutOrderId.Value)
                                   || (item.Remark != null
                                       && (item.Remark == saleOutRemark || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();
                var residualStockInOrders = await cleanupContext.StockInOrders
                    .Where(item => (inboundOrderId.HasValue && item.Id == inboundOrderId.Value)
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
                    .Where(relation => relation.RoleId == limitedRoleId || relation.MenuId == seedMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoles = await cleanupContext.Roles
                    .Where(role => role.Id == limitedRoleId || role.Code == limitedRoleCode)
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
            Assert.False(await residualContext.CustomerBills.AnyAsync(item =>
                (customerBillId.HasValue && item.Id == customerBillId.Value)
                || (saleOrderId.HasValue && item.SaleOrderId == saleOrderId.Value)));
            Assert.False(await residualContext.OrderReceipts.AnyAsync(item =>
                item.SignRemark != null && item.SignRemark.StartsWith(batch.Id)));
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
            Assert.True(await residualContext.Drivers.AnyAsync(item => item.Id == managedDriverId));
        }
    }

    private static string BuildSalesGoodsUrl(
        DateTime dateStart,
        DateTime dateEnd,
        Guid customerId,
        string areaKeyword)
    {
        return $"/api/reports/sales/goods?current=1&size=20&dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}&customerId={customerId}&areaKeyword={Uri.EscapeDataString(areaKeyword)}";
    }

    private static string BuildDashboardBriefUrl(DateTime dateStart, DateTime dateEnd)
    {
        return $"/api/dashboard/brief?dateStart={Uri.EscapeDataString(dateStart.ToString("O"))}&dateEnd={Uri.EscapeDataString(dateEnd.ToString("O"))}";
    }

    private static SignDeliveryTaskDto BuildSignPayload(
        string signerName,
        Guid stockOutDetailId,
        decimal acceptedBaseQuantity,
        string remark)
    {
        return new SignDeliveryTaskDto
        {
            SignerName = signerName,
            Remark = remark,
            Details =
            [
                new SignDeliveryCheckDetailDto
                {
                    StockOutDetailId = stockOutDetailId,
                    AcceptedBaseQuantity = acceptedBaseQuantity,
                    CheckStatus = OrderCustomerCheckStatus.Accepted
                }
            ]
        };
    }

    private static async Task<(DeliveryTaskDto Task, Guid StockOutDetailId)> CreateAuditedSaleOutAndTaskAsync(
        HttpClient adminClient,
        BatchCleanupRegistry registry,
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
        StockOutOrderDto saleOutOrder;
        using (var createSaleOut = await adminClient.PostAsJsonAsync(
                   "/api/stock-out/sale",
                   BuildCreateSaleOutPayload(
                       wareId,
                       saleOrderId,
                       customerId,
                       stockBatchId,
                       goodsUnitId,
                       saleOrderDetailId,
                       quantity,
                       unitPrice,
                       remark)))
        {
            Assert.Equal(HttpStatusCode.OK, createSaleOut.StatusCode);
            saleOutOrder = await ReadApiDataAsync<StockOutOrderDto>(createSaleOut);
            registry.Register<StockOutOrder>(saleOutOrder.Id, nameof(StockOutOrder.Remark), remark);
        }

        using (var auditSaleOut = await adminClient.PostAsJsonAsync(
                   $"/api/stock-out/sale/{saleOutOrder.Id}/audit",
                   new StockOutAuditDto { Remark = $"{remark}-审核" }))
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
            registry.Register<DeliveryTask>(task.Id, nameof(DeliveryTask.Remark), remark);
        }

        var detailId = Assert.Single(saleOutOrder.Details).Id;
        return (task, detailId);
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
            inTime = "2026-03-18T09:00:00Z",
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
                    remark = "T11其他入库明细"
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
            outTime = "2026-03-18T10:00:00Z",
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
