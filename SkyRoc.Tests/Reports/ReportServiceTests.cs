using Application.QueryParameters.Reports;
using Application.Services;
using Domain.Entities.AfterSales;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Reports;

/// <summary>
/// 销售与售后报表服务回归测试。
/// </summary>
public class ReportServiceTests
{
    [Fact]
    public async Task SalesGoodsSummary_UsesSignedAcceptedQuantityAndCustomerTagFilters()
    {
        await using var context = CreateDbContext();
        var seed = await SeedSalesAsync(context);
        var service = CreateService(context);

        var page = await service.GetSalesGoodsSummaryAsync(new SalesReportQueryParameters
        {
            Current = 1,
            Size = 10,
            DateStart = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            DateEnd = new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc),
            CustomerTagIds = [seed.TagId]
        });

        Assert.Equal(1, page.Total);
        var item = Assert.Single(page.Records!);
        Assert.Equal(seed.TomatoId, item.GoodsId);
        Assert.Equal("番茄", item.GoodsName);
        Assert.Equal("蔬菜", item.GoodsTypeName);
        Assert.Equal("千克", item.BaseUnitName);
        Assert.Equal(8m, item.SaleBaseQuantity);
        Assert.Equal(80m, item.SaleAmount);
        Assert.Equal(1, item.OrderCount);
        Assert.Equal(1, item.CustomerCount);
    }

    [Fact]
    public async Task SalesCategoryCustomerAndAreaSummaries_GroupBySnapshots()
    {
        await using var context = CreateDbContext();
        await SeedSalesAsync(context);
        var service = CreateService(context);
        var query = new SalesReportQueryParameters
        {
            Current = 1,
            Size = 10,
            DateStart = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            DateEnd = new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        var categories = await service.GetSalesCategorySummaryAsync(query);
        var customers = await service.GetSalesCustomerSummaryAsync(query);
        var areas = await service.GetSalesAreaSummaryAsync(query);

        var category = Assert.Single(categories.Records!);
        Assert.Equal("蔬菜", category.GoodsTypeName);
        Assert.Equal(8m, category.SaleBaseQuantity);
        Assert.Equal(80m, category.SaleAmount);

        var customer = Assert.Single(customers.Records!);
        Assert.Equal("第一学校", customer.CustomerName);
        Assert.Equal(8m, customer.SaleBaseQuantity);
        Assert.Equal(80m, customer.SaleAmount);

        var area = Assert.Single(areas.Records!);
        Assert.Equal("上海市浦东新区学校路1号", area.AreaName);
        Assert.Equal(8m, area.SaleBaseQuantity);
        Assert.Equal(80m, area.SaleAmount);
    }

    [Fact]
    public async Task AfterSaleSummary_UsesCompletedAfterSaleGoodsAndReasonFilters()
    {
        await using var context = CreateDbContext();
        var seed = await SeedSalesAsync(context);
        await SeedAfterSalesAsync(context, seed);
        var service = CreateService(context);

        var page = await service.GetAfterSaleSummaryAsync(new AfterSaleReportQueryParameters
        {
            Current = 1,
            Size = 10,
            DateStart = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            DateEnd = new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc),
            ReasonType = AfterSaleReasonType.QualityIssue
        });

        Assert.Equal(1, page.Total);
        var item = Assert.Single(page.Records!);
        Assert.Equal(AfterSaleReasonType.QualityIssue, item.ReasonType);
        Assert.Equal(AfterSaleType.RefundOnly, item.AfterSaleType);
        Assert.Equal(AfterSaleHandleType.GoodsDiscount, item.HandleType);
        Assert.Equal(2m, item.RefundBaseQuantity);
        Assert.Equal(20m, item.RefundAmount);
        Assert.Equal(1, item.AfterSaleCount);
        Assert.Equal(1, item.CustomerCount);
    }

    [Fact]
    public async Task AfterSaleSummary_ExcludesNonFinancialHandleQuantityFromRefundBaseQuantity()
    {
        await using var context = CreateDbContext();
        var seed = await SeedSalesAsync(context);
        await SeedNonFinancialAfterSaleAsync(context, seed);
        var service = CreateService(context);

        var page = await service.GetAfterSaleSummaryAsync(new AfterSaleReportQueryParameters
        {
            Current = 1,
            Size = 10,
            HandleType = AfterSaleHandleType.Replenishment
        });

        var item = Assert.Single(page.Records!);
        Assert.Equal(AfterSaleHandleType.Replenishment, item.HandleType);
        Assert.Equal(0m, item.RefundBaseQuantity);
        Assert.Equal(0m, item.RefundAmount);
        Assert.Equal(1, item.AfterSaleCount);
    }

    [Fact]
    public async Task SalesGoodsSummary_FiltersGoodsTypeByNameSnapshot_NotLiveGoodsTypeId()
    {
        await using var context = CreateDbContext();
        var seed = await SeedSalesAsync(context);
        var fruitTypeId = Guid.NewGuid();
        await context.GoodsTypes.AddAsync(new GoodsType { Id = fruitTypeId, Name = "水果", Code = "FRUIT" });
        var goods = await context.Goods.SingleAsync(x => x.Id == seed.TomatoId);
        goods.GoodsTypeId = fruitTypeId;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = CreateService(context);

        var byHistoricalVegetable = await service.GetSalesGoodsSummaryAsync(new SalesReportQueryParameters
        {
            Current = 1,
            Size = 10,
            GoodsTypeIds = [seed.GoodsTypeId]
        });
        var byLiveFruit = await service.GetSalesGoodsSummaryAsync(new SalesReportQueryParameters
        {
            Current = 1,
            Size = 10,
            GoodsTypeIds = [fruitTypeId]
        });

        var vegetableItem = Assert.Single(byHistoricalVegetable.Records!);
        Assert.Equal("蔬菜", vegetableItem.GoodsTypeName);
        Assert.Equal(8m, vegetableItem.SaleBaseQuantity);
        Assert.Empty(byLiveFruit.Records!);
    }

    private static ReportService CreateService(ApplicationDbContext context)
    {
        return new ReportService(new ReportRepository(context));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<ReportSeed> SeedSalesAsync(ApplicationDbContext context)
    {
        var tagId = Guid.NewGuid();
        var otherTagId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var goodsTypeId = Guid.NewGuid();
        var goodsId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var pendingOrderId = Guid.NewGuid();

        await context.CustomerTags.AddRangeAsync(
            new CustomerTag { Id = tagId, Name = "学校客户", Code = "SCHOOL" },
            new CustomerTag { Id = otherTagId, Name = "企业客户", Code = "COMPANY" });
        await context.Customers.AddRangeAsync(
            new Customer
            {
                Id = customerId,
                Name = "第一学校",
                Code = "SCHOOL_001",
                Address = "上海市浦东新区学校路1号"
            },
            new Customer
            {
                Id = otherCustomerId,
                Name = "第二客户",
                Code = "CUSTOMER_002",
                Address = "上海市黄浦区客户路2号"
            });
        await context.CustomerTagRelations.AddRangeAsync(
            new CustomerTagRelation { CustomerId = customerId, CustomerTagId = tagId },
            new CustomerTagRelation { CustomerId = otherCustomerId, CustomerTagId = otherTagId });
        await context.GoodsTypes.AddAsync(new GoodsType { Id = goodsTypeId, Name = "蔬菜", Code = "VEGETABLE" });
        await context.Goods.AddAsync(new GoodsEntity
        {
            Id = goodsId,
            Name = "番茄",
            Code = "TOMATO",
            GoodsTypeId = goodsTypeId,
            BaseUnitId = unitId
        });
        await context.GoodsUnits.AddAsync(new GoodsUnit
        {
            Id = unitId,
            GoodsId = goodsId,
            Name = "千克",
            Code = "KG",
            IsBaseUnit = true,
            ConversionRate = 1m
        });

        await context.SaleOrders.AddRangeAsync(
            new SaleOrder
            {
                Id = orderId,
                OrderNo = "SO-RPT-001",
                CustomerId = customerId,
                CustomerNameSnapshot = "第一学校",
                CustomerCodeSnapshot = "SCHOOL_001",
                OrderDate = new DateTime(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc),
                DeliveryAddressSnapshot = "上海市浦东新区学校路1号",
                OrderStatus = SaleOrderStatus.Signed,
                OrderPrice = 100m,
                SettlementPrice = 80m
            },
            new SaleOrder
            {
                Id = pendingOrderId,
                OrderNo = "SO-RPT-PENDING",
                CustomerId = customerId,
                CustomerNameSnapshot = "第一学校",
                CustomerCodeSnapshot = "SCHOOL_001",
                OrderDate = new DateTime(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc),
                DeliveryAddressSnapshot = "上海市浦东新区学校路1号",
                OrderStatus = SaleOrderStatus.SortingPending,
                OrderPrice = 999m,
                SettlementPrice = 999m
            });
        await context.SaleOrderDetails.AddRangeAsync(
            new SaleOrderDetail
            {
                Id = Guid.NewGuid(),
                SaleOrderId = orderId,
                GoodsId = goodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                GoodsTypeNameSnapshot = "蔬菜",
                GoodsUnitId = unitId,
                GoodsUnitNameSnapshot = "千克",
                Quantity = 10m,
                BaseQuantity = 10m,
                BaseUnitId = unitId,
                BaseUnitNameSnapshot = "千克",
                UnitConversion = 1m,
                FixedPrice = 10m,
                FixedGoodsUnitId = unitId,
                FixedGoodsUnitNameSnapshot = "千克",
                TotalPrice = 100m,
                CustomerCheckStatus = OrderCustomerCheckStatus.Rejected,
                CustomerCheckBaseQuantity = 8m,
                CustomerCheckPrice = 80m
            },
            new SaleOrderDetail
            {
                Id = Guid.NewGuid(),
                SaleOrderId = pendingOrderId,
                GoodsId = goodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                GoodsTypeNameSnapshot = "蔬菜",
                GoodsUnitId = unitId,
                GoodsUnitNameSnapshot = "千克",
                Quantity = 99m,
                BaseQuantity = 99m,
                BaseUnitId = unitId,
                BaseUnitNameSnapshot = "千克",
                UnitConversion = 1m,
                FixedPrice = 10m,
                FixedGoodsUnitId = unitId,
                FixedGoodsUnitNameSnapshot = "千克",
                TotalPrice = 999m
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new ReportSeed(customerId, goodsId, unitId, tagId, goodsTypeId);
    }

    private static async Task SeedAfterSalesAsync(ApplicationDbContext context, ReportSeed seed)
    {
        var completedId = Guid.NewGuid();
        var draftId = Guid.NewGuid();
        await context.AfterSales.AddRangeAsync(
            new AfterSale
            {
                Id = completedId,
                AfterSaleNo = "AS-RPT-001",
                CustomerId = seed.CustomerId,
                CustomerNameSnapshot = "第一学校",
                Source = "客户反馈",
                AfterStatus = AfterSaleStatus.Completed,
                OrderPrice = 100m,
                SettlementPrice = 80m,
                CreateTime = new DateTime(2026, 7, 11, 8, 0, 0, DateTimeKind.Utc)
            },
            new AfterSale
            {
                Id = draftId,
                AfterSaleNo = "AS-RPT-DRAFT",
                CustomerId = seed.CustomerId,
                CustomerNameSnapshot = "第一学校",
                Source = "客户反馈",
                AfterStatus = AfterSaleStatus.Draft,
                OrderPrice = 100m,
                SettlementPrice = 100m,
                CreateTime = new DateTime(2026, 7, 11, 8, 0, 0, DateTimeKind.Utc)
            });
        await context.AfterSaleGoods.AddRangeAsync(
            new AfterSaleGoods
            {
                Id = Guid.NewGuid(),
                AfterSaleId = completedId,
                GoodsId = seed.TomatoId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                GoodsTypeNameSnapshot = "蔬菜",
                GoodsUnitId = seed.UnitId,
                GoodsUnitNameSnapshot = "千克",
                BaseUnitId = seed.UnitId,
                BaseUnitNameSnapshot = "千克",
                ConversionRate = 1m,
                AfterSaleType = AfterSaleType.RefundOnly,
                ActualRefundQuantity = 2m,
                BaseRefundQuantity = 2m,
                UnitPrice = 10m,
                RefundAmount = 20m,
                ReasonType = AfterSaleReasonType.QualityIssue,
                HandleType = AfterSaleHandleType.GoodsDiscount
            },
            new AfterSaleGoods
            {
                Id = Guid.NewGuid(),
                AfterSaleId = draftId,
                GoodsId = seed.TomatoId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                GoodsTypeNameSnapshot = "蔬菜",
                GoodsUnitId = seed.UnitId,
                GoodsUnitNameSnapshot = "千克",
                BaseUnitId = seed.UnitId,
                BaseUnitNameSnapshot = "千克",
                ConversionRate = 1m,
                AfterSaleType = AfterSaleType.RefundOnly,
                ActualRefundQuantity = 9m,
                BaseRefundQuantity = 9m,
                UnitPrice = 10m,
                RefundAmount = 90m,
                ReasonType = AfterSaleReasonType.QualityIssue,
                HandleType = AfterSaleHandleType.GoodsDiscount
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static async Task SeedNonFinancialAfterSaleAsync(ApplicationDbContext context, ReportSeed seed)
    {
        var afterSaleId = Guid.NewGuid();
        await context.AfterSales.AddAsync(new AfterSale
        {
            Id = afterSaleId,
            AfterSaleNo = "AS-RPT-REPLENISH",
            CustomerId = seed.CustomerId,
            CustomerNameSnapshot = "第一学校",
            Source = "客户反馈",
            AfterStatus = AfterSaleStatus.Completed,
            OrderPrice = 100m,
            SettlementPrice = 100m,
            CreateTime = new DateTime(2026, 7, 12, 8, 0, 0, DateTimeKind.Utc)
        });
        await context.AfterSaleGoods.AddAsync(new AfterSaleGoods
        {
            Id = Guid.NewGuid(),
            AfterSaleId = afterSaleId,
            GoodsId = seed.TomatoId,
            GoodsNameSnapshot = "番茄",
            GoodsCodeSnapshot = "TOMATO",
            GoodsTypeNameSnapshot = "蔬菜",
            GoodsUnitId = seed.UnitId,
            GoodsUnitNameSnapshot = "千克",
            BaseUnitId = seed.UnitId,
            BaseUnitNameSnapshot = "千克",
            ConversionRate = 1m,
            AfterSaleType = AfterSaleType.RefundOnly,
            ActualRefundQuantity = 3m,
            BaseRefundQuantity = 3m,
            UnitPrice = 10m,
            RefundAmount = 0m,
            ReasonType = AfterSaleReasonType.QualityIssue,
            HandleType = AfterSaleHandleType.Replenishment
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private sealed record ReportSeed(
        Guid CustomerId,
        Guid TomatoId,
        Guid UnitId,
        Guid TagId,
        Guid GoodsTypeId);
}
