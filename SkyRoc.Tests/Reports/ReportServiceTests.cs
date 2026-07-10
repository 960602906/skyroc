using Application.QueryParameters.Reports;
using Application.Services;
using Domain.Entities.AfterSales;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
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

    [Fact]
    public async Task DailyStockInOutSummary_UsesAuditedStockDocumentsAndDateFilters()
    {
        await using var context = CreateDbContext();
        await SeedStockPurchaseReportsAsync(context);
        var service = CreateService(context);

        var page = await service.GetDailyStockInOutSummaryAsync(new StockReportQueryParameters
        {
            Current = 1,
            Size = 10,
            DateStart = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            DateEnd = new DateTime(2026, 7, 1, 23, 59, 59, DateTimeKind.Utc)
        });

        var item = Assert.Single(page.Records!);
        Assert.Equal(new DateOnly(2026, 7, 1), item.ReportDate);
        Assert.Equal(12m, item.InBaseQuantity);
        Assert.Equal(120m, item.InAmount);
        Assert.Equal(3m, item.OutBaseQuantity);
        Assert.Equal(30m, item.OutAmount);
        Assert.Equal(1, item.InOrderCount);
        Assert.Equal(1, item.OutOrderCount);
    }

    [Fact]
    public async Task DailyGoodsStockInOutSummary_GroupsAuditedStockByDateAndGoods()
    {
        await using var context = CreateDbContext();
        var seed = await SeedStockPurchaseReportsAsync(context);
        var service = CreateService(context);

        var page = await service.GetDailyGoodsStockInOutSummaryAsync(new StockReportQueryParameters
        {
            Current = 1,
            Size = 10,
            GoodsIds = [seed.GoodsId],
            DateStart = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            DateEnd = new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc)
        });

        Assert.Equal(2, page.Total);
        var firstDay = Assert.Single(page.Records!, x => x.ReportDate == new DateOnly(2026, 7, 1));
        Assert.Equal(seed.GoodsId, firstDay.GoodsId);
        Assert.Equal("番茄", firstDay.GoodsName);
        Assert.Equal("TOMATO", firstDay.GoodsCode);
        Assert.Equal("千克", firstDay.BaseUnitName);
        Assert.Equal(12m, firstDay.InBaseQuantity);
        Assert.Equal(120m, firstDay.InAmount);
        Assert.Equal(3m, firstDay.OutBaseQuantity);
        Assert.Equal(30m, firstDay.OutAmount);
    }

    [Fact]
    public async Task PurchaseInOutSummaries_UsePurchaseInboundAndReturnOutboundOnly()
    {
        await using var context = CreateDbContext();
        var seed = await SeedStockPurchaseReportsAsync(context);
        var service = CreateService(context);
        var query = new PurchaseInOutReportQueryParameters
        {
            Current = 1,
            Size = 10,
            SupplierId = seed.SupplierId,
            PurchaserId = seed.PurchaserId
        };

        var goods = await service.GetPurchaseInOutGoodsSummaryAsync(query);
        var suppliers = await service.GetPurchaseInOutSupplierSummaryAsync(query);
        var purchasers = await service.GetPurchaseInOutPurchaserSummaryAsync(query);

        var goodsItem = Assert.Single(goods.Records!);
        Assert.Equal(seed.GoodsId, goodsItem.GoodsId);
        Assert.Equal(12m, goodsItem.InBaseQuantity);
        Assert.Equal(120m, goodsItem.InAmount);
        Assert.Equal(0m, goodsItem.OutBaseQuantity);
        Assert.Equal(0m, goodsItem.OutAmount);
        Assert.Equal(120m, goodsItem.NetAmount);

        var supplierItem = Assert.Single(suppliers.Records!);
        Assert.Equal(seed.SupplierId, supplierItem.SupplierId);
        Assert.Equal("绿源供应商", supplierItem.SupplierName);
        Assert.Equal(12m, supplierItem.InBaseQuantity);
        Assert.Equal(0m, supplierItem.OutBaseQuantity);

        var purchaserItem = Assert.Single(purchasers.Records!);
        Assert.Equal(seed.PurchaserId, purchaserItem.PurchaserId);
        Assert.Equal("张采购", purchaserItem.PurchaserName);
        Assert.Equal(120m, purchaserItem.InAmount);
        Assert.Equal(0m, purchaserItem.OutAmount);
    }

    [Fact]
    public async Task PurchaseInOutPurchaserSummary_AssignsReturnToEarliestPurchaseInboundPurchaser()
    {
        await using var context = CreateDbContext();
        var seed = await SeedStockPurchaseReportsAsync(context);
        var service = CreateService(context);

        var laterPurchaserPage = await service.GetPurchaseInOutPurchaserSummaryAsync(
            new PurchaseInOutReportQueryParameters
            {
                Current = 1,
                Size = 10,
                PurchaserId = seed.PurchaserId
            });
        var originalPurchaserPage = await service.GetPurchaseInOutPurchaserSummaryAsync(
            new PurchaseInOutReportQueryParameters
            {
                Current = 1,
                Size = 10,
                PurchaserId = seed.OtherPurchaserId
            });

        var laterPurchaser = Assert.Single(laterPurchaserPage.Records!);
        Assert.Equal(12m, laterPurchaser.InBaseQuantity);
        Assert.Equal(0m, laterPurchaser.OutBaseQuantity);

        var originalPurchaser = Assert.Single(originalPurchaserPage.Records!);
        Assert.Equal(2m, originalPurchaser.InBaseQuantity);
        Assert.Equal(3m, originalPurchaser.OutBaseQuantity);
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

    private static async Task<StockPurchaseReportSeed> SeedStockPurchaseReportsAsync(ApplicationDbContext context)
    {
        var wareId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var purchaserId = Guid.NewGuid();
        var otherPurchaserId = Guid.NewGuid();
        var goodsId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var purchaseOrderId = Guid.NewGuid();
        var otherPurchaseOrderId = Guid.NewGuid();
        var purchaseOrderDetailId = Guid.NewGuid();
        var otherPurchaseOrderDetailId = Guid.NewGuid();
        var stockBatchId = Guid.NewGuid();
        var otherPurchaseInOrderId = Guid.NewGuid();
        var purchaseInOrderId = Guid.NewGuid();
        var otherInOrderId = Guid.NewGuid();
        var purchaseReturnOrderId = Guid.NewGuid();
        var draftOutOrderId = Guid.NewGuid();

        await context.Wares.AddAsync(new Ware { Id = wareId, Name = "一号仓", Code = "WARE-01" });
        await context.Suppliers.AddAsync(new Supplier { Id = supplierId, Name = "绿源供应商", Code = "SUP-01" });
        await context.Purchasers.AddAsync(new Purchaser { Id = purchaserId, Name = "张采购", Code = "PUR-01" });
        await context.Purchasers.AddAsync(new Purchaser { Id = otherPurchaserId, Name = "李采购", Code = "PUR-02" });
        await context.Goods.AddAsync(new GoodsEntity
        {
            Id = goodsId,
            Name = "番茄",
            Code = "TOMATO",
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
        await context.StockBatches.AddAsync(new StockBatch
        {
            Id = stockBatchId,
            WareId = wareId,
            GoodsId = goodsId,
            GoodsNameSnapshot = "番茄",
            GoodsCodeSnapshot = "TOMATO",
            BatchNo = "BATCH-001",
            BaseUnitId = unitId,
            BaseUnitNameSnapshot = "千克",
            CurrentQuantity = 9m,
            AvailableQuantity = 9m,
            UnitCost = 10m
        });
        await context.PurchaseOrders.AddRangeAsync(
            new PurchaseOrder
            {
                Id = otherPurchaseOrderId,
                PurchaseNo = "PO-RPT-OTHER",
                SupplierId = supplierId,
                SupplierNameSnapshot = "绿源供应商",
                PurchaserId = otherPurchaserId,
                PurchaserNameSnapshot = "李采购",
                PurchasePattern = PurchasePattern.SupplierDirect,
                BusinessStatus = PurchaseOrderStatus.Completed
            },
            new PurchaseOrder
            {
                Id = purchaseOrderId,
                PurchaseNo = "PO-RPT-001",
                SupplierId = supplierId,
                SupplierNameSnapshot = "绿源供应商",
                PurchaserId = purchaserId,
                PurchaserNameSnapshot = "张采购",
                PurchasePattern = PurchasePattern.SupplierDirect,
                BusinessStatus = PurchaseOrderStatus.Completed
            });
        await context.PurchaseOrderDetails.AddRangeAsync(
            new PurchaseOrderDetail
            {
                Id = otherPurchaseOrderDetailId,
                PurchaseOrderId = otherPurchaseOrderId,
                GoodsId = goodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                PurchaseUnitId = unitId,
                PurchaseUnitNameSnapshot = "千克",
                RequiredQuantity = 2m,
                PurchaseQuantity = 2m,
                PurchasePrice = 10m,
                PurchaseTotalPrice = 20m
            },
            new PurchaseOrderDetail
            {
                Id = purchaseOrderDetailId,
                PurchaseOrderId = purchaseOrderId,
                GoodsId = goodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                PurchaseUnitId = unitId,
                PurchaseUnitNameSnapshot = "千克",
                RequiredQuantity = 12m,
                PurchaseQuantity = 12m,
                PurchasePrice = 10m,
                PurchaseTotalPrice = 120m
            });
        await context.StockInOrders.AddRangeAsync(
            new StockInOrder
            {
                Id = otherPurchaseInOrderId,
                InNo = "SI-PUR-OTHER",
                OrderType = StockInOrderType.Purchase,
                BusinessStatus = StockDocumentStatus.Audited,
                WareId = wareId,
                WareNameSnapshot = "一号仓",
                PurchaseOrderId = otherPurchaseOrderId,
                SupplierId = supplierId,
                SupplierNameSnapshot = "绿源供应商",
                PurchaserId = otherPurchaserId,
                PurchaserNameSnapshot = "李采购",
                PurchasePattern = PurchasePattern.SupplierDirect,
                InTime = new DateTime(2026, 6, 30, 9, 0, 0, DateTimeKind.Utc),
                TotalBaseQuantity = 2m,
                TotalAmount = 20m
            },
            new StockInOrder
            {
                Id = purchaseInOrderId,
                InNo = "SI-PUR-001",
                OrderType = StockInOrderType.Purchase,
                BusinessStatus = StockDocumentStatus.Audited,
                WareId = wareId,
                WareNameSnapshot = "一号仓",
                PurchaseOrderId = purchaseOrderId,
                SupplierId = supplierId,
                SupplierNameSnapshot = "绿源供应商",
                PurchaserId = purchaserId,
                PurchaserNameSnapshot = "张采购",
                PurchasePattern = PurchasePattern.SupplierDirect,
                InTime = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
                TotalBaseQuantity = 12m,
                TotalAmount = 120m
            },
            new StockInOrder
            {
                Id = otherInOrderId,
                InNo = "SI-OTHER-001",
                OrderType = StockInOrderType.Other,
                BusinessStatus = StockDocumentStatus.Audited,
                WareId = wareId,
                WareNameSnapshot = "一号仓",
                InTime = new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
                TotalBaseQuantity = 5m,
                TotalAmount = 50m
            });
        await context.StockInDetails.AddRangeAsync(
            new StockInDetail
            {
                Id = Guid.NewGuid(),
                StockInOrderId = otherPurchaseInOrderId,
                PurchaseOrderDetailId = otherPurchaseOrderDetailId,
                StockBatchId = stockBatchId,
                GoodsId = goodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                GoodsUnitId = unitId,
                GoodsUnitNameSnapshot = "千克",
                ConversionRate = 1m,
                Quantity = 2m,
                BaseQuantity = 2m,
                UnitPrice = 10m,
                TotalPrice = 20m,
                BatchNo = "BATCH-001"
            },
            new StockInDetail
            {
                Id = Guid.NewGuid(),
                StockInOrderId = purchaseInOrderId,
                PurchaseOrderDetailId = purchaseOrderDetailId,
                StockBatchId = stockBatchId,
                GoodsId = goodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                GoodsUnitId = unitId,
                GoodsUnitNameSnapshot = "千克",
                ConversionRate = 1m,
                Quantity = 12m,
                BaseQuantity = 12m,
                UnitPrice = 10m,
                TotalPrice = 120m,
                BatchNo = "BATCH-001"
            },
            new StockInDetail
            {
                Id = Guid.NewGuid(),
                StockInOrderId = otherInOrderId,
                GoodsId = goodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                GoodsUnitId = unitId,
                GoodsUnitNameSnapshot = "千克",
                ConversionRate = 1m,
                Quantity = 5m,
                BaseQuantity = 5m,
                UnitPrice = 10m,
                TotalPrice = 50m,
                BatchNo = "BATCH-002"
            });
        await context.StockOutOrders.AddRangeAsync(
            new StockOutOrder
            {
                Id = purchaseReturnOrderId,
                OutNo = "SO-PUR-RETURN-001",
                OrderType = StockOutOrderType.PurchaseReturn,
                BusinessStatus = StockDocumentStatus.Audited,
                WareId = wareId,
                WareNameSnapshot = "一号仓",
                SupplierId = supplierId,
                SupplierNameSnapshot = "绿源供应商",
                OutTime = new DateTime(2026, 7, 1, 15, 0, 0, DateTimeKind.Utc),
                TotalBaseQuantity = 3m,
                TotalAmount = 30m
            },
            new StockOutOrder
            {
                Id = draftOutOrderId,
                OutNo = "SO-DRAFT-001",
                OrderType = StockOutOrderType.PurchaseReturn,
                BusinessStatus = StockDocumentStatus.Draft,
                WareId = wareId,
                WareNameSnapshot = "一号仓",
                SupplierId = supplierId,
                SupplierNameSnapshot = "绿源供应商",
                OutTime = new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Utc),
                TotalBaseQuantity = 99m,
                TotalAmount = 990m
            });
        await context.StockOutDetails.AddRangeAsync(
            new StockOutDetail
            {
                Id = Guid.NewGuid(),
                StockOutOrderId = purchaseReturnOrderId,
                StockBatchId = stockBatchId,
                GoodsId = goodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                GoodsUnitId = unitId,
                GoodsUnitNameSnapshot = "千克",
                ConversionRate = 1m,
                Quantity = 3m,
                BaseQuantity = 3m,
                UnitPrice = 10m,
                TotalPrice = 30m,
                BatchNoSnapshot = "BATCH-001"
            },
            new StockOutDetail
            {
                Id = Guid.NewGuid(),
                StockOutOrderId = draftOutOrderId,
                GoodsId = goodsId,
                GoodsNameSnapshot = "番茄",
                GoodsCodeSnapshot = "TOMATO",
                GoodsUnitId = unitId,
                GoodsUnitNameSnapshot = "千克",
                ConversionRate = 1m,
                Quantity = 99m,
                BaseQuantity = 99m,
                UnitPrice = 10m,
                TotalPrice = 990m,
                BatchNoSnapshot = "BATCH-DRAFT"
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new StockPurchaseReportSeed(
            WareId: wareId,
            SupplierId: supplierId,
            PurchaserId: purchaserId,
            OtherPurchaserId: otherPurchaserId,
            GoodsId: goodsId);
    }

    private sealed record ReportSeed(
        Guid CustomerId,
        Guid TomatoId,
        Guid UnitId,
        Guid TagId,
        Guid GoodsTypeId);

    private sealed record StockPurchaseReportSeed(
        Guid WareId,
        Guid SupplierId,
        Guid PurchaserId,
        Guid OtherPurchaserId,
        Guid GoodsId);
}
