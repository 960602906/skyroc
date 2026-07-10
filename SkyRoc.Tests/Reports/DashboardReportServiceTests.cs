using Application.QueryParameters.Reports;
using Application.Services;
using Domain.Entities.AfterSales;
using Domain.Entities.Finance;
using Domain.Entities.Orders;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SkyRoc.Tests.Reports;

/// <summary>
/// 首页驾驶舱统计口径回归测试。
/// </summary>
public class DashboardReportServiceTests
{
    [Fact]
    public async Task DashboardQueries_UseSignedSalesBillsAndPickupCreationPeriod()
    {
        await using var context = CreateDbContext();
        await SeedAsync(context);
        var service = new ReportService(new ReportRepository(context));
        var parameters = new DashboardQueryParameters
        {
            DateStart = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            DateEnd = new DateTime(2026, 7, 11, 23, 59, 59, DateTimeKind.Utc),
            RankSize = 1
        };

        var brief = await service.GetDashboardBriefAsync(parameters);
        var trend = await service.GetDashboardSalesTrendAsync(parameters);
        var customerRank = await service.GetDashboardCustomerSalesRankAsync(parameters);
        var goodsTypeRank = await service.GetDashboardGoodsTypeSalesRankAsync(parameters);
        var reconciliation = await service.GetDashboardReconciliationAsync(parameters);
        var pickupStatuses = await service.GetDashboardPickupStatusesAsync(parameters);

        Assert.Equal(150m, brief.SaleAmount);
        Assert.Equal(2, brief.OrderCount);
        Assert.Equal(2, brief.CustomerCount);

        Assert.Collection(
            trend,
            first =>
            {
                Assert.Equal(new DateOnly(2026, 7, 10), first.ReportDate);
                Assert.Equal(100m, first.SaleAmount);
                Assert.Equal(1, first.OrderCount);
            },
            second =>
            {
                Assert.Equal(new DateOnly(2026, 7, 11), second.ReportDate);
                Assert.Equal(50m, second.SaleAmount);
                Assert.Equal(1, second.OrderCount);
            });

        var customer = Assert.Single(customerRank);
        Assert.Equal("第一学校", customer.CustomerName);
        Assert.Equal(100m, customer.SaleAmount);
        Assert.Equal(1, customer.OrderCount);

        var goodsType = Assert.Single(goodsTypeRank);
        Assert.Equal("蔬菜", goodsType.GoodsTypeName);
        Assert.Equal(100m, goodsType.SaleAmount);

        Assert.Equal(150m, reconciliation.ReceivableAmount);
        Assert.Equal(90m, reconciliation.SettledAmount);
        Assert.Equal(60m, reconciliation.PendingAmount);
        Assert.Equal(2, reconciliation.BillCount);

        Assert.Equal(Enum.GetValues<PickupTaskStatus>().Length, pickupStatuses.Count);
        Assert.Equal(2, Assert.Single(pickupStatuses, x => x.PickupStatus == PickupTaskStatus.PendingAssign).TaskCount);
        Assert.Equal(0, Assert.Single(pickupStatuses, x => x.PickupStatus == PickupTaskStatus.PendingPickup).TaskCount);
        Assert.Equal(0, Assert.Single(pickupStatuses, x => x.PickupStatus == PickupTaskStatus.PickingUp).TaskCount);
        Assert.Equal(1, Assert.Single(pickupStatuses, x => x.PickupStatus == PickupTaskStatus.Completed).TaskCount);
        Assert.Equal(0, Assert.Single(pickupStatuses, x => x.PickupStatus == PickupTaskStatus.Cancelled).TaskCount);
    }

