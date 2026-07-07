using Application.DTOs.AfterSales;
using Application.DTOs.Storage;
using Application.Mappers;
using Application.Services;
using Application.Validator;
using Application.Validator.AfterSales;
using Application.interfaces;
using AutoMapper;
using Domain.Entities.AfterSales;
using Domain.Entities.Customers;
using Domain.Entities.Delivery;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.AfterSales;

/// <summary>
/// 验证售后审核、取货履约、销售退货入库和库存流水的跨服务完整链路。
/// </summary>
public class AfterSaleStockIntegrationTests
{
    [Fact]
    public async Task ReturnAndRefund_FlowsFromApprovalToIdempotentSalesReturnInbound()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var mapper = CreateMapper();
        var afterSaleService = CreateAfterSaleService(context, unitOfWork, mapper);
        var pickupTaskService = CreatePickupTaskService(context, unitOfWork, mapper);
        var stockInService = CreateStockInService(context, unitOfWork, mapper);

        var created = await afterSaleService.CreateAsync(new CreateAfterSaleDto
        {
            SaleOrderId = seed.SaleOrderId,
            CustomerId = seed.CustomerId,
            Source = "客户退货",
            ContactName = "王老师",
            ContactPhone = "13800000000",
            PickupAddress = "学校正门",
            Goods =
            [
                new CreateAfterSaleGoodsDto
                {
                    SaleOrderDetailId = seed.SaleOrderDetailId,
                    ActualRefundQuantity = 2m,
                    AfterSaleType = AfterSaleType.ReturnAndRefund,
                    ReasonType = AfterSaleReasonType.QualityIssue,
                    HandleType = AfterSaleHandleType.GoodsDiscount
                }
            ]
        });
        await afterSaleService.SubmitAsync(created.Id, "提交");
        var approved = await afterSaleService.ApproveAsync(created.Id, "同意退货");
        var task = Assert.Single(approved.PickupTasks);

