using Application.DTOs.Traceability;
using Application.interfaces;
using Application.Services;
using Application.Validator.Traceability;
using Domain.Entities.Goods;
using Domain.Entities.Customers;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Entities.Traceability;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Traceability;

/// <summary>验证检测报告会固化采购入库快照、约束送检数量，并在已被二维码溯源引用时保护报告历史。</summary>
public class TraceabilityServiceTests
{
    [Fact]
    public async Task CreateInspectionReportAsync_SnapshotsAuditedPurchaseStockInDetails()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedPurchaseStockInAsync(context);
        var service = CreateService(context);

        var result = await service.CreateInspectionReportAsync(new SaveInspectionReportDto
        {
            StockInOrderId = seed.StockInOrderId,
            InspectionOrg = "检测中心",
            InspectTime = DateTime.UtcNow,
            Conclusion = InspectionConclusion.Qualified,
            Goods = [new SaveInspectionReportGoodsDto
            {
                StockInDetailId = seed.StockInDetailId, SampleQuantity = 3.1234567m,
                Conclusion = InspectionConclusion.Qualified
            }],
            Attachments = [new SaveInspectionAttachmentDto
            {
                AttachmentType = InspectionAttachmentType.Report, FileName = "report.pdf", FileUrl = "/files/report.pdf", Sort = 0
            }]
        });

