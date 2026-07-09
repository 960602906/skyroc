using Application.DTOs.Storage;
using Application.Exceptions;
using Application.interfaces;
using Application.Mappers;
using Application.QueryParameters;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Delivery;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Storage;

public class StockOutServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("10000000-0000-0000-0000-000000000022");

    [Fact]
    public async Task CreateSaleAsync_persists_batch_snapshots_and_converts_to_base_quantity()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));

        var result = await service.CreateSaleAsync(SaleRequest(seed, 2m, seed.CaseUnitId));

        Assert.StartsWith("OUT", result.OutNo);
        Assert.Equal(StockOutOrderType.Sale, result.OrderType);
        Assert.Equal(StockDocumentStatus.Draft, result.BusinessStatus);
        Assert.Equal("中心仓", result.WareName);
        Assert.Equal("零售客户", result.CustomerName);
        Assert.Equal(CurrentUserId, result.CreateBy);
        var detail = Assert.Single(result.Details);
        Assert.Equal(seed.BatchId, detail.StockBatchId);
        Assert.Equal("BATCH-001", detail.BatchNo);
        Assert.Equal(24m, detail.BaseQuantity);
        Assert.Equal(48m, detail.TotalPrice);
        Assert.Empty(context.StockLedgers);
    }

    [Fact]
    public async Task AuditAsync_decreases_batch_and_appends_costed_outbound_ledger()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context, batchQuantity: 30m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreateSaleAsync(SaleRequest(seed, 8m));

        var audited = await service.AuditAsync(StockOutOrderType.Sale, created.Id, "销售出库");

        Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
        Assert.Equal(CurrentUserId, audited.AuditUserId);
        var batch = await context.StockBatches.SingleAsync();
        Assert.Equal(22m, batch.CurrentQuantity);
        Assert.Equal(22m, batch.AvailableQuantity);
        Assert.Equal(5m, batch.UnitCost);
        var ledger = await context.StockLedgers.SingleAsync();
        Assert.Equal(StockLedgerDirection.Decrease, ledger.Direction);
        Assert.Equal(StockLedgerSourceType.SalesOutbound, ledger.SourceType);
        Assert.Equal(8m, ledger.ChangeQuantity);
        Assert.Equal(22m, ledger.BalanceQuantity);
        Assert.Equal(40m, ledger.TotalCost);
    }

    [Fact]
    public async Task AuditAsync_rejects_insufficient_available_stock_without_partial_changes()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context, batchQuantity: 5m);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreateService(context, unitOfWork);
        var created = await service.CreateSaleAsync(SaleRequest(seed, 6m));

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.AuditAsync(StockOutOrderType.Sale, created.Id, null));

        Assert.Contains("可用库存不足", exception.Message);
        var batch = await context.StockBatches.SingleAsync();
        Assert.Equal(5m, batch.CurrentQuantity);
        Assert.Empty(context.StockLedgers);
        Assert.Equal(StockDocumentStatus.Draft,
            (await context.StockOutOrders.SingleAsync()).BusinessStatus);
        Assert.Equal(1, unitOfWork.RollbackCount);
    }

    [Fact]
    public async Task ReverseAuditAsync_restores_stock_with_append_only_reversal_ledger()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreateOtherAsync(OtherRequest(seed, 7m));
        await service.AuditAsync(StockOutOrderType.Other, created.Id, null);

        var reversed = await service.ReverseAuditAsync(StockOutOrderType.Other, created.Id, "撤销错误出库");

        Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);
        Assert.Equal(CurrentUserId, reversed.ReverseUserId);
        var batch = await context.StockBatches.SingleAsync();
        Assert.Equal(30m, batch.CurrentQuantity);
        Assert.Equal(30m, batch.AvailableQuantity);
        Assert.Equal(2, await context.StockLedgers.CountAsync());
        var reversal = await context.StockLedgers.SingleAsync(x => x.ReversedFromLedgerId != null);
        Assert.Equal(StockLedgerDirection.Increase, reversal.Direction);
        Assert.Equal(7m, reversal.ChangeQuantity);
        Assert.Equal(30m, reversal.BalanceQuantity);
    }

    [Fact]
    public async Task ReverseAuditAsync_rejects_second_reversal()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreateOtherAsync(OtherRequest(seed, 2m));
        await service.AuditAsync(StockOutOrderType.Other, created.Id, null);
        await service.ReverseAuditAsync(StockOutOrderType.Other, created.Id, null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ReverseAuditAsync(StockOutOrderType.Other, created.Id, null));

        Assert.Contains("未处于已审核状态", exception.Message);
        Assert.Equal(2, await context.StockLedgers.CountAsync());
    }

    [Fact]
    public async Task SaleOutbound_updates_source_order_from_partial_to_generated_and_back_on_reverse()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context, batchQuantity: 30m);
        var source = await SeedSaleOrderAsync(context, seed, SaleOrderStatus.SortingPending, 10m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var firstOutTime = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc);
        var secondOutTime = new DateTime(2026, 7, 5, 10, 0, 0, DateTimeKind.Utc);
        var firstRequest = SaleRequest(
            seed,
            4m,
            saleOrderId: source.OrderId,
            saleOrderDetailId: source.DetailId,
            outTime: firstOutTime);
        var first = await service.CreateSaleAsync(firstRequest);
        await service.AuditAsync(StockOutOrderType.Sale, first.Id, null);

        var partiallyGenerated = await context.SaleOrders.SingleAsync(x => x.Id == source.OrderId);
        Assert.True(partiallyGenerated.HasOutSale);
        Assert.Equal(OrderOutStorageStatus.PartiallyGenerated, partiallyGenerated.OutStorageStatus);
        Assert.Equal(firstOutTime, partiallyGenerated.OutDate);

        var secondRequest = SaleRequest(
            seed,
            6m,
            saleOrderId: source.OrderId,
            saleOrderDetailId: source.DetailId,
            outTime: secondOutTime);
        var second = await service.CreateSaleAsync(secondRequest);
        await service.AuditAsync(StockOutOrderType.Sale, second.Id, null);

        var generated = await context.SaleOrders.SingleAsync(x => x.Id == source.OrderId);
        Assert.Equal(OrderOutStorageStatus.Generated, generated.OutStorageStatus);
        Assert.Equal(secondOutTime, generated.OutDate);

        await service.ReverseAuditAsync(StockOutOrderType.Sale, second.Id, null);

        var reverted = await context.SaleOrders.SingleAsync(x => x.Id == source.OrderId);
        Assert.True(reverted.HasOutSale);
        Assert.Equal(OrderOutStorageStatus.PartiallyGenerated, reverted.OutStorageStatus);
        Assert.Equal(firstOutTime, reverted.OutDate);
    }

    [Fact]
    public async Task ReverseAuditAsync_RejectsSaleOutboundThatHasGeneratedDeliveryTask()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context, batchQuantity: 30m);
        var source = await SeedSaleOrderAsync(context, seed, SaleOrderStatus.SortingPending, 10m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var outbound = await service.CreateSaleAsync(SaleRequest(
            seed,
            10m,
            saleOrderId: source.OrderId,
            saleOrderDetailId: source.DetailId));
        await service.AuditAsync(StockOutOrderType.Sale, outbound.Id, null);
        await context.DeliveryTasks.AddAsync(new DeliveryTask
        {
            Id = Guid.NewGuid(),
            TaskNo = "DT20260704001",
            StockOutOrderId = outbound.Id,
            SaleOrderId = source.OrderId,
            CustomerId = seed.CustomerId,
            CustomerNameSnapshot = "零售客户",
            WareId = seed.WareId,
            WareNameSnapshot = "中心仓",
            OutTime = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ReverseAuditAsync(StockOutOrderType.Sale, outbound.Id, null));

        Assert.Contains("已生成配送任务", exception.Message);
        Assert.Equal(StockDocumentStatus.Audited, (await context.StockOutOrders.SingleAsync()).BusinessStatus);
        Assert.Single(await context.StockLedgers.ToListAsync());
    }

    [Fact]
    public async Task CreateSaleAsync_rejects_unapproved_source_order()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var source = await SeedSaleOrderAsync(context, seed, SaleOrderStatus.PendingAudit, 10m);
        var service = CreateService(context, new RecordingUnitOfWork(context));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateSaleAsync(
            SaleRequest(seed, 2m, saleOrderId: source.OrderId, saleOrderDetailId: source.DetailId)));

        Assert.Contains("未审核通过", exception.Message);
        Assert.Empty(context.StockOutOrders);
    }

    [Fact]
    public async Task AuditAsync_rechecks_source_quantity_and_rejects_over_outbound()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context, batchQuantity: 30m);
        var source = await SeedSaleOrderAsync(context, seed, SaleOrderStatus.SortingPending, 10m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var first = await service.CreateSaleAsync(
            SaleRequest(seed, 7m, saleOrderId: source.OrderId, saleOrderDetailId: source.DetailId));
        var second = await service.CreateSaleAsync(
            SaleRequest(seed, 7m, saleOrderId: source.OrderId, saleOrderDetailId: source.DetailId));
        await service.AuditAsync(StockOutOrderType.Sale, first.Id, null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.AuditAsync(StockOutOrderType.Sale, second.Id, null));

        Assert.Contains("超过剩余可出库数量", exception.Message);
        Assert.Equal(23m, (await context.StockBatches.SingleAsync()).AvailableQuantity);
        Assert.Single(context.StockLedgers);
    }

    [Fact]
    public async Task PurchaseReturn_and_other_audits_use_distinct_source_types()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var purchaseReturn = await service.CreatePurchaseReturnAsync(PurchaseReturnRequest(seed, 3m));
        await service.AuditAsync(StockOutOrderType.PurchaseReturn, purchaseReturn.Id, null);
        var other = await service.CreateOtherAsync(OtherRequest(seed, 2m));
        await service.AuditAsync(StockOutOrderType.Other, other.Id, null);

        Assert.Contains(context.StockLedgers, x => x.SourceType == StockLedgerSourceType.PurchaseReturnOutbound);
        Assert.Contains(context.StockLedgers, x => x.SourceType == StockLedgerSourceType.OtherOutbound);
        Assert.Equal(25m, (await context.StockBatches.SingleAsync()).AvailableQuantity);
    }

    [Fact]
    public async Task UpdateOtherAsync_replaces_details_and_delete_removes_editable_order()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var orderRepository = new TrackingStockOutOrderRepository(context, () => unitOfWork.HasActiveTransaction);
        var service = CreateService(context, unitOfWork, orderRepository);
        var created = await service.CreateOtherAsync(OtherRequest(seed, 2m));

        var updated = await service.UpdateOtherAsync(new UpdateOtherStockOutDto
        {
            Id = created.Id,
            WareId = seed.WareId,
            OutTime = new DateTime(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc),
            Remark = "调整数量",
            Details =
            [
                new UpdateStockOutDetailDto
                {
                    Id = Assert.Single(created.Details).Id,
                    StockBatchId = seed.BatchId,
                    GoodsUnitId = seed.BaseUnitId,
                    Quantity = 5m,
                    UnitPrice = 4m
                }
            ]
        });

        Assert.Equal(5m, Assert.Single(updated.Details).Quantity);
        Assert.Equal(20m, updated.TotalAmount);
        Assert.True(orderRepository.TransactionWasActiveWhenLocked);
        Assert.Equal(1, orderRepository.LockCount);
        Assert.True(await service.DeleteAsync(StockOutOrderType.Other, created.Id));
        Assert.True(orderRepository.TransactionWasActiveWhenLocked);
        Assert.Equal(2, orderRepository.LockCount);
        Assert.Empty(context.StockOutOrders);
    }

    [Fact]
    public async Task DeleteAsync_rejects_audited_order()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreateOtherAsync(OtherRequest(seed, 1m));
        await service.AuditAsync(StockOutOrderType.Other, created.Id, null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.DeleteAsync(StockOutOrderType.Other, created.Id));

        Assert.Contains("不能删除", exception.Message);
    }

    [Fact]
    public async Task GetPagedAsync_isolates_outbound_types()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        await service.CreateSaleAsync(SaleRequest(seed, 1m));
        await service.CreateOtherAsync(OtherRequest(seed, 1m));

        var result = await service.GetPagedAsync(
            StockOutOrderType.Sale,
            new StockOutOrderQueryParameters { Current = 1, Size = 10 });

        Assert.Equal(1, result.Total);
        Assert.Equal(StockOutOrderType.Sale, Assert.Single(result.Records!).OrderType);
    }

    [Fact]
    public async Task AuditAsync_locks_order_source_and_batch_inside_transaction()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var source = await SeedSaleOrderAsync(context, seed, SaleOrderStatus.SortingPending, 10m);
        var unitOfWork = new RecordingUnitOfWork(context);
        var orderRepository = new TrackingStockOutOrderRepository(context, () => unitOfWork.HasActiveTransaction);
        var batchRepository = new TrackingStockBatchRepository(context, () => unitOfWork.HasActiveTransaction);
        var saleRepository = new TrackingSaleOrderRepository(context, () => unitOfWork.HasActiveTransaction);
        var service = CreateService(context, unitOfWork, orderRepository, batchRepository, saleRepository);
        var created = await service.CreateSaleAsync(
            SaleRequest(seed, 2m, saleOrderId: source.OrderId, saleOrderDetailId: source.DetailId));

        await service.AuditAsync(StockOutOrderType.Sale, created.Id, null);

        Assert.True(orderRepository.TransactionWasActiveWhenLocked);
        Assert.True(batchRepository.TransactionWasActiveWhenLocked);
        Assert.True(saleRepository.TransactionWasActiveWhenLocked);
    }

    private static CreateSaleStockOutDto SaleRequest(
        CatalogSeed seed,
        decimal quantity,
        Guid? unitId = null,
        Guid? saleOrderId = null,
        Guid? saleOrderDetailId = null,
        DateTime? outTime = null)
    {
        return new CreateSaleStockOutDto
        {
            WareId = seed.WareId,
            SaleOrderId = saleOrderId,
            CustomerId = seed.CustomerId,
            OutTime = outTime ?? new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            Remark = "销售出库",
            Details =
            [
                new CreateStockOutDetailDto
                {
                    SaleOrderDetailId = saleOrderDetailId,
                    StockBatchId = seed.BatchId,
                    GoodsUnitId = unitId ?? seed.BaseUnitId,
                    Quantity = quantity,
                    UnitPrice = 24m
                }
            ]
        };
    }

    private static CreatePurchaseReturnStockOutDto PurchaseReturnRequest(CatalogSeed seed, decimal quantity)
    {
        return new CreatePurchaseReturnStockOutDto
        {
            WareId = seed.WareId,
            SupplierId = seed.SupplierId,
            OutTime = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            Remark = "采购退货",
            Details = [DetailRequest(seed, quantity)]
        };
    }

    private static CreateOtherStockOutDto OtherRequest(CatalogSeed seed, decimal quantity)
    {
        return new CreateOtherStockOutDto
        {
            WareId = seed.WareId,
            OutTime = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            Remark = "其他出库",
            Details = [DetailRequest(seed, quantity)]
        };
    }

    private static CreateStockOutDetailDto DetailRequest(CatalogSeed seed, decimal quantity)
    {
        return new CreateStockOutDetailDto
        {
            StockBatchId = seed.BatchId,
            GoodsUnitId = seed.BaseUnitId,
            Quantity = quantity,
            UnitPrice = 4m
        };
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static StockOutService CreateService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IStockOutOrderRepository? orderRepository = null,
        IStockBatchRepository? batchRepository = null,
        ISaleOrderRepository? saleOrderRepository = null)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<StockOutMappingProfile>()).CreateMapper();
        return new StockOutService(
            orderRepository ?? new StockOutOrderRepository(context),
            batchRepository ?? new StockBatchRepository(context),
            new StockLedgerRepository(context),
            new WareRepository(context),
            new CustomerRepository(context),
            new SupplierRepository(context),
            new DepartmentRepository(context),
            saleOrderRepository ?? new SaleOrderRepository(context),
            new DeliveryTaskRepository(context),
            new SupplierBillService(
                new SupplierBillRepository(context),
                new SupplierSettlementRepository(context),
                new FakeCurrentUserService()),
            new GoodsUnitRepository(context),
            unitOfWork,
            mapper,
            new FakeCurrentUserService(),
            new CreateSaleStockOutValidator(),
            new UpdateSaleStockOutValidator(),
            new CreatePurchaseReturnStockOutValidator(),
            new UpdatePurchaseReturnStockOutValidator(),
            new CreateOtherStockOutValidator(),
            new UpdateOtherStockOutValidator(),
            NullLogger<StockOutService>.Instance);
    }

    private static async Task<CatalogSeed> SeedCatalogAsync(
        ApplicationDbContext context,
        decimal batchQuantity = 30m)
    {
        var goodsType = new GoodsType { Id = Guid.NewGuid(), Name = "蔬菜", Code = "VEGETABLE" };
        var goods = new GoodsEntity
        {
            Id = Guid.NewGuid(),
            Name = "番茄",
            Code = "TOMATO",
            GoodsTypeId = goodsType.Id,
            Spec = "大红"
        };
        var baseUnit = new GoodsUnit
        {
            Id = Guid.NewGuid(),
            GoodsId = goods.Id,
            Name = "千克",
            Code = "KG",
            ConversionRate = 1m,
            IsBaseUnit = true
        };
        var caseUnit = new GoodsUnit
        {
            Id = Guid.NewGuid(),
            GoodsId = goods.Id,
            Name = "箱",
            Code = "CASE",
            ConversionRate = 12m,
            IsBaseUnit = false
        };
        goods.BaseUnitId = baseUnit.Id;
        var ware = new Ware { Id = Guid.NewGuid(), Name = "中心仓", Code = "WARE_001" };
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "蔬菜直供商", Code = "SUP_001" };
        var customer = new Domain.Entities.Customers.Customer
        {
            Id = Guid.NewGuid(),
            Name = "零售客户",
            Code = "CUS_001"
        };
        var batch = new StockBatch
        {
            Id = Guid.NewGuid(),
            WareId = ware.Id,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            BatchNo = "BATCH-001",
            BaseUnitId = baseUnit.Id,
            BaseUnitNameSnapshot = baseUnit.Name,
            CurrentQuantity = batchQuantity,
            AvailableQuantity = batchQuantity,
            UnitCost = 5m
        };

        await context.GoodsTypes.AddAsync(goodsType);
        await context.Goods.AddAsync(goods);
        await context.GoodsUnits.AddRangeAsync(baseUnit, caseUnit);
        await context.Wares.AddAsync(ware);
        await context.Suppliers.AddAsync(supplier);
        await context.Customers.AddAsync(customer);
        await context.StockBatches.AddAsync(batch);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new CatalogSeed(
            goods.Id,
            baseUnit.Id,
            caseUnit.Id,
            ware.Id,
            supplier.Id,
            customer.Id,
            batch.Id);
    }

    private static async Task<SaleSourceSeed> SeedSaleOrderAsync(
        ApplicationDbContext context,
        CatalogSeed seed,
        SaleOrderStatus status,
        decimal baseQuantity)
    {
        var order = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = $"SO{Guid.NewGuid():N}",
            CustomerId = seed.CustomerId,
            CustomerNameSnapshot = "零售客户",
            CustomerCodeSnapshot = "CUS_001",
            WareId = seed.WareId,
            OrderDate = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            OrderStatus = status
        };
        var detail = new SaleOrderDetail
        {
            Id = Guid.NewGuid(),
            SaleOrderId = order.Id,
            GoodsId = seed.GoodsId,
            GoodsNameSnapshot = "番茄",
            GoodsCodeSnapshot = "TOMATO",
            GoodsUnitId = seed.BaseUnitId,
            GoodsUnitNameSnapshot = "千克",
            Quantity = baseQuantity,
            BaseQuantity = baseQuantity,
            BaseUnitId = seed.BaseUnitId,
            BaseUnitNameSnapshot = "千克",
            UnitConversion = 1m,
            FixedPrice = 24m,
            TotalPrice = baseQuantity * 24m
        };
        order.Details.Add(detail);
        await context.SaleOrders.AddAsync(order);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new SaleSourceSeed(order.Id, detail.Id);
    }

    private sealed record CatalogSeed(
        Guid GoodsId,
        Guid BaseUnitId,
        Guid CaseUnitId,
        Guid WareId,
        Guid SupplierId,
        Guid CustomerId,
        Guid BatchId);

    private sealed record SaleSourceSeed(Guid OrderId, Guid DetailId);

    private sealed class TrackingStockOutOrderRepository(
        ApplicationDbContext context,
        Func<bool> isTransactionActive) : StockOutOrderRepository(context)
    {
        public bool TransactionWasActiveWhenLocked { get; private set; }

        public int LockCount { get; private set; }

        public override async Task<StockOutOrder?> GetByIdForUpdateAsync(Guid id)
        {
            TransactionWasActiveWhenLocked = isTransactionActive();
            LockCount++;
            return await base.GetByIdForUpdateAsync(id);
        }
    }

    private sealed class TrackingStockBatchRepository(
        ApplicationDbContext context,
        Func<bool> isTransactionActive) : StockBatchRepository(context)
    {
        public bool TransactionWasActiveWhenLocked { get; private set; }

        public override async Task<StockBatch?> GetByIdentityForUpdateAsync(
            Guid wareId,
            Guid goodsId,
            string batchNo)
        {
            TransactionWasActiveWhenLocked = isTransactionActive();
            return await base.GetByIdentityForUpdateAsync(wareId, goodsId, batchNo);
        }
    }

    private sealed class TrackingSaleOrderRepository(
        ApplicationDbContext context,
        Func<bool> isTransactionActive) : SaleOrderRepository(context)
    {
        public bool TransactionWasActiveWhenLocked { get; private set; }

        public override async Task<SaleOrder?> GetByIdForUpdateAsync(Guid id)
        {
            TransactionWasActiveWhenLocked = isTransactionActive();
            return await base.GetByIdForUpdateAsync(id);
        }
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => CurrentUserId;

        public string? GetUserName() => "stock-out-user";

        public string? GetEmail() => "stock-out-user@example.com";

        public string? GetRole() => "operator";

        public IReadOnlyList<string> GetRoles() => ["operator"];

        public bool HasClaim(string claimType, string claimValue) => false;
    }

    private sealed class RecordingUnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        public bool HasActiveTransaction { get; private set; }

        public int RollbackCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return context.SaveChangesAsync(cancellationToken);
        }

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
            HasActiveTransaction = false;
            RollbackCount++;
            context.ChangeTracker.Clear();
            return Task.CompletedTask;
        }

        public Task<int> ExecuteSqlAsync(string sql, params object[] parameters)
        {
            throw new NotSupportedException();
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                if (HasActiveTransaction)
                    await RollbackTransactionAsync(cancellationToken);

                throw;
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await CommitTransactionAsync(cancellationToken);
                return result;
            }
            catch
            {
                if (HasActiveTransaction)
                    await RollbackTransactionAsync(cancellationToken);

                throw;
            }
        }

        public void ClearChangeTracking()
        {
            context.ChangeTracker.Clear();
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
