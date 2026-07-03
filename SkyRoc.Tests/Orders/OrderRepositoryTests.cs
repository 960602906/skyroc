using Application.QueryParameters;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Orders;

public class OrderRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_loads_order_details_and_audit_logs()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context);
        context.ChangeTracker.Clear();

        var result = await new SaleOrderRepository(context).GetByIdAsync(seed.OrderId);

        Assert.NotNull(result);
        Assert.Equal("学校客户", result.Customer.Name);
        Assert.Equal("番茄", Assert.Single(result.Details).Goods.Name);
        Assert.Equal("审核员", Assert.Single(result.AuditLogs).AuditUserNameSnapshot);
    }

    [Fact]
    public async Task ExistsOrderNoAsync_honors_excluded_order()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context);
        var repository = new SaleOrderRepository(context);

        Assert.True(await repository.ExistsOrderNoAsync(" SO202607020001 "));
        Assert.False(await repository.ExistsOrderNoAsync("SO202607020001", seed.OrderId));
    }

    [Fact]
    public async Task Order_query_models_filter_order_and_goods_views_independently()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context);
        var orderParameters = new SaleOrderQueryParameters
        {
            CustomerId = seed.CustomerId,
            GoodsIds = [seed.GoodsId],
            DateStart = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
            DateEnd = new DateTime(2026, 7, 2, 23, 59, 59, DateTimeKind.Utc)
        };
        var detailParameters = new SaleOrderDetailQueryParameters
        {
            GoodsKey = "番茄",
            CustomerId = seed.CustomerId,
            HasPurchasePlan = false
        };

        var orders = await context.SaleOrders.Where(orderParameters.QueryBuild()).ToListAsync();
        var details = await context.SaleOrderDetails.Where(detailParameters.QueryBuild()).ToListAsync();

        Assert.Single(orders);
        Assert.Single(details);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<OrderSeed> SeedOrderAsync(ApplicationDbContext context)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "学校客户",
            Code = "SCHOOL_001"
        };
        var goodsType = new GoodsType
        {
            Id = Guid.NewGuid(),
            Name = "蔬菜",
            Code = "VEGETABLE"
        };
        var goods = new GoodsEntity
        {
            Id = Guid.NewGuid(),
            Name = "番茄",
            Code = "TOMATO",
            GoodsTypeId = goodsType.Id
        };
        var unit = new GoodsUnit
        {
            Id = Guid.NewGuid(),
            GoodsId = goods.Id,
            Name = "千克",
            Code = "KG",
            ConversionRate = 1m,
            IsBaseUnit = true
        };
        goods.BaseUnitId = unit.Id;
        var order = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO202607020001",
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            CustomerCodeSnapshot = customer.Code,
            OrderDate = new DateTime(2026, 7, 2, 8, 0, 0, DateTimeKind.Utc)
        };
        var detail = new SaleOrderDetail
        {
            Id = Guid.NewGuid(),
            SaleOrderId = order.Id,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            GoodsUnitId = unit.Id,
            GoodsUnitNameSnapshot = unit.Name,
            Quantity = 10m,
            BaseQuantity = 10m,
            BaseUnitId = unit.Id,
            BaseUnitNameSnapshot = unit.Name,
            UnitConversion = 1m,
            FixedPrice = 8m,
            FixedGoodsUnitId = unit.Id,
            FixedGoodsUnitNameSnapshot = unit.Name,
            TotalPrice = 80m
        };
        var auditLog = new OrderAuditLog
        {
            Id = Guid.NewGuid(),
            SaleOrderId = order.Id,
            Action = OrderAuditAction.Submit,
            PreviousStatus = SaleOrderStatus.PendingAudit,
            CurrentStatus = SaleOrderStatus.PendingAudit,
            AuditUserNameSnapshot = "审核员",
            AuditTime = DateTime.UtcNow
        };

        await context.Customers.AddAsync(customer);
        await context.GoodsTypes.AddAsync(goodsType);
        await context.Goods.AddAsync(goods);
        await context.GoodsUnits.AddAsync(unit);
        await context.SaleOrders.AddAsync(order);
        await context.SaleOrderDetails.AddAsync(detail);
        await context.OrderAuditLogs.AddAsync(auditLog);
        await context.SaveChangesAsync();
        return new OrderSeed(order.Id, customer.Id, goods.Id);
    }

    private sealed record OrderSeed(Guid OrderId, Guid CustomerId, Guid GoodsId);
}
