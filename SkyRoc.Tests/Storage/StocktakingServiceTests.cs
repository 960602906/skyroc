using Application.DTOs.Storage;
using Application.Exceptions;
using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Goods;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkyRoc.Tests.Testing;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Storage;

public class StocktakingServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("10000000-0000-0000-0000-000000000023");

    [Fact]
    public async Task CreateAsync_captures_batch_snapshots_and_calculates_differences()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));

        var result = await service.CreateAsync(Request(seed, 13m, 5m));

        Assert.StartsWith("STK", result.StocktakingNo);
        Assert.Equal(StockDocumentStatus.Draft, result.BusinessStatus);
        Assert.Equal("中心仓", result.WareName);
        Assert.Equal(18m, result.TotalActualQuantity);
        Assert.Equal(20m, result.TotalBookQuantity);
        Assert.Equal(-2m, result.TotalDifferenceQuantity);
        Assert.Equal(CurrentUserId, result.CreateBy);
        var gain = result.Details.Single(x => x.StockBatchId == seed.FirstBatchId);
        Assert.Equal("番茄", gain.GoodsName);
        Assert.Equal("BATCH-001", gain.BatchNo);
        Assert.Equal(10m, gain.BookQuantity);
        Assert.Equal(3m, gain.DifferenceQuantity);
        Assert.Equal(15m, gain.DifferenceAmount);
        var loss = result.Details.Single(x => x.StockBatchId == seed.SecondBatchId);
        Assert.Equal(-5m, loss.DifferenceQuantity);
        Assert.Equal(-25m, loss.DifferenceAmount);
        Assert.Empty(context.StockLedgers);
    }

    [Fact]
    public async Task CreateAsync_rejects_batch_from_another_warehouse()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var otherWare = new Ware { Id = Guid.NewGuid(), Name = "分仓", Code = "WARE_002" };
        var foreignBatch = new StockBatch
        {
            Id = Guid.NewGuid(),
            WareId = otherWare.Id,
            GoodsId = seed.GoodsId,
            GoodsNameSnapshot = "番茄",
            GoodsCodeSnapshot = "TOMATO",
            BatchNo = "FOREIGN-001",
            BaseUnitId = seed.BaseUnitId,
            BaseUnitNameSnapshot = "千克",
            CurrentQuantity = 3m,
            AvailableQuantity = 3m,
            UnitCost = 5m
        };
        await context.Wares.AddAsync(otherWare);
        await context.StockBatches.AddAsync(foreignBatch);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = CreateService(context, new RecordingUnitOfWork(context));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(new CreateStocktakingDto
        {
            WareId = seed.WareId,
            Details = [new CreateStocktakingDetailDto { StockBatchId = foreignBatch.Id, ActualQuantity = 3m }]
        }));

        Assert.Contains("不属于盘点仓库", exception.Message);
        Assert.Empty(context.StocktakingOrders);
    }

    [Fact]
    public async Task CreateAsync_loads_all_requested_batches_in_one_repository_call()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var batchRepository = new TrackingStockBatchRepository(context, () => unitOfWork.HasActiveTransaction);
        var service = CreateService(context, unitOfWork, batchRepository: batchRepository);

        await service.CreateAsync(Request(seed, 10m, 10m));

        Assert.Equal(1, batchRepository.BulkReadCount);
    }

    [Fact]
    public async Task CreateValidator_reports_null_details_without_throwing()
    {
        var validator = new CreateStocktakingValidator();
        var request = new CreateStocktakingDto
        {
            WareId = Guid.NewGuid(),
            Details = null!
        };

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateStocktakingDto.Details));
    }

    [Fact]
    public async Task AuditAsync_applies_gain_and_loss_with_append_only_ledgers()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreateAsync(Request(seed, 13m, 6m));

        var audited = await service.AuditAsync(created.Id, "月末盘点调整");

        Assert.Equal(StockDocumentStatus.Audited, audited.BusinessStatus);
        Assert.True(audited.IsAdjustmentApplied);
        Assert.NotNull(audited.AdjustmentTime);
        Assert.Equal(CurrentUserId, audited.AuditUserId);
        var batches = await context.StockBatches.OrderBy(x => x.BatchNo).ToListAsync();
        Assert.Equal(13m, batches[0].CurrentQuantity);
        Assert.Equal(13m, batches[0].AvailableQuantity);
        Assert.Equal(6m, batches[1].CurrentQuantity);
        Assert.Equal(6m, batches[1].AvailableQuantity);
        var ledgers = await context.StockLedgers.OrderBy(x => x.Direction).ToListAsync();
        Assert.Equal(2, ledgers.Count);
        Assert.All(ledgers, ledger => Assert.Equal(StockLedgerSourceType.Stocktaking, ledger.SourceType));
        var increase = Assert.Single(ledgers, x => x.Direction == StockLedgerDirection.Increase);
        Assert.Equal(3m, increase.ChangeQuantity);
        Assert.Equal(13m, increase.BalanceQuantity);
        Assert.Equal(15m, increase.TotalCost);
        Assert.Equal("月末盘点调整", increase.Remark);
        var decrease = Assert.Single(ledgers, x => x.Direction == StockLedgerDirection.Decrease);
        Assert.Equal(4m, decrease.ChangeQuantity);
        Assert.Equal(6m, decrease.BalanceQuantity);
        Assert.Equal(20m, decrease.TotalCost);
    }

    [Fact]
    public async Task AuditAsync_rejects_duplicate_adjustment()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreateAsync(Request(seed, 12m, 10m));
        await service.AuditAsync(created.Id, null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.AuditAsync(created.Id, null));

        Assert.Contains("不能重复审核", exception.Message);
        Assert.Single(context.StockLedgers);
        Assert.Equal(12m, (await context.StockBatches.SingleAsync(x => x.Id == seed.FirstBatchId)).CurrentQuantity);
    }

    [Fact]
    public async Task AuditAsync_rejects_inventory_changed_after_snapshot_without_partial_adjustment()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreateService(context, unitOfWork);
        var created = await service.CreateAsync(Request(seed, 12m, 8m));
        var changedBatch = await context.StockBatches.SingleAsync(x => x.Id == seed.FirstBatchId);
        changedBatch.CurrentQuantity = 9.999999m;
        changedBatch.AvailableQuantity = 9.999999m;
        changedBatch.LastMovementTime = created.StocktakingTime;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.AuditAsync(created.Id, null));

        Assert.Contains("发生库存变更", exception.Message);
        Assert.Empty(context.StockLedgers);
        Assert.Equal(StockDocumentStatus.Draft,
            (await context.StocktakingOrders.SingleAsync()).BusinessStatus);
        Assert.Equal(1, unitOfWork.RollbackCount);
    }

    [Fact]
    public async Task AuditAsync_rejects_loss_that_would_consume_reserved_stock()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context, firstAvailableQuantity: 2m);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreateService(context, unitOfWork);
        var created = await service.CreateAsync(Request(seed, 5m, 10m));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.AuditAsync(created.Id, null));

        Assert.Contains("可用库存不足", exception.Message);
        Assert.Empty(context.StockLedgers);
        var batch = await context.StockBatches.SingleAsync(x => x.Id == seed.FirstBatchId);
        Assert.Equal(10m, batch.CurrentQuantity);
        Assert.Equal(2m, batch.AvailableQuantity);
        Assert.Equal(1, unitOfWork.RollbackCount);
    }

    [Fact]
    public async Task AuditAsync_rejects_loss_when_rounded_available_quantity_would_be_negative()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context, firstAvailableQuantity: 0m);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreateService(context, unitOfWork);
        var created = await service.CreateAsync(Request(seed, 9.999999m, 10m));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.AuditAsync(created.Id, null));

        Assert.Contains("可用库存不足", exception.Message);
        Assert.Empty(context.StockLedgers);
        var batch = await context.StockBatches.SingleAsync(x => x.Id == seed.FirstBatchId);
        Assert.Equal(10m, batch.CurrentQuantity);
        Assert.Equal(0m, batch.AvailableQuantity);
        Assert.Equal(1, unitOfWork.RollbackCount);
    }

    [Fact]
    public async Task AuditAsync_locks_order_and_batches_inside_transaction()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var orderRepository = new TrackingStocktakingOrderRepository(context, () => unitOfWork.HasActiveTransaction);
        var batchRepository = new TrackingStockBatchRepository(context, () => unitOfWork.HasActiveTransaction);
        var service = CreateService(context, unitOfWork, orderRepository, batchRepository);
        var created = await service.CreateAsync(Request(seed, 11m, 9m));

        await service.AuditAsync(created.Id, null);

        Assert.True(orderRepository.TransactionWasActiveWhenLocked);
        Assert.True(batchRepository.TransactionWasActiveWhenLocked);
        Assert.Equal(2, batchRepository.LockCount);
    }

    private static CreateStocktakingDto Request(CatalogSeed seed, decimal firstActual, decimal secondActual)
    {
        return new CreateStocktakingDto
        {
            WareId = seed.WareId,
            Remark = "月末库存盘点",
            Details =
            [
                new CreateStocktakingDetailDto
                {
                    StockBatchId = seed.FirstBatchId,
                    ActualQuantity = firstActual,
                    Remark = "第一批次复核"
                },
                new CreateStocktakingDetailDto
                {
                    StockBatchId = seed.SecondBatchId,
                    ActualQuantity = secondActual
                }
            ]
        };
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static StocktakingService CreateService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IStocktakingOrderRepository? orderRepository = null,
        IStockBatchRepository? batchRepository = null)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<StocktakingMappingProfile>()).CreateMapper();
        return new StocktakingService(
            orderRepository ?? new StocktakingOrderRepository(context),
            batchRepository ?? new StockBatchRepository(context),
            new StockLedgerRepository(context),
            new WareRepository(context),
            unitOfWork,
            mapper,
            new FakeCurrentUserService(),
            DocumentNoGeneratorTestDouble.Instance,
            new CreateStocktakingValidator(),
            NullLogger<StocktakingService>.Instance);
    }

    private static async Task<CatalogSeed> SeedCatalogAsync(
        ApplicationDbContext context,
        decimal firstAvailableQuantity = 10m)
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
        goods.BaseUnitId = baseUnit.Id;
        var ware = new Ware { Id = Guid.NewGuid(), Name = "中心仓", Code = "WARE_001" };
        var firstBatch = CreateBatch(ware.Id, goods, baseUnit, "BATCH-001", 10m, firstAvailableQuantity);
        var secondBatch = CreateBatch(ware.Id, goods, baseUnit, "BATCH-002", 10m, 10m);

        await context.GoodsTypes.AddAsync(goodsType);
        await context.Goods.AddAsync(goods);
        await context.GoodsUnits.AddAsync(baseUnit);
        await context.Wares.AddAsync(ware);
        await context.StockBatches.AddRangeAsync(firstBatch, secondBatch);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new CatalogSeed(goods.Id, baseUnit.Id, ware.Id, firstBatch.Id, secondBatch.Id);
    }

    private static StockBatch CreateBatch(
        Guid wareId,
        GoodsEntity goods,
        GoodsUnit baseUnit,
        string batchNo,
        decimal currentQuantity,
        decimal availableQuantity)
    {
        return new StockBatch
        {
            Id = Guid.NewGuid(),
            WareId = wareId,
            GoodsId = goods.Id,
            GoodsNameSnapshot = goods.Name,
            GoodsCodeSnapshot = goods.Code,
            BatchNo = batchNo,
            BaseUnitId = baseUnit.Id,
            BaseUnitNameSnapshot = baseUnit.Name,
            CurrentQuantity = currentQuantity,
            AvailableQuantity = availableQuantity,
            UnitCost = 5m
        };
    }

    private sealed record CatalogSeed(
        Guid GoodsId,
        Guid BaseUnitId,
        Guid WareId,
        Guid FirstBatchId,
        Guid SecondBatchId);

    private sealed class TrackingStocktakingOrderRepository(
        ApplicationDbContext context,
        Func<bool> isTransactionActive) : StocktakingOrderRepository(context)
    {
        public bool TransactionWasActiveWhenLocked { get; private set; }

        public override async Task<StocktakingOrder?> GetByIdForUpdateAsync(Guid id)
        {
            TransactionWasActiveWhenLocked = isTransactionActive();
            return await base.GetByIdForUpdateAsync(id);
        }
    }

    private sealed class TrackingStockBatchRepository(
        ApplicationDbContext context,
        Func<bool> isTransactionActive) : StockBatchRepository(context)
    {
        public bool TransactionWasActiveWhenLocked { get; private set; }

        public int BulkReadCount { get; private set; }

        public int LockCount { get; private set; }

        public override async Task<IReadOnlyList<StockBatch>> GetByIdsAsync(IReadOnlyCollection<Guid> ids)
        {
            BulkReadCount++;
            return await base.GetByIdsAsync(ids);
        }

        public override async Task<StockBatch?> GetByIdentityForUpdateAsync(
            Guid wareId,
            Guid goodsId,
            string batchNo)
        {
            TransactionWasActiveWhenLocked = isTransactionActive();
            LockCount++;
            return await base.GetByIdentityForUpdateAsync(wareId, goodsId, batchNo);
        }
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => CurrentUserId;

        public string? GetUserName() => "stocktaking-user";

        public string? GetEmail() => "stocktaking-user@example.com";

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
