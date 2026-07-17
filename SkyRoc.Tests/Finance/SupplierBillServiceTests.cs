using Application.Interfaces;
using Application.Services;
using Domain.Entities.Finance;
using Domain.Entities.Goods;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using SkyRoc.Tests.Testing;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Finance;

/// <summary>
/// 验证供应商待结单据服务把采购入库和采购退货出库审核结果幂等同步为应付单据。
/// </summary>
public class SupplierBillServiceTests
{
    [Fact]
    public async Task SyncPurchaseStockInAsync_CreatesPositivePayableBill()
    {
        await using var context = CreateDbContext();
        var seed = await SeedPurchaseStockInAsync(context);
        var service = CreateService(context);
        var stockIn = await context.StockInOrders.Include(x => x.Details)
            .SingleAsync(x => x.Id == seed.StockInOrderId);

        await service.SyncPurchaseStockInAsync(stockIn);
        await context.SaveChangesAsync();

        var bill = await context.SupplierBills.Include(x => x.Details).SingleAsync();
        Assert.Equal(SupplierBillSourceType.PurchaseStockIn, bill.SourceType);
        Assert.Equal(100m, bill.DocumentAmount);
        Assert.Equal(100m, bill.PayableAmount);
        Assert.Equal(SupplierBillStatus.Pending, bill.BillStatus);
        Assert.Single(bill.Details);
        Assert.Equal(100m, bill.Details.Single().Amount);
    }

    [Fact]
    public async Task SyncPurchaseReturnOutAsync_CreatesNegativePayableBill()
    {
        await using var context = CreateDbContext();
        var seed = await SeedPurchaseReturnOutAsync(context);
        var service = CreateService(context);
        var stockOut = await context.StockOutOrders.Include(x => x.Details)
            .SingleAsync(x => x.Id == seed.StockOutOrderId);

        await service.SyncPurchaseReturnOutAsync(stockOut);
        await context.SaveChangesAsync();

        var bill = await context.SupplierBills.Include(x => x.Details).SingleAsync();
        Assert.Equal(SupplierBillSourceType.PurchaseReturnOut, bill.SourceType);
        Assert.Equal(30m, bill.DocumentAmount);
        Assert.Equal(-30m, bill.PayableAmount);
        Assert.Single(bill.Details);
        Assert.Equal(-30m, bill.Details.Single().Amount);
    }