        await pickupTaskService.AssignAsync(task.Id, new AssignPickupTaskDto
        {
            DriverId = seed.DriverId,
            PlannedPickupTime = new DateTime(2026, 7, 7, 8, 30, 0, DateTimeKind.Utc)
        });
        await pickupTaskService.StartAsync(task.Id);
        var completedTask = await pickupTaskService.CompleteAsync(task.Id);
        var prematureCompletion = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            afterSaleService.CompleteAsync(created.Id));
        Assert.Contains("销售退货入库", prematureCompletion.Message);

        var request = new CreateSalesReturnStockInDto
        {
            AfterSaleId = created.Id,
            WareId = seed.WareId,
            CustomerId = seed.CustomerId,
            InTime = new DateTime(2026, 7, 7, 9, 0, 0, DateTimeKind.Utc),
            Remark = "售后取货入库",
            Details =
            [
                new CreateStockInDetailDto
                {
                    PickupTaskId = task.Id,
                    GoodsId = seed.GoodsId,
                    GoodsUnitId = seed.GoodsUnitId,
                    Quantity = 2m,
                    UnitPrice = 5m,
                    BatchNo = "RETURN-20260707"
                }
            ]
        };
        var stockIn = await stockInService.CreateSalesReturnAsync(request);
        var repeated = await stockInService.CreateSalesReturnAsync(request);
        request.Details[0].BatchNo = "RETURN-CHANGED";
        var divergentRetry = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            stockInService.CreateSalesReturnAsync(request));
        request.Details[0].BatchNo = "RETURN-20260707";
        var audited = await stockInService.AuditAsync(StockInOrderType.SalesReturn, stockIn.Id, "质检通过");
        var completedAfterSale = await afterSaleService.CompleteAsync(created.Id);
        var reverseAfterCompletion = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            stockInService.ReverseAuditAsync(StockInOrderType.SalesReturn, stockIn.Id, "错误反审核"));

        Assert.Equal(stockIn.Id, repeated.Id);
        Assert.Contains("与原销售退货入库单不一致", divergentRetry.Message);
        Assert.Equal(created.Id, audited.AfterSaleId);
        Assert.Equal(task.Id, Assert.Single(audited.Details).PickupTaskId);
        Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
        Assert.Equal(PickupTaskStatus.Completed, completedTask.PickupStatus);
        Assert.Equal(AfterSaleStatus.Completed, completedAfterSale.AfterStatus);
        Assert.Contains("来源售后单已完成", reverseAfterCompletion.Message);
        Assert.Equal(1, await context.StockInOrders.CountAsync());
        Assert.Equal(2m, Assert.Single(await context.StockBatches.ToListAsync()).CurrentQuantity);
        var ledger = Assert.Single(await context.StockLedgers.ToListAsync());
        Assert.Equal(StockLedgerSourceType.SalesReturnInbound, ledger.SourceType);
        var linkedTask = await new PickupTaskRepository(context).GetByIdAsync(task.Id);
        Assert.Equal(stockIn.Id, linkedTask!.StockInDetail!.StockInOrderId);
    }

    [Fact]
    public async Task SalesReturnInbound_RejectsIncompleteAndMismatchedPickupTask()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAsync(context);
        var afterSale = new AfterSale
        {
            Id = Guid.NewGuid(),
            AfterSaleNo = "AS-LINK-001",
            CustomerId = seed.CustomerId,
            CustomerNameSnapshot = "学校客户",
            Source = "测试",
            AfterStatus = AfterSaleStatus.ReturnPending,
            PickupAddressSnapshot = "学校正门"
        };
        var goods = new AfterSaleGoods
        {
            Id = Guid.NewGuid(),
            AfterSaleId = afterSale.Id,
            GoodsId = seed.GoodsId,
            GoodsNameSnapshot = "番茄",
            GoodsCodeSnapshot = "TOMATO",
            GoodsUnitId = seed.GoodsUnitId,
            GoodsUnitNameSnapshot = "千克",
            BaseUnitId = seed.GoodsUnitId,
            BaseUnitNameSnapshot = "千克",
            ConversionRate = 1m,
            AfterSaleType = AfterSaleType.ReturnAndRefund,
            ActualRefundQuantity = 2m,
            BaseRefundQuantity = 2m,
            UnitPrice = 5m,
            ReasonType = AfterSaleReasonType.QualityIssue,
            HandleType = AfterSaleHandleType.GoodsDiscount
        };
        var task = new PickupTask
        {
            Id = Guid.NewGuid(),
            TaskNo = "PU-LINK-001",
            AfterSaleId = afterSale.Id,
            AfterSaleGoodsId = goods.Id,
            PickupAddressSnapshot = "学校正门",
            PickupStatus = PickupTaskStatus.PickingUp
        };
        afterSale.Goods.Add(goods);
        afterSale.PickupTasks.Add(task);
        await context.AfterSales.AddAsync(afterSale);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = CreateStockInService(context, new RecordingUnitOfWork(context), CreateMapper());

        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CreateSalesReturnAsync(new CreateSalesReturnStockInDto
            {
                AfterSaleId = afterSale.Id,
                WareId = seed.WareId,
                CustomerId = seed.CustomerId,
                InTime = DateTime.UtcNow,
                Details =
                [
                    new CreateStockInDetailDto
                    {
                        PickupTaskId = task.Id,
                        GoodsId = seed.GoodsId,
                        GoodsUnitId = seed.GoodsUnitId,
                        Quantity = 1m,
                        UnitPrice = 5m,
                        BatchNo = "INVALID"
                    }
                ]
            }));

        Assert.Contains("尚未完成", exception.Message);
        Assert.Empty(context.StockInOrders);

        var persistedTask = await context.PickupTasks.SingleAsync(x => x.Id == task.Id);
        persistedTask.PickupStatus = PickupTaskStatus.Completed;
        persistedTask.CompletedTime = DateTime.UtcNow;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var quantityException = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CreateSalesReturnAsync(new CreateSalesReturnStockInDto
            {
                AfterSaleId = afterSale.Id,
                WareId = seed.WareId,
                CustomerId = seed.CustomerId,
                InTime = DateTime.UtcNow,
                Details =
                [
                    new CreateStockInDetailDto
                    {
                        PickupTaskId = task.Id,
                        GoodsId = seed.GoodsId,
                        GoodsUnitId = seed.GoodsUnitId,
                        Quantity = 1m,
                        UnitPrice = 5m,
                        BatchNo = "INVALID"
                    }
                ]
            }));

        Assert.Contains("批准退货数量不一致", quantityException.Message);
        Assert.Empty(context.StockInOrders);
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(config =>
        {
            config.AddProfile<AfterSaleMappingProfile>();
            config.AddProfile<StockInMappingProfile>();
        }).CreateMapper();
    }

    private static AfterSaleService CreateAfterSaleService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        var goodsValidator = new CreateAfterSaleGoodsValidator();
        return new AfterSaleService(
            new AfterSaleRepository(context),
            new AfterSaleGoodsRepository(context),
            new AfterSaleAuditLogRepository(context),
            new PickupTaskRepository(context),
            new StockInOrderRepository(context),
            new SaleOrderRepository(context),
            new CustomerBillService(
                new CustomerBillRepository(context),
                new AfterSaleRepository(context),
                new FakeCurrentUserService()),
            new CustomerRepository(context),
            new GoodsRepository(context),
            new GoodsUnitRepository(context),
            new SupplierRepository(context),
            new DepartmentRepository(context),
            unitOfWork,
            mapper,
            new FakeCurrentUserService(),
            new CreateAfterSaleValidator(goodsValidator),
            new UpdateAfterSaleValidator(goodsValidator),
            NullLogger<AfterSaleService>.Instance);
    }

    private static PickupTaskService CreatePickupTaskService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        return new PickupTaskService(
            new PickupTaskRepository(context),
            new DriverRepository(context),
            unitOfWork,
            mapper,
            new FakeCurrentUserService(),
            new AssignPickupTaskValidator(),
            NullLogger<PickupTaskService>.Instance);
    }

    private static StockInService CreateStockInService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        return new StockInService(
            new StockInOrderRepository(context),
            new StockBatchRepository(context),
            new StockLedgerRepository(context),
            new WareRepository(context),
            new SupplierRepository(context),
            new PurchaserRepository(context),
            new CustomerRepository(context),
            new DepartmentRepository(context),
            new PurchaseOrderRepository(context),
            new PickupTaskRepository(context),
            new AfterSaleRepository(context),
            new SupplierBillService(
                new SupplierBillRepository(context),
                new SupplierSettlementRepository(context),
                new FakeCurrentUserService()),
            new GoodsRepository(context),
            new GoodsUnitRepository(context),
            unitOfWork,
            mapper,
            new FakeCurrentUserService(),
            new CreatePurchaseStockInValidator(),
            new UpdatePurchaseStockInValidator(),
            new CreateOtherStockInValidator(),
            new UpdateOtherStockInValidator(),
            new CreateSalesReturnStockInValidator(),
            new UpdateSalesReturnStockInValidator(),
            NullLogger<StockInService>.Instance);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<IntegrationSeed> SeedAsync(ApplicationDbContext context)
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
        var ware = new Ware { Id = Guid.NewGuid(), Name = "退货仓", Code = "RETURN" };
        var driver = new Driver { Id = Guid.NewGuid(), Name = "张师傅", Code = "DRIVER-01", Phone = "13900000000" };
        var order = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO-LINK-001",
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            CustomerCodeSnapshot = customer.Code,
            OrderDate = DateTime.UtcNow,
            OrderStatus = SaleOrderStatus.Signed,
            OrderPrice = 50m,
            SettlementPrice = 50m
        };
        var detail = new SaleOrderDetail
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
            FixedPrice = 5m,
            FixedGoodsUnitId = unit.Id,
            FixedGoodsUnitNameSnapshot = unit.Name,
            TotalPrice = 50m,
            CustomerCheckBaseQuantity = 10m
        };
        order.Details.Add(detail);
        await context.AddRangeAsync(customer, goodsType, goods, unit, ware, driver, order);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new IntegrationSeed(customer.Id, goods.Id, unit.Id, ware.Id, driver.Id, order.Id, detail.Id);
    }

    private sealed record IntegrationSeed(
        Guid CustomerId,
        Guid GoodsId,
        Guid GoodsUnitId,
        Guid WareId,
        Guid DriverId,
        Guid SaleOrderId,
        Guid SaleOrderDetailId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string? GetUserName() => "integration-user";
        public string? GetEmail() => "integration@example.com";
        public string? GetRole() => "operator";
        public IReadOnlyList<string> GetRoles() => ["operator"];
        public bool HasClaim(string claimType, string claimValue) => false;
    }

    private sealed class RecordingUnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        public bool HasActiveTransaction { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = true;
            return Task.CompletedTask;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            await context.SaveChangesAsync(cancellationToken);
            HasActiveTransaction = false;
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            context.ChangeTracker.Clear();
            HasActiveTransaction = false;
            return Task.CompletedTask;
        }

        public Task<int> ExecuteSqlAsync(string sql, params object[] parameters) => throw new NotSupportedException();
        public void ClearChangeTracking() => context.ChangeTracker.Clear();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