        Assert.StartsWith("IR", result.InspectionNo);
        Assert.Equal("采购入库-001", result.InNo);
        Assert.Equal("供应商A", result.SupplierName);
        Assert.Equal("叶菜", Assert.Single(result.Goods).GoodsTypeName);
        Assert.Equal(3.123457m, result.Goods[0].SampleQuantity);
        Assert.Single(result.Attachments);
    }

    [Fact]
    public async Task CreateInspectionReportAsync_RejectsSampleQuantityBeyondStockInQuantity()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedPurchaseStockInAsync(context);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CreateInspectionReportAsync(CreateRequest(seed.StockInOrderId, seed.StockInDetailId, 10.000001m)));

        Assert.Contains("不能超过来源入库数量", exception.Message);
        Assert.Empty(context.InspectionReports);
    }

    [Fact]
    public async Task DeleteInspectionReportAsync_RejectsReportReferencedByTraceRecord()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedPurchaseStockInAsync(context);
        var service = CreateService(context);
        var report = await service.CreateInspectionReportAsync(CreateRequest(seed.StockInOrderId, seed.StockInDetailId, 1m));
        context.TraceRecords.Add(new TraceRecord
        {
            Id = Guid.NewGuid(),
            TraceNo = "TR-001",
            SaleOrderId = Guid.NewGuid(),
            SaleOrderDetailId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            GoodsId = seed.GoodsId,
            SaleOrderNoSnapshot = "SO-001",
            CustomerNameSnapshot = "客户A",
            GoodsNameSnapshot = "青菜",
            GoodsCodeSnapshot = "VEG-001",
            InspectionReportId = report.Id
        });
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() => service.DeleteInspectionReportAsync(report.Id));

        Assert.Equal("报告已被溯源引用，不可删除", exception.Message);
        Assert.Single(context.InspectionReports);

        var updateException = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.UpdateInspectionReportAsync(report.Id, CreateRequest(seed.StockInOrderId, seed.StockInDetailId, 2m)));
        Assert.Equal("报告已被溯源引用，不可修改", updateException.Message);

        var qrCode = await service.GetTraceQrCodeAsync("TR-001");
        Assert.Equal("TR-001", qrCode.TraceRecord.TraceNo);
        Assert.DoesNotContain("CustomerName", typeof(PublicTraceQrRecordDto).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("SaleOrderId", typeof(PublicTraceQrRecordDto).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("Id", typeof(PublicInspectionReportDto).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("StockInOrderId", typeof(PublicInspectionReportDto).GetProperties().Select(x => x.Name));
        Assert.DoesNotContain("StockInDetailId", typeof(PublicInspectionReportGoodsDto).GetProperties().Select(x => x.Name));
    }

    [Fact]
    public async Task GenerateSaleOrderTracesAsync_GeneratesOneTraceFromAuditedSalesBatch()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedPurchaseStockInAsync(context);
        var stockInDetail = await context.StockInDetails.SingleAsync(x => x.Id == seed.StockInDetailId);
        var goods = await context.Goods.SingleAsync(x => x.Id == seed.GoodsId);
        var ware = await context.Wares.SingleAsync();
        var customer = new Customer { Id = Guid.NewGuid(), Name = "客户A", Code = "CUS-01" };
        var saleOrder = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO-001",
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            CustomerCodeSnapshot = customer.Code,
            OrderDate = DateTime.UtcNow,
            OrderStatus = SaleOrderStatus.SortingPending
        };
        var saleDetail = new SaleOrderDetail
        {
            Id = Guid.NewGuid(),
            SaleOrderId = saleOrder.Id,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            GoodsTypeNameSnapshot = "叶菜",
            GoodsUnitId = stockInDetail.GoodsUnitId,
            GoodsUnitNameSnapshot = stockInDetail.GoodsUnitNameSnapshot,
            Quantity = 2m,
            BaseQuantity = 2m,
            UnitConversion = 1m,
            FixedPrice = 3m,
            TotalPrice = 6m
        };
        var batch = new StockBatch
        {
            Id = Guid.NewGuid(),
            WareId = ware.Id,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            BatchNo = stockInDetail.BatchNo,
            BaseUnitId = stockInDetail.GoodsUnitId,
            BaseUnitNameSnapshot = stockInDetail.GoodsUnitNameSnapshot,
            CurrentQuantity = 8m,
            AvailableQuantity = 8m
        };
        stockInDetail.StockBatchId = batch.Id;
        var stockOut = new StockOutOrder
        {
            Id = Guid.NewGuid(),
            OutNo = "OUT-001",
            OrderType = StockOutOrderType.Sale,
            BusinessStatus = StockDocumentStatus.Audited,
            WareId = ware.Id,
            WareNameSnapshot = ware.Name,
            SaleOrderId = saleOrder.Id,
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            OutTime = DateTime.UtcNow,
            Details = [new StockOutDetail
            {
                Id = Guid.NewGuid(), SaleOrderDetailId = saleDetail.Id, StockBatchId = batch.Id, GoodsId = goods.Id,
                GoodsNameSnapshot = goods.Name, GoodsCodeSnapshot = goods.Code, GoodsUnitId = stockInDetail.GoodsUnitId,
                GoodsUnitNameSnapshot = stockInDetail.GoodsUnitNameSnapshot, ConversionRate = 1m, Quantity = 2m,
                BaseQuantity = 2m, UnitPrice = 3m, TotalPrice = 6m, BatchNoSnapshot = batch.BatchNo
            }]
        };
        await context.AddRangeAsync(customer, saleOrder, saleDetail, batch, stockOut);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var traces = await CreateService(context).GenerateSaleOrderTracesAsync(saleOrder.Id);

        var trace = Assert.Single(traces);
        Assert.StartsWith("TR", trace.TraceNo);
        Assert.Equal("SO-001", trace.SaleOrderNo);
        Assert.Equal("BATCH-01", trace.BatchNo);
        Assert.Equal("供应商A", trace.SupplierName);

        var retry = await CreateService(context).GenerateSaleOrderTracesAsync(saleOrder.Id);
        Assert.Single(retry);
        Assert.Equal(trace.TraceNo, retry[0].TraceNo);

        var untraceableDetail = new SaleOrderDetail
        {
            Id = Guid.NewGuid(), SaleOrderId = saleOrder.Id, GoodsId = goods.Id, GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code, GoodsUnitId = stockInDetail.GoodsUnitId,
            GoodsUnitNameSnapshot = stockInDetail.GoodsUnitNameSnapshot, Quantity = 1m, BaseQuantity = 1m,
            UnitConversion = 1m, FixedPrice = 3m, TotalPrice = 3m
        };
        var untraceableBatch = new StockBatch
        {
            Id = Guid.NewGuid(), WareId = ware.Id, GoodsId = goods.Id, GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code, BatchNo = "BATCH-NO-SOURCE", BaseUnitId = stockInDetail.GoodsUnitId,
            BaseUnitNameSnapshot = stockInDetail.GoodsUnitNameSnapshot, CurrentQuantity = 1m, AvailableQuantity = 1m
        };
        context.StockOutDetails.Add(new StockOutDetail
        {
            Id = Guid.NewGuid(), StockOutOrderId = stockOut.Id, SaleOrderDetailId = untraceableDetail.Id,
            StockBatchId = untraceableBatch.Id, GoodsId = goods.Id, GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code, GoodsUnitId = stockInDetail.GoodsUnitId,
            GoodsUnitNameSnapshot = stockInDetail.GoodsUnitNameSnapshot, ConversionRate = 1m, Quantity = 1m,
            BaseQuantity = 1m, UnitPrice = 3m, TotalPrice = 3m, BatchNoSnapshot = untraceableBatch.BatchNo
        });
        await context.AddRangeAsync(untraceableDetail, untraceableBatch);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var sourceException = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            CreateService(context).GenerateSaleOrderTracesAsync(saleOrder.Id));
        Assert.Contains("缺少或存在多个已审核采购入库来源", sourceException.Message);
    }

    private static SaveInspectionReportDto CreateRequest(Guid stockInOrderId, Guid stockInDetailId, decimal quantity) => new()
    {
        StockInOrderId = stockInOrderId,
        InspectionOrg = "检测中心",
        InspectTime = DateTime.UtcNow,
        Conclusion = InspectionConclusion.Qualified,
        Goods = [new SaveInspectionReportGoodsDto { StockInDetailId = stockInDetailId, SampleQuantity = quantity, Conclusion = InspectionConclusion.Qualified }]
    };

    private static TraceabilityService CreateService(ApplicationDbContext context) => new(
        new InspectionReportRepository(context), new TraceRecordRepository(context), new ExternalPushLogRepository(context),
        new StockInOrderRepository(context), new SavingUnitOfWork(context), new FakeCurrentUserService(), new SaveInspectionReportValidator());

    private static ApplicationDbContext CreateDbContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<TraceabilitySeed> SeedAuditedPurchaseStockInAsync(ApplicationDbContext context)
    {
        var type = new GoodsType { Id = Guid.NewGuid(), Name = "叶菜", Code = "VEG" };
        var goods = new GoodsEntity { Id = Guid.NewGuid(), Name = "青菜", Code = "VEG-001", GoodsTypeId = type.Id, GoodsType = type };
        var unit = new GoodsUnit { Id = Guid.NewGuid(), GoodsId = goods.Id, Name = "公斤", ConversionRate = 1m, IsBaseUnit = true, Goods = goods };
        var ware = new Ware { Id = Guid.NewGuid(), Name = "主仓", Code = "W-01" };
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "供应商A", Code = "SUP-01" };
        var order = new StockInOrder
        {
            Id = Guid.NewGuid(),
            InNo = "采购入库-001",
            OrderType = StockInOrderType.Purchase,
            BusinessStatus = StockDocumentStatus.Audited,
            WareId = ware.Id,
            WareNameSnapshot = ware.Name,
            SupplierId = supplier.Id,
            SupplierNameSnapshot = supplier.Name,
            InTime = DateTime.UtcNow,
            AuditTime = DateTime.UtcNow,
            Details = [new StockInDetail
            {
                Id = Guid.NewGuid(), GoodsId = goods.Id, GoodsNameSnapshot = goods.Name, GoodsCodeSnapshot = goods.Code,
                GoodsUnitId = unit.Id, GoodsUnitNameSnapshot = unit.Name, ConversionRate = 1m, Quantity = 10m,
                BaseQuantity = 10m, UnitPrice = 2m, TotalPrice = 20m, BatchNo = "BATCH-01", Goods = goods, GoodsUnit = unit
            }]
        };
        await context.AddRangeAsync(type, goods, unit, ware, supplier, order);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new TraceabilitySeed(order.Id, order.Details.Single().Id, goods.Id);
    }

    private sealed record TraceabilitySeed(Guid StockInOrderId, Guid StockInDetailId, Guid GoodsId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("77777777-7777-7777-7777-777777777777");
        public string? GetUserName() => "trace-user";
        public string? GetEmail() => null;
        public string? GetRole() => null;
        public IReadOnlyList<string> GetRoles() => [];
        public bool HasClaim(string claimType, string claimValue) => false;
    }

    private sealed class SavingUnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        public bool HasActiveTransaction { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) { HasActiveTransaction = true; return Task.CompletedTask; }
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default) { await context.SaveChangesAsync(cancellationToken); HasActiveTransaction = false; }
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) { context.ChangeTracker.Clear(); HasActiveTransaction = false; return Task.CompletedTask; }
        public Task<int> ExecuteSqlAsync(string sql, params object[] parameters) => throw new NotSupportedException();
        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default) { await BeginTransactionAsync(cancellationToken); try { await action(); await CommitTransactionAsync(cancellationToken); } catch { await RollbackTransactionAsync(cancellationToken); throw; } }
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) { await BeginTransactionAsync(cancellationToken); try { var result = await action(); await CommitTransactionAsync(cancellationToken); return result; } catch { await RollbackTransactionAsync(cancellationToken); throw; } }
        public void ClearChangeTracking() => context.ChangeTracker.Clear();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
