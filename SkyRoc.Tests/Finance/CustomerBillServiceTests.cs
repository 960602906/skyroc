using Application.interfaces;
using Application.Services;
using Domain.Entities.AfterSales;
using Domain.Entities.Customers;
using Domain.Entities.Finance;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using SkyRoc.Tests.Testing;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Finance;

/// <summary>
/// 验证客户账单服务把订单签收和已完成售后结果幂等同步为应收账单。
/// </summary>
public class CustomerBillServiceTests
{
    [Fact]
    public async Task SyncOrderAcceptanceAsync_IncludesCompletedAfterSaleAdjustment_WhenAfterSaleFinishedBeforeBilling()
    {
        await using var context = CreateDbContext();
        var seed = await SeedSignedOrderWithCompletedAfterSaleAsync(context);
        var service = CreateService(context);
        var saleOrder = await new SaleOrderRepository(context).GetByIdAsync(seed.SaleOrderId);

        await service.SyncOrderAcceptanceAsync(saleOrder!);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        service = CreateService(context);
        saleOrder = await new SaleOrderRepository(context).GetByIdAsync(seed.SaleOrderId);
        await service.SyncOrderAcceptanceAsync(saleOrder!);
        await context.SaveChangesAsync();

        var bill = await context.CustomerBills.Include(x => x.Details).SingleAsync();
        Assert.Equal(25m, bill.OrderAmount);
        Assert.Equal(-5m, bill.AfterSaleAdjustmentAmount);
        Assert.Equal(20m, bill.ReceivableAmount);
        Assert.Equal(2, bill.Details.Count);
        Assert.Single(bill.Details, x => x.SourceType == CustomerBillDetailSourceType.OrderAcceptance);
        Assert.Single(bill.Details, x => x.SourceType == CustomerBillDetailSourceType.AfterSaleAdjustment);
    }

    private static CustomerBillService CreateService(ApplicationDbContext context)
    {
        return new CustomerBillService(
            new CustomerBillRepository(context),
            new AfterSaleRepository(context),
            new FakeCurrentUserService(),
            DocumentNoGeneratorTestDouble.Instance);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<BillSeed> SeedSignedOrderWithCompletedAfterSaleAsync(ApplicationDbContext context)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "学校客户", Code = "SCHOOL" };
        var goodsType = new GoodsType { Id = Guid.NewGuid(), Name = "蔬菜", Code = "VEG" };
        var goods = new GoodsEntity { Id = Guid.NewGuid(), Name = "番茄", Code = "TOMATO", GoodsTypeId = goodsType.Id };
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
            OrderNo = "SO-BILL-001",
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            CustomerCodeSnapshot = customer.Code,
            OrderDate = DateTime.UtcNow,
            OrderStatus = SaleOrderStatus.Signed,
            OrderPrice = 25m,
            SettlementPrice = 25m
        };
        var orderDetail = new SaleOrderDetail
        {
            Id = Guid.NewGuid(),
            SaleOrderId = order.Id,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            GoodsTypeNameSnapshot = goodsType.Name,
            GoodsUnitId = unit.Id,
            GoodsUnitNameSnapshot = unit.Name,
            Quantity = 10m,
            BaseQuantity = 10m,
            BaseUnitId = unit.Id,
            BaseUnitNameSnapshot = unit.Name,
            UnitConversion = 1m,
            FixedPrice = 2.5m,
            TotalPrice = 25m,
            CustomerCheckBaseQuantity = 10m,
            CustomerCheckPrice = 25m,
            CustomerCheckStatus = OrderCustomerCheckStatus.Accepted
        };
        order.Details.Add(orderDetail);
        var afterSale = new AfterSale
        {
            Id = Guid.NewGuid(),
            AfterSaleNo = "AS-BILL-001",
            SaleOrderId = order.Id,
            SaleOrderNoSnapshot = order.OrderNo,
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            Source = "测试",
            AfterStatus = AfterSaleStatus.Completed,
            OrderPrice = 25m,
            SettlementPrice = 20m
        };
        afterSale.Goods.Add(new AfterSaleGoods
        {
            Id = Guid.NewGuid(),
            AfterSaleId = afterSale.Id,
            SaleOrderDetailId = orderDetail.Id,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            GoodsTypeNameSnapshot = goodsType.Name,
            GoodsUnitId = unit.Id,
            GoodsUnitNameSnapshot = unit.Name,
            BaseUnitId = unit.Id,
            BaseUnitNameSnapshot = unit.Name,
            ConversionRate = 1m,
            AfterSaleType = AfterSaleType.RefundOnly,
            ActualRefundQuantity = 2m,
            BaseRefundQuantity = 2m,
            UnitPrice = 2.5m,
            RefundAmount = 5m,
            ReasonType = AfterSaleReasonType.QualityIssue,
            HandleType = AfterSaleHandleType.GoodsDiscount
        });

        await context.AddRangeAsync(customer, goodsType, goods, unit, order, afterSale);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new BillSeed(order.Id);
    }

    private sealed record BillSeed(Guid SaleOrderId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("22222222-2222-2222-2222-222222222222");
        public string? GetUserName() => "finance-user";
        public string? GetEmail() => "finance@example.com";
        public string? GetRole() => "finance";
        public IReadOnlyList<string> GetRoles() => ["finance"];
        public bool HasClaim(string claimType, string claimValue) => false;
    }
}