    [Fact]
    public async Task DashboardSales_UsesCustomerCheckAmount_WithoutFallingBackToOrderTotal()
    {
        await using var context = CreateDbContext();
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var unitId = Guid.NewGuid();

        await context.SaleOrders.AddAsync(
            CreateOrder(orderId, customerId, "验收学校", "SO-CHECK-001", new DateTime(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc), SaleOrderStatus.Signed));
        await context.SaleOrderDetails.AddRangeAsync(
            CreateDetail(orderId, unitId, "蔬菜", acceptedAmount: 80m, orderTotal: 999m),
            CreateDetail(orderId, unitId, "水果", acceptedAmount: null, orderTotal: 500m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new ReportService(new ReportRepository(context));
        var parameters = new DashboardQueryParameters
        {
            DateStart = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            DateEnd = new DateTime(2026, 7, 10, 23, 59, 59, DateTimeKind.Utc)
        };

        var brief = await service.GetDashboardBriefAsync(parameters);
        var trend = await service.GetDashboardSalesTrendAsync(parameters);

        Assert.Equal(80m, brief.SaleAmount);
        Assert.Equal(80m, Assert.Single(trend).SaleAmount);
    }

    [Fact]
    public async Task DashboardRanks_SortByAmountThenName_AndClampRankSize()
    {
        await using var context = CreateDbContext();
        var unitId = Guid.NewGuid();
        var alphaCustomerId = Guid.NewGuid();
        var betaCustomerId = Guid.NewGuid();
        var gammaCustomerId = Guid.NewGuid();
        var alphaOrderId = Guid.NewGuid();
        var betaOrderId = Guid.NewGuid();
        var gammaOrderId = Guid.NewGuid();
        var orderDate = new DateTime(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc);

        await context.SaleOrders.AddRangeAsync(
            CreateOrder(alphaOrderId, alphaCustomerId, "乙客户", "SO-RANK-A", orderDate, SaleOrderStatus.Signed),
            CreateOrder(betaOrderId, betaCustomerId, "甲客户", "SO-RANK-B", orderDate, SaleOrderStatus.Signed),
            CreateOrder(gammaOrderId, gammaCustomerId, "丙客户", "SO-RANK-C", orderDate, SaleOrderStatus.Signed));
        await context.SaleOrderDetails.AddRangeAsync(
            CreateDetail(alphaOrderId, unitId, "乙分类", 100m),
            CreateDetail(betaOrderId, unitId, "甲分类", 100m),
            CreateDetail(gammaOrderId, unitId, "丙分类", 50m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new ReportService(new ReportRepository(context));
        var tieBreakParameters = new DashboardQueryParameters
        {
            DateStart = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            DateEnd = new DateTime(2026, 7, 10, 23, 59, 59, DateTimeKind.Utc),
            RankSize = 10
        };

        var customerRank = await service.GetDashboardCustomerSalesRankAsync(tieBreakParameters);
        var goodsTypeRank = await service.GetDashboardGoodsTypeSalesRankAsync(tieBreakParameters);

        Assert.Collection(
            customerRank,
            first =>
            {
                Assert.Equal("甲客户", first.CustomerName);
                Assert.Equal(100m, first.SaleAmount);
            },
            second =>
            {
                Assert.Equal("乙客户", second.CustomerName);
                Assert.Equal(100m, second.SaleAmount);
            },
            third =>
            {
                Assert.Equal("丙客户", third.CustomerName);
                Assert.Equal(50m, third.SaleAmount);
            });
        Assert.Collection(
            goodsTypeRank,
            first => Assert.Equal("甲分类", first.GoodsTypeName),
            second => Assert.Equal("乙分类", second.GoodsTypeName),
            third => Assert.Equal("丙分类", third.GoodsTypeName));

        var clampedLow = await service.GetDashboardCustomerSalesRankAsync(new DashboardQueryParameters
        {
            DateStart = tieBreakParameters.DateStart,
            DateEnd = tieBreakParameters.DateEnd,
            RankSize = 0
        });
        var clampedHigh = await service.GetDashboardCustomerSalesRankAsync(new DashboardQueryParameters
        {
            DateStart = tieBreakParameters.DateStart,
            DateEnd = tieBreakParameters.DateEnd,
            RankSize = 101
        });

        Assert.Single(clampedLow);
        Assert.Equal(3, clampedHigh.Count);
    }

    [Fact]
    public async Task DashboardReconciliation_FloorsPerBillPendingAtZero()
    {
        await using var context = CreateDbContext();
        var customerId = Guid.NewGuid();
        var normalOrderId = Guid.NewGuid();
        var overSettledOrderId = Guid.NewGuid();
        var billDate = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

        await context.CustomerBills.AddRangeAsync(
            CreateBill(customerId, normalOrderId, billDate, receivableAmount: 100m, settledAmount: 40m),
            CreateBill(customerId, overSettledOrderId, billDate, receivableAmount: 50m, settledAmount: 80m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new ReportService(new ReportRepository(context));
        var reconciliation = await service.GetDashboardReconciliationAsync(new DashboardQueryParameters
        {
            DateStart = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            DateEnd = new DateTime(2026, 7, 10, 23, 59, 59, DateTimeKind.Utc)
        });

        Assert.Equal(150m, reconciliation.ReceivableAmount);
        Assert.Equal(120m, reconciliation.SettledAmount);
        Assert.Equal(60m, reconciliation.PendingAmount);
        Assert.Equal(2, reconciliation.BillCount);
    }

    [Fact]
    public async Task DashboardQueries_WithoutDateBounds_IncludeAllMatchingRows()
    {
        await using var context = CreateDbContext();
        await SeedAsync(context);
        var service = new ReportService(new ReportRepository(context));

        var brief = await service.GetDashboardBriefAsync(new DashboardQueryParameters());
        var reconciliation = await service.GetDashboardReconciliationAsync(new DashboardQueryParameters());
        var pickupStatuses = await service.GetDashboardPickupStatusesAsync(new DashboardQueryParameters());

        Assert.Equal(150m, brief.SaleAmount);
        Assert.Equal(2, brief.OrderCount);
        Assert.Equal(1149m, reconciliation.ReceivableAmount);
        Assert.Equal(3, reconciliation.BillCount);
        Assert.Equal(2, Assert.Single(pickupStatuses, x => x.PickupStatus == PickupTaskStatus.PendingAssign).TaskCount);
        Assert.Equal(1, Assert.Single(pickupStatuses, x => x.PickupStatus == PickupTaskStatus.Cancelled).TaskCount);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        var firstCustomerId = Guid.NewGuid();
        var secondCustomerId = Guid.NewGuid();
        var firstOrderId = Guid.NewGuid();
        var secondOrderId = Guid.NewGuid();
        var ignoredOrderId = Guid.NewGuid();
        var unitId = Guid.NewGuid();

        await context.SaleOrders.AddRangeAsync(
            CreateOrder(firstOrderId, firstCustomerId, "第一学校", "SO-DB-001", new DateTime(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc), SaleOrderStatus.Signed),
            CreateOrder(secondOrderId, secondCustomerId, "第二学校", "SO-DB-002", new DateTime(2026, 7, 11, 8, 0, 0, DateTimeKind.Utc), SaleOrderStatus.Signed),
            CreateOrder(ignoredOrderId, firstCustomerId, "第一学校", "SO-DB-IGNORED", new DateTime(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc), SaleOrderStatus.SortingPending));
        await context.SaleOrderDetails.AddRangeAsync(
            CreateDetail(firstOrderId, unitId, "蔬菜", 100m),
            CreateDetail(secondOrderId, unitId, "水果", 50m),
            CreateDetail(ignoredOrderId, unitId, "蔬菜", 999m));
        await context.CustomerBills.AddRangeAsync(
            CreateBill(firstCustomerId, firstOrderId, new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc), 100m, 40m),
            CreateBill(secondCustomerId, secondOrderId, new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc), 50m, 50m),
            CreateBill(firstCustomerId, ignoredOrderId, new DateTime(2026, 7, 9, 12, 0, 0, DateTimeKind.Utc), 999m, 0m));
        await context.PickupTasks.AddRangeAsync(
            CreatePickupTask("PT-DB-001", PickupTaskStatus.PendingAssign, new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc)),
            CreatePickupTask("PT-DB-002", PickupTaskStatus.PendingAssign, new DateTime(2026, 7, 11, 9, 0, 0, DateTimeKind.Utc)),
            CreatePickupTask("PT-DB-003", PickupTaskStatus.Completed, new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc)),
            CreatePickupTask("PT-DB-004", PickupTaskStatus.Cancelled, new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        // DbContext 会在新增时统一填充当前创建时间；为验证按创建日期筛选，测试数据在持久化后覆写为固定历史时间。
        var pickupTasks = await context.PickupTasks.ToDictionaryAsync(x => x.TaskNo);
        pickupTasks["PT-DB-001"].CreateTime = new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc);
        pickupTasks["PT-DB-002"].CreateTime = new DateTime(2026, 7, 11, 9, 0, 0, DateTimeKind.Utc);
        pickupTasks["PT-DB-003"].CreateTime = new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc);
        pickupTasks["PT-DB-004"].CreateTime = new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static SaleOrder CreateOrder(Guid id, Guid customerId, string customerName, string orderNo, DateTime orderDate, SaleOrderStatus status)
    {
        return new SaleOrder
        {
            Id = id,
            CustomerId = customerId,
            CustomerNameSnapshot = customerName,
            CustomerCodeSnapshot = orderNo,
            OrderNo = orderNo,
            OrderDate = orderDate,
            OrderStatus = status
        };
    }

    private static SaleOrderDetail CreateDetail(
        Guid orderId,
        Guid unitId,
        string goodsTypeName,
        decimal? acceptedAmount,
        decimal? orderTotal = null)
    {
        var totalPrice = orderTotal ?? acceptedAmount ?? 0m;
        return new SaleOrderDetail
        {
            Id = Guid.NewGuid(),
            SaleOrderId = orderId,
            GoodsId = Guid.NewGuid(),
            GoodsNameSnapshot = goodsTypeName + "商品",
            GoodsCodeSnapshot = goodsTypeName,
            GoodsTypeNameSnapshot = goodsTypeName,
            GoodsUnitId = unitId,
            GoodsUnitNameSnapshot = "件",
            BaseUnitId = unitId,
            BaseUnitNameSnapshot = "件",
            Quantity = totalPrice,
            BaseQuantity = totalPrice,
            UnitConversion = 1m,
            FixedPrice = 1m,
            FixedGoodsUnitId = unitId,
            FixedGoodsUnitNameSnapshot = "件",
            TotalPrice = totalPrice,
            CustomerCheckBaseQuantity = acceptedAmount,
            CustomerCheckPrice = acceptedAmount
        };
    }

    private static CustomerBill CreateBill(Guid customerId, Guid orderId, DateTime billDate, decimal receivableAmount, decimal settledAmount)
    {
        return new CustomerBill
        {
            Id = Guid.NewGuid(),
            BillNo = "CB-" + Guid.NewGuid().ToString("N"),
            CustomerId = customerId,
            CustomerNameSnapshot = "客户",
            SaleOrderId = orderId,
            SaleOrderNoSnapshot = "SO",
            BillDate = billDate,
            OrderAmount = receivableAmount,
            ReceivableAmount = receivableAmount,
            SettledAmount = settledAmount,
            BillStatus = settledAmount == receivableAmount ? CustomerBillStatus.Settled : CustomerBillStatus.PartiallySettled
        };
    }

    private static PickupTask CreatePickupTask(string taskNo, PickupTaskStatus pickupStatus, DateTime createTime)
    {
        return new PickupTask
        {
            Id = Guid.NewGuid(),
            TaskNo = taskNo,
            AfterSaleId = Guid.NewGuid(),
            AfterSaleGoodsId = Guid.NewGuid(),
            PickupAddressSnapshot = "测试地址",
            PickupStatus = pickupStatus,
            CreateTime = createTime
        };
    }
}
