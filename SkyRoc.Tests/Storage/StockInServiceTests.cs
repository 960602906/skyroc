using Application.DTOs.Storage;
using Application.Exceptions;
using Application.interfaces;
using Application.Mappers;
using Application.QueryParameters;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Goods;
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

public class StockInServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("10000000-0000-0000-0000-000000000021");

    [Fact]
    public async Task CreatePurchaseAsync_persists_draft_with_ware_supplier_and_snapshots()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreateService(context, unitOfWork);

        var result = await service.CreatePurchaseAsync(PurchaseRequest(seed, 10m, 3m, "B001"));

        Assert.StartsWith("IN", result.InNo);
        Assert.Equal(StockInOrderType.Purchase, result.OrderType);
        Assert.Equal(StockDocumentStatus.Draft, result.BusinessStatus);
        Assert.Equal("中心仓", result.WareName);
        Assert.Equal("蔬菜直供商", result.SupplierName);
        Assert.Equal(CurrentUserId, result.CreateBy);
        var detail = Assert.Single(result.Details);
        Assert.Equal("番茄", detail.GoodsName);
        Assert.Equal(10m, detail.Quantity);
        Assert.Equal(10m, detail.BaseQuantity);
        Assert.Equal(30m, detail.TotalPrice);
        Assert.Equal(30m, result.TotalAmount);
        Assert.Empty(context.StockBatches);
        Assert.Equal(1, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task AuditAsync_creates_batch_and_increases_stock_with_ledger()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreatePurchaseAsync(PurchaseRequest(seed, 10m, 3m, "B001"));

        var audited = await service.AuditAsync(StockInOrderType.Purchase, created.Id, "首次入库");

        Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
        Assert.Equal(CurrentUserId, audited.AuditUserId);
        Assert.NotNull(audited.AuditTime);
        var batch = Assert.Single(context.StockBatches);
        Assert.Equal(10m, batch.CurrentQuantity);
        Assert.Equal(10m, batch.AvailableQuantity);
        Assert.Equal(3m, batch.UnitCost);
        var ledger = Assert.Single(context.StockLedgers);
        Assert.Equal(StockLedgerDirection.Increase, ledger.Direction);
        Assert.Equal(StockLedgerSourceType.PurchaseInbound, ledger.SourceType);
        Assert.Equal(10m, ledger.ChangeQuantity);
        Assert.Equal(10m, ledger.BalanceQuantity);
        Assert.Equal(created.Id, ledger.SourceOrderId);
        var detail = Assert.Single(audited.Details);
        Assert.Equal(batch.Id, detail.StockBatchId);
    }

    [Fact]
    public async Task AuditAsync_merges_into_existing_batch_with_weighted_average_cost()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var first = await service.CreatePurchaseAsync(PurchaseRequest(seed, 10m, 3m, "B001"));
        await service.AuditAsync(StockInOrderType.Purchase, first.Id, null);
        var second = await service.CreatePurchaseAsync(PurchaseRequest(seed, 30m, 5m, "B001"));

        await service.AuditAsync(StockInOrderType.Purchase, second.Id, null);

        var batch = Assert.Single(context.StockBatches);
        Assert.Equal(40m, batch.CurrentQuantity);
        Assert.Equal(40m, batch.AvailableQuantity);
        // (10*3 + 30*5) / 40 = 4.5
        Assert.Equal(4.5m, batch.UnitCost);
        Assert.Equal(2, await context.StockLedgers.CountAsync());
    }

    [Fact]
    public async Task ReverseAuditAsync_restores_weighted_cost_after_multiple_inbounds()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var first = await service.CreatePurchaseAsync(PurchaseRequest(seed, 10m, 3m, "B001"));
        await service.AuditAsync(StockInOrderType.Purchase, first.Id, null);
        var second = await service.CreatePurchaseAsync(PurchaseRequest(seed, 30m, 5m, "B001"));
        await service.AuditAsync(StockInOrderType.Purchase, second.Id, null);

        await service.ReverseAuditAsync(StockInOrderType.Purchase, second.Id, "撤销第二次入库");

        var batch = await context.StockBatches.SingleAsync();
        Assert.Equal(10m, batch.CurrentQuantity);
        Assert.Equal(10m, batch.AvailableQuantity);
        Assert.Equal(3m, batch.UnitCost);
    }

    [Fact]
    public async Task AuditAsync_converts_units_to_base_quantity_and_unit_cost()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreatePurchaseAsync(PurchaseRequest(seed, 2m, 60m, "CASE01", seed.CaseUnitId));

        var audited = await service.AuditAsync(StockInOrderType.Purchase, created.Id, null);

        var detail = Assert.Single(audited.Details);
        Assert.Equal(2m, detail.Quantity);
        Assert.Equal(24m, detail.BaseQuantity);
        var batch = Assert.Single(context.StockBatches);
        Assert.Equal(24m, batch.CurrentQuantity);
        // 60 per case / 12 base per case = 5 per base
        Assert.Equal(5m, batch.UnitCost);
    }

    [Fact]
    public async Task ReverseAuditAsync_rolls_back_stock_with_reversal_ledger()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreatePurchaseAsync(PurchaseRequest(seed, 10m, 3m, "B001"));
        await service.AuditAsync(StockInOrderType.Purchase, created.Id, null);

        var reversed = await service.ReverseAuditAsync(StockInOrderType.Purchase, created.Id, "退回错误入库");

        Assert.Equal(StockDocumentStatus.Reversed, reversed.BusinessStatus);
        Assert.Equal(CurrentUserId, reversed.ReverseUserId);
        var batch = Assert.Single(context.StockBatches);
        Assert.Equal(0m, batch.CurrentQuantity);
        Assert.Equal(0m, batch.AvailableQuantity);
        Assert.Equal(2, await context.StockLedgers.CountAsync());
        var reversalLedger = await context.StockLedgers.SingleAsync(x => x.ReversedFromLedgerId != null);
        Assert.Equal(StockLedgerDirection.Decrease, reversalLedger.Direction);
        Assert.Equal(10m, reversalLedger.ChangeQuantity);
        Assert.Equal(0m, reversalLedger.BalanceQuantity);
    }

    [Fact]
    public async Task ReverseAuditAsync_rejects_when_available_stock_already_consumed()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreatePurchaseAsync(PurchaseRequest(seed, 10m, 3m, "B001"));
        await service.AuditAsync(StockInOrderType.Purchase, created.Id, null);
        var batch = await context.StockBatches.SingleAsync();
        batch.AvailableQuantity = 4m;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ReverseAuditAsync(StockInOrderType.Purchase, created.Id, null));

        Assert.Contains("可用库存不足", exception.Message);
        var reloaded = await context.StockBatches.SingleAsync();
        Assert.Equal(10m, reloaded.CurrentQuantity);
        Assert.Equal(1, await context.StockLedgers.CountAsync());
    }

    [Fact]
    public async Task AuditAsync_rejects_reaudit_of_audited_order()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreatePurchaseAsync(PurchaseRequest(seed, 10m, 3m, "B001"));
        await service.AuditAsync(StockInOrderType.Purchase, created.Id, null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.AuditAsync(StockInOrderType.Purchase, created.Id, null));

        Assert.Contains("不允许审核", exception.Message);
    }

    [Fact]
    public async Task CreatePurchaseAsync_rejects_source_purchase_order_that_is_not_completed()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var source = await SeedPurchaseOrderAsync(context, seed, PurchaseOrderStatus.Draft, 10m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var request = PurchaseRequest(seed, 5m, 3m, "B001");
        request.PurchaseOrderId = source.OrderId;
        request.Details[0].PurchaseOrderDetailId = source.DetailId;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreatePurchaseAsync(request));

        Assert.Contains("未完成或已取消", exception.Message);
        Assert.Empty(context.StockInOrders);
    }

    [Fact]
    public async Task CreatePurchaseAsync_rejects_detail_from_another_purchase_order()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var source = await SeedPurchaseOrderAsync(context, seed, PurchaseOrderStatus.Completed, 10m);
        var unrelated = await SeedPurchaseOrderAsync(context, seed, PurchaseOrderStatus.Completed, 10m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var request = PurchaseRequest(seed, 5m, 3m, "B001");
        request.PurchaseOrderId = source.OrderId;
        request.Details[0].PurchaseOrderDetailId = unrelated.DetailId;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreatePurchaseAsync(request));

        Assert.Contains("不属于当前来源采购单", exception.Message);
        Assert.Empty(context.StockInOrders);
    }

    [Fact]
    public async Task AuditAsync_revalidates_remaining_purchase_quantity()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var source = await SeedPurchaseOrderAsync(context, seed, PurchaseOrderStatus.Completed, 10m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var firstRequest = PurchaseRequest(seed, 6m, 3m, "B001");
        firstRequest.PurchaseOrderId = source.OrderId;
        firstRequest.Details[0].PurchaseOrderDetailId = source.DetailId;
        var secondRequest = PurchaseRequest(seed, 5m, 3m, "B002");
        secondRequest.PurchaseOrderId = source.OrderId;
        secondRequest.Details[0].PurchaseOrderDetailId = source.DetailId;
        var first = await service.CreatePurchaseAsync(firstRequest);
        var second = await service.CreatePurchaseAsync(secondRequest);
        await service.AuditAsync(StockInOrderType.Purchase, first.Id, null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.AuditAsync(StockInOrderType.Purchase, second.Id, null));

        Assert.Contains("超过剩余可入库数量", exception.Message);
        Assert.Equal(6m, (await context.StockBatches.SingleAsync()).CurrentQuantity);
        Assert.Single(context.StockLedgers);
        Assert.Equal(
            StockDocumentStatus.Draft,
            (await context.StockInOrders.SingleAsync(order => order.Id == second.Id)).BusinessStatus);
    }

    [Fact]
    public async Task AuditAsync_locks_order_after_transaction_begins()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var repository = new TrackingStockInOrderRepository(context, () => unitOfWork.HasActiveTransaction);
        var batchRepository = new TrackingStockBatchRepository(context, () => unitOfWork.HasActiveTransaction);
        var service = CreateService(context, unitOfWork, repository, batchRepository);
        var created = await service.CreatePurchaseAsync(PurchaseRequest(seed, 5m, 3m, "B001"));

        await service.AuditAsync(StockInOrderType.Purchase, created.Id, null);

        Assert.Equal(1, repository.LockCallCount);
        Assert.True(repository.TransactionWasActiveWhenLocked);
        Assert.Equal(1, batchRepository.LockCallCount);
        Assert.True(batchRepository.TransactionWasActiveWhenLocked);
    }

    [Fact]
    public async Task UpdatePurchaseAsync_replaces_details_and_blocks_after_audit()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreatePurchaseAsync(PurchaseRequest(seed, 10m, 3m, "B001"));
        var detail = Assert.Single(created.Details);

        var updated = await service.UpdatePurchaseAsync(new UpdatePurchaseStockInDto
        {
            Id = created.Id,
            WareId = seed.WareId,
            SupplierId = seed.SupplierId,
            PurchasePattern = PurchasePattern.SupplierDirect,
            InTime = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            Remark = "  调整入库  ",
            Details =
            [
                new UpdateStockInDetailDto
                {
                    Id = detail.Id,
                    GoodsId = seed.GoodsId,
                    GoodsUnitId = seed.BaseUnitId,
                    Quantity = 12m,
                    UnitPrice = 3m,
                    BatchNo = "B001"
                }
            ]
        });

        Assert.Equal("调整入库", updated.Remark);
        Assert.Equal(12m, Assert.Single(updated.Details).Quantity);
        await service.AuditAsync(StockInOrderType.Purchase, created.Id, null);
        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.DeleteAsync(StockInOrderType.Purchase, created.Id));
        Assert.Contains("已审核", exception.Message);
    }

    [Fact]
    public async Task CreateOtherAsync_and_SalesReturn_use_expected_source_types()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));

        var other = await service.CreateOtherAsync(new CreateOtherStockInDto
        {
            WareId = seed.WareId,
            InTime = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            Details = [DetailRequest(seed, 5m, 2m, "OTHER01")]
        });
        await service.AuditAsync(StockInOrderType.Other, other.Id, null);

        var salesReturn = await service.CreateSalesReturnAsync(new CreateSalesReturnStockInDto
        {
            WareId = seed.WareId,
            CustomerId = seed.CustomerId,
            InTime = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            Details = [DetailRequest(seed, 3m, 2m, "RETURN01")]
        });
        var auditedReturn = await service.AuditAsync(StockInOrderType.SalesReturn, salesReturn.Id, null);

        Assert.Equal("零售客户", auditedReturn.CustomerName);
        Assert.Contains(context.StockLedgers, x => x.SourceType == StockLedgerSourceType.OtherInbound);
        Assert.Contains(context.StockLedgers, x => x.SourceType == StockLedgerSourceType.SalesReturnInbound);
    }

    [Fact]
    public async Task GetByIdAsync_rejects_cross_type_access()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreatePurchaseAsync(PurchaseRequest(seed, 10m, 3m, "B001"));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(StockInOrderType.Other, created.Id));
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_order_type()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        await service.CreatePurchaseAsync(PurchaseRequest(seed, 10m, 3m, "B001"));
        await service.CreateOtherAsync(new CreateOtherStockInDto
        {
            WareId = seed.WareId,
            InTime = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            Details = [DetailRequest(seed, 5m, 2m, "OTHER01")]
        });

        var purchasePage = await service.GetPagedAsync(
            StockInOrderType.Purchase,
            new StockInOrderQueryParameters { Current = 1, Size = 10 });

        Assert.Equal(1, purchasePage.Total);
        Assert.Equal(StockInOrderType.Purchase, Assert.Single(purchasePage.Records!).OrderType);
    }

    private static CreatePurchaseStockInDto PurchaseRequest(
        CatalogSeed seed,
        decimal quantity,
        decimal unitPrice,
        string batchNo,
        Guid? unitId = null)
    {
        return new CreatePurchaseStockInDto
        {
            WareId = seed.WareId,
            SupplierId = seed.SupplierId,
            PurchaserId = seed.PurchaserId,
            PurchasePattern = PurchasePattern.SupplierDirect,
            InTime = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            Remark = "采购入库",
            Details = [DetailRequest(seed, quantity, unitPrice, batchNo, unitId)]
        };
    }

    private static CreateStockInDetailDto DetailRequest(
        CatalogSeed seed,
        decimal quantity,
        decimal unitPrice,
        string batchNo,
        Guid? unitId = null)
    {
        return new CreateStockInDetailDto
        {
            GoodsId = seed.GoodsId,
            GoodsUnitId = unitId ?? seed.BaseUnitId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            BatchNo = batchNo,
            ProductDate = new DateOnly(2026, 7, 1)
        };
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static StockInService CreateService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IStockInOrderRepository? stockInOrderRepository = null,
        IStockBatchRepository? stockBatchRepository = null)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<StockInMappingProfile>()).CreateMapper();
        return new StockInService(
            stockInOrderRepository ?? new StockInOrderRepository(context),
            stockBatchRepository ?? new StockBatchRepository(context),
            new StockLedgerRepository(context),
            new WareRepository(context),
            new SupplierRepository(context),
            new PurchaserRepository(context),
            new CustomerRepository(context),
            new DepartmentRepository(context),
            new PurchaseOrderRepository(context),
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

    private static async Task<CatalogSeed> SeedCatalogAsync(ApplicationDbContext context)
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
        var purchaser = new Purchaser { Id = Guid.NewGuid(), Name = "采购员甲", Code = "PUR_001" };
        var customer = new Domain.Entities.Customers.Customer
        {
            Id = Guid.NewGuid(),
            Name = "零售客户",
            Code = "CUS_001"
        };

        await context.GoodsTypes.AddAsync(goodsType);
        await context.Goods.AddAsync(goods);
        await context.GoodsUnits.AddRangeAsync(baseUnit, caseUnit);
        await context.Wares.AddAsync(ware);
        await context.Suppliers.AddAsync(supplier);
        await context.Purchasers.AddAsync(purchaser);
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new CatalogSeed(
            goods.Id,
            baseUnit.Id,
            caseUnit.Id,
            ware.Id,
            supplier.Id,
            purchaser.Id,
            customer.Id);
    }

    private static async Task<PurchaseSourceSeed> SeedPurchaseOrderAsync(
        ApplicationDbContext context,
        CatalogSeed seed,
        PurchaseOrderStatus status,
        decimal purchaseQuantity)
    {
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PurchaseNo = $"PO{Guid.NewGuid():N}",
            SupplierId = seed.SupplierId,
            SupplierNameSnapshot = "蔬菜直供商",
            PurchaserId = seed.PurchaserId,
            PurchaserNameSnapshot = "采购员甲",
            PurchasePattern = PurchasePattern.SupplierDirect,
            BusinessStatus = status
        };
        var detail = new PurchaseOrderDetail
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = order.Id,
            GoodsId = seed.GoodsId,
            GoodsNameSnapshot = "番茄",
            GoodsCodeSnapshot = "TOMATO",
            PurchaseUnitId = seed.BaseUnitId,
            PurchaseUnitNameSnapshot = "千克",
            RequiredQuantity = purchaseQuantity,
            PurchaseQuantity = purchaseQuantity,
            PurchasePrice = 3m,
            PurchaseTotalPrice = purchaseQuantity * 3m
        };
        order.Details.Add(detail);
        await context.PurchaseOrders.AddAsync(order);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new PurchaseSourceSeed(order.Id, detail.Id);
    }

    private sealed record CatalogSeed(
        Guid GoodsId,
        Guid BaseUnitId,
        Guid CaseUnitId,
        Guid WareId,
        Guid SupplierId,
        Guid PurchaserId,
        Guid CustomerId);

    private sealed record PurchaseSourceSeed(Guid OrderId, Guid DetailId);

    private sealed class TrackingStockInOrderRepository(
        ApplicationDbContext context,
        Func<bool> isTransactionActive) : StockInOrderRepository(context)
    {
        public int LockCallCount { get; private set; }

        public bool TransactionWasActiveWhenLocked { get; private set; }

        public override async Task<StockInOrder?> GetByIdForUpdateAsync(Guid id)
        {
            LockCallCount++;
            TransactionWasActiveWhenLocked = isTransactionActive();
            return await base.GetByIdForUpdateAsync(id);
        }
    }

    private sealed class TrackingStockBatchRepository(
        ApplicationDbContext context,
        Func<bool> isTransactionActive) : StockBatchRepository(context)
    {
        public int LockCallCount { get; private set; }

        public bool TransactionWasActiveWhenLocked { get; private set; }

        public override async Task<StockBatch?> GetByIdentityForUpdateAsync(
            Guid wareId,
            Guid goodsId,
            string batchNo)
        {
            LockCallCount++;
            TransactionWasActiveWhenLocked = isTransactionActive();
            return await base.GetByIdentityForUpdateAsync(wareId, goodsId, batchNo);
        }
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => CurrentUserId;

        public string? GetUserName() => "stock-in-user";

        public string? GetEmail() => "stock-in-user@example.com";

        public string? GetRole() => "operator";

        public IReadOnlyList<string> GetRoles() => ["operator"];

        public bool HasClaim(string claimType, string claimValue) => false;
    }

    private sealed class RecordingUnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        public bool HasActiveTransaction { get; private set; }

        public int CommitCount { get; private set; }

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
            CommitCount++;
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = false;
            context.ChangeTracker.Clear();
            return Task.CompletedTask;
        }

        public Task<int> ExecuteSqlAsync(string sql, params object[] parameters)
        {
            throw new NotSupportedException();
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