    [Fact]
    public async Task EnsureCanReverseSourceDocumentAsync_RejectsWhenBillAlreadySettled()
    {
        await using var context = CreateDbContext();
        var seed = await SeedPurchaseStockInAsync(context);
        var bill = new SupplierBill
        {
            Id = Guid.NewGuid(),
            BillNo = "SB-REVERSE",
            SupplierId = seed.SupplierId,
            SupplierNameSnapshot = "供应商A",
            SourceType = SupplierBillSourceType.PurchaseStockIn,
            StockInOrderId = seed.StockInOrderId,
            SourceDocumentNoSnapshot = "IN-001",
            BillDate = DateTime.UtcNow,
            DocumentAmount = 100m,
            PayableAmount = 100m,
            SettledAmount = 10m,
            BillStatus = SupplierBillStatus.PartiallySettled
        };
        await context.AddAsync(bill);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.EnsureCanReverseSourceDocumentAsync(seed.StockInOrderId, null));
        Assert.Contains("已有结算金额", exception.Message);
    }

    [Fact]
    public async Task EnsureCanReverseSourceDocumentAsync_RejectsWhenVoidedSettlementStillReferencesBill()
    {
        await using var context = CreateDbContext();
        var seed = await SeedPurchaseStockInAsync(context);
        var bill = new SupplierBill
        {
            Id = Guid.NewGuid(),
            BillNo = "SB-VOIDED",
            SupplierId = seed.SupplierId,
            SupplierNameSnapshot = "供应商A",
            SourceType = SupplierBillSourceType.PurchaseStockIn,
            StockInOrderId = seed.StockInOrderId,
            SourceDocumentNoSnapshot = "IN-001",
            BillDate = DateTime.UtcNow,
            DocumentAmount = 100m,
            PayableAmount = 100m,
            SettledAmount = 0m,
            BillStatus = SupplierBillStatus.Pending
        };
        var settlement = new SupplierSettlement
        {
            Id = Guid.NewGuid(),
            SettlementNo = "SS-VOIDED",
            SupplierId = seed.SupplierId,
            SupplierNameSnapshot = "供应商A",
            SettlementDate = DateTime.UtcNow,
            ShouldAmount = 100m,
            PaymentAmount = 100m,
            DiscountAmount = 0m,
            AppliedAmount = 100m,
            RemainingAmount = 100m,
            SettlementStatus = SupplierSettlementStatus.Voided
        };
        settlement.Details.Add(new SupplierSettlementDetail
        {
            Id = Guid.NewGuid(),
            SupplierSettlementId = settlement.Id,
            SupplierBillId = bill.Id,
            SupplierBillNoSnapshot = bill.BillNo,
            SourceType = SupplierBillSourceType.PurchaseStockIn,
            SourceDocumentNoSnapshot = bill.SourceDocumentNoSnapshot,
            PayableAmountSnapshot = 100m,
            PreviousSettledAmount = 0m,
            PaymentAmount = 100m,
            DiscountAmount = 0m,
            AppliedAmount = 100m,
            CurrentSettledAmount = 100m,
            RemainingAmount = 0m
        });
        await context.AddRangeAsync(bill, settlement);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);
        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.EnsureCanReverseSourceDocumentAsync(seed.StockInOrderId, null));
        Assert.Contains("已存在结算记录", exception.Message);
    }

    private static SupplierBillService CreateService(ApplicationDbContext context)
    {
        return new SupplierBillService(
            new SupplierBillRepository(context),
            new SupplierSettlementRepository(context),
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

    private static async Task<StockInSeed> SeedPurchaseStockInAsync(ApplicationDbContext context)
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "供应商A", Code = "SUP-A" };
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
        var stockIn = new StockInOrder
        {
            Id = Guid.NewGuid(),
            InNo = "IN-001",
            OrderType = StockInOrderType.Purchase,
            BusinessStatus = StockDocumentStatus.Audited,
            WareId = Guid.NewGuid(),
            WareNameSnapshot = "主仓",
            SupplierId = supplier.Id,
            SupplierNameSnapshot = supplier.Name,
            InTime = DateTime.UtcNow,
            AuditTime = DateTime.UtcNow,
            TotalAmount = 100m
        };
        stockIn.Details.Add(new StockInDetail
        {
            Id = Guid.NewGuid(),
            StockInOrderId = stockIn.Id,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            GoodsUnitId = unit.Id,
            GoodsUnitNameSnapshot = unit.Name,
            ConversionRate = 1m,
            Quantity = 10m,
            BaseQuantity = 10m,
            UnitPrice = 10m,
            TotalPrice = 100m
        });

        await context.AddRangeAsync(supplier, goodsType, goods, unit, stockIn);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new StockInSeed(supplier.Id, stockIn.Id);
    }

    private static async Task<StockOutSeed> SeedPurchaseReturnOutAsync(ApplicationDbContext context)
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "供应商A", Code = "SUP-A" };
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
        var stockOut = new StockOutOrder
        {
            Id = Guid.NewGuid(),
            OutNo = "OUT-001",
            OrderType = StockOutOrderType.PurchaseReturn,
            BusinessStatus = StockDocumentStatus.Audited,
            WareId = Guid.NewGuid(),
            WareNameSnapshot = "主仓",
            SupplierId = supplier.Id,
            SupplierNameSnapshot = supplier.Name,
            OutTime = DateTime.UtcNow,
            AuditTime = DateTime.UtcNow,
            TotalAmount = 30m
        };
        stockOut.Details.Add(new StockOutDetail
        {
            Id = Guid.NewGuid(),
            StockOutOrderId = stockOut.Id,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            GoodsUnitId = unit.Id,
            GoodsUnitNameSnapshot = unit.Name,
            ConversionRate = 1m,
            Quantity = 3m,
            BaseQuantity = 3m,
            UnitPrice = 10m,
            TotalPrice = 30m
        });

        await context.AddRangeAsync(supplier, goodsType, goods, unit, stockOut);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new StockOutSeed(stockOut.Id);
    }

    private sealed record StockInSeed(Guid SupplierId, Guid StockInOrderId);

    private sealed record StockOutSeed(Guid StockOutOrderId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("44444444-4444-4444-4444-444444444444");
        public string? GetUserName() => "finance-user";
        public string? GetEmail() => "finance@example.com";
        public string? GetRole() => "finance";
        public IReadOnlyList<string> GetRoles() => ["finance"];
        public bool HasClaim(string claimType, string claimValue) => false;
    }
}
