using Application.DTOs.Purchases;
using Application.Exceptions;
using Application.interfaces;
using Application.Mappers;
using Application.QueryParameters;
using Application.Services;
using Application.Validator;
using AutoMapper;
using System.Text.Json;
using Domain.Entities.Goods;
using Domain.Entities.Purchases;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkyRoc.Tests.Testing;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Purchases;

public class PurchaseOrderServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("10000000-0000-0000-0000-000000000010");

    [Fact]
    public async Task CreateAsync_persists_draft_with_party_goods_and_money_snapshots()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreateService(context, unitOfWork);

        var result = await service.CreateAsync(CreateOrderRequest(seed, 2.5m, 3.2m));

        Assert.StartsWith("PO", result.PurchaseNo);
        Assert.Equal(PurchaseOrderStatus.Draft, result.BusinessStatus);
        Assert.Equal("蔬菜直供商", result.SupplierName);
        Assert.Equal("供应商联系人", result.SupplierContactName);
        Assert.Equal("采购员甲", result.PurchaserName);
        Assert.Equal(CurrentUserId, result.CreateBy);
        var detail = Assert.Single(result.Details);
        Assert.Equal("番茄", detail.GoodsName);
        Assert.Equal("千克", detail.PurchaseUnitName);
        Assert.Equal(2.5m, detail.RequiredQuantity);
        Assert.Equal(8m, detail.PurchaseTotalPrice);
        using var goodsInfo = JsonDocument.Parse(detail.GoodsInfo!);
        Assert.Equal("大红", goodsInfo.RootElement.GetProperty("Spec").GetString());
        Assert.Empty(detail.PlanRelations);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task UpdateAsync_and_queries_replace_editable_values_for_draft()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await service.CreateAsync(CreateOrderRequest(seed, 2m, 3m));
        var originalDetail = Assert.Single(created.Details);

        var updated = await service.UpdateAsync(new UpdatePurchaseOrderDto
        {
            Id = created.Id,
            SupplierId = seed.SupplierId,
            PurchaserId = seed.PurchaserId,
            PurchasePattern = PurchasePattern.SupplierDirect,
            ReceiveTime = new DateTime(2026, 7, 8, 8, 0, 0, DateTimeKind.Utc),
            Remark = "  调整数量  ",
            Details =
            [
                new UpdatePurchaseOrderDetailDto
                {
                    Id = originalDetail.Id,
                    GoodsId = seed.GoodsId,
                    PurchaseUnitId = seed.BaseUnitId,
                    PurchaseQuantity = 3m,
                    PurchasePrice = 4m
                }
            ]
        });
        var page = await service.GetPagedAsync(new PurchaseOrderQueryParameters
        {
            Current = 1,
            Size = 10,
            Keyword = created.PurchaseNo
        });
        var detail = await service.GetByIdAsync(created.Id);

        Assert.Equal("调整数量", updated.Remark);
        Assert.Equal(12m, Assert.Single(updated.Details).PurchaseTotalPrice);
        Assert.Equal(created.Id, Assert.Single(page.Records!).Id);
        Assert.Equal(3m, Assert.Single(detail.Details).PurchaseQuantity);
    }

    [Fact]
    public async Task CreateAsync_rejects_purchase_unit_from_another_goods()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var request = CreateOrderRequest(seed, 1m, 1m);
        request.Details[0].PurchaseUnitId = seed.OtherGoodsUnitId;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(request));

        Assert.Contains("采购单位不属于商品", exception.Message);
        Assert.Empty(context.PurchaseOrders);
    }

    [Fact]
    public async Task GenerateFromPlansAsync_groups_compatible_plans_and_marks_them_generated()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var firstPlan = await SeedPlanAsync(context, seed, 4m);
        var secondPlan = await SeedPlanAsync(context, seed, 6m);
        var service = CreateService(context, new RecordingUnitOfWork(context));

        var results = await service.GenerateFromPlansAsync(new GeneratePurchaseOrdersFromPlansDto
        {
            PlanIds = [firstPlan.Id, secondPlan.Id],
            Remark = "计划转采购"
        });

        var order = Assert.Single(results);
        var detail = Assert.Single(order.Details);
        Assert.Equal(10m, detail.PurchaseQuantity);
        Assert.Equal(2, detail.PlanRelations.Count);
        Assert.Equal(0m, detail.PurchasePrice);
        var plans = await context.PurchasePlans.Include(x => x.Details).ToListAsync();
        Assert.All(plans, plan => Assert.Equal(PurchasePlanStatus.Generated, plan.PurchaseStatus));
        Assert.All(plans.SelectMany(x => x.Details), planDetail =>
            Assert.Equal(planDetail.PlannedQuantity, planDetail.PurchasedQuantity));
    }

    [Fact]
    public async Task GenerateFromPlansAsync_rejects_plan_without_remaining_quantity()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var plan = await SeedPlanAsync(context, seed, 5m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        await service.GenerateFromPlansAsync(new GeneratePurchaseOrdersFromPlansDto { PlanIds = [plan.Id] });

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GenerateFromPlansAsync(new GeneratePurchaseOrdersFromPlansDto { PlanIds = [plan.Id] }));

        Assert.Contains("没有可生成采购单的剩余数量", exception.Message);
        Assert.Equal(1, await context.PurchaseOrders.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_reduces_plan_allocation_and_sets_partial_status()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var plan = await SeedPlanAsync(context, seed, 5m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var order = Assert.Single(await service.GenerateFromPlansAsync(
            new GeneratePurchaseOrdersFromPlansDto { PlanIds = [plan.Id] }));
        var detail = Assert.Single(order.Details);
        var relation = Assert.Single(detail.PlanRelations);

        var updated = await service.UpdateAsync(new UpdatePurchaseOrderDto
        {
            Id = order.Id,
            SupplierId = seed.SupplierId,
            PurchaserId = seed.PurchaserId,
            PurchasePattern = PurchasePattern.SupplierDirect,
            Details =
            [
                new UpdatePurchaseOrderDetailDto
                {
                    Id = detail.Id,
                    GoodsId = seed.GoodsId,
                    PurchaseUnitId = seed.BaseUnitId,
                    PurchaseQuantity = 2m,
                    PurchasePrice = 2m,
                    PlanAllocations =
                    [
                        new PurchaseOrderPlanAllocationDto
                        {
                            PurchasePlanDetailId = relation.PurchasePlanDetailId,
                            AllocatedQuantity = 2m
                        }
                    ]
                }
            ]
        });

        Assert.Equal(2m, Assert.Single(updated.Details).PurchaseQuantity);
        var reloadedPlan = await context.PurchasePlans.Include(x => x.Details).SingleAsync(x => x.Id == plan.Id);
        Assert.Equal(PurchasePlanStatus.PartiallyGenerated, reloadedPlan.PurchaseStatus);
        Assert.Equal(2m, Assert.Single(reloadedPlan.Details).PurchasedQuantity);
    }

    [Fact]
    public async Task CancelAsync_releases_plan_allocation_and_prevents_further_changes()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var plan = await SeedPlanAsync(context, seed, 5m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var order = Assert.Single(await service.GenerateFromPlansAsync(
            new GeneratePurchaseOrdersFromPlansDto { PlanIds = [plan.Id] }));

        var cancelled = await service.CancelAsync(order.Id);
        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(order.Id));

        Assert.Equal(PurchaseOrderStatus.Cancelled, cancelled.BusinessStatus);
        Assert.Contains("不是草稿", exception.Message);
        var reloadedPlan = await context.PurchasePlans.Include(x => x.Details).SingleAsync(x => x.Id == plan.Id);
        Assert.Equal(PurchasePlanStatus.Unpublished, reloadedPlan.PurchaseStatus);
        Assert.Equal(0m, Assert.Single(reloadedPlan.Details).PurchasedQuantity);
    }

    [Fact]
    public async Task CompleteAsync_requires_draft_and_blocks_delete_after_completion()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var order = await service.CreateAsync(CreateOrderRequest(seed, 2m, 3m));

        var completed = await service.CompleteAsync(order.Id);
        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(order.Id));

        Assert.Equal(PurchaseOrderStatus.Completed, completed.BusinessStatus);
        Assert.Contains("不是草稿", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_releases_plan_allocation_and_removes_draft()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var plan = await SeedPlanAsync(context, seed, 5m);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var order = Assert.Single(await service.GenerateFromPlansAsync(
            new GeneratePurchaseOrdersFromPlansDto { PlanIds = [plan.Id] }));

        Assert.True(await service.DeleteAsync(order.Id));

        Assert.Empty(context.PurchaseOrders);
        var reloadedPlan = await context.PurchasePlans.Include(x => x.Details).SingleAsync(x => x.Id == plan.Id);
        Assert.Equal(PurchasePlanStatus.Unpublished, reloadedPlan.PurchaseStatus);
        Assert.Equal(0m, Assert.Single(reloadedPlan.Details).PurchasedQuantity);
    }

    private static CreatePurchaseOrderDto CreateOrderRequest(CatalogSeed seed, decimal quantity, decimal price)
    {
        return new CreatePurchaseOrderDto
        {
            SupplierId = seed.SupplierId,
            PurchaserId = seed.PurchaserId,
            PurchasePattern = PurchasePattern.SupplierDirect,
            ReceiveTime = new DateTime(2026, 7, 7, 8, 0, 0, DateTimeKind.Utc),
            Remark = "  手工采购  ",
            Details =
            [
                new CreatePurchaseOrderDetailDto
                {
                    GoodsId = seed.GoodsId,
                    PurchaseUnitId = seed.BaseUnitId,
                    PurchaseQuantity = quantity,
                    PurchasePrice = price
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

    private static PurchaseOrderService CreateService(ApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<PurchaseOrderMappingProfile>()).CreateMapper();
        return new PurchaseOrderService(
            new PurchaseOrderRepository(context),
            new PurchasePlanRepository(context),
            new SupplierRepository(context),
            new PurchaserRepository(context),
            new GoodsRepository(context),
            new GoodsUnitRepository(context),
            unitOfWork,
            mapper,
            new FakeCurrentUserService(),
            DocumentNoGeneratorTestDouble.Instance,
            new CreatePurchaseOrderValidator(),
            new UpdatePurchaseOrderValidator(),
            new GeneratePurchaseOrdersFromPlansValidator(),
            NullLogger<PurchaseOrderService>.Instance);
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
        goods.BaseUnitId = baseUnit.Id;
        var otherGoods = new GoodsEntity
        {
            Id = Guid.NewGuid(),
            Name = "白菜",
            Code = "CABBAGE",
            GoodsTypeId = goodsType.Id
        };
        var otherUnit = new GoodsUnit
        {
            Id = Guid.NewGuid(),
            GoodsId = otherGoods.Id,
            Name = "千克",
            ConversionRate = 1m,
            IsBaseUnit = true
        };
        otherGoods.BaseUnitId = otherUnit.Id;
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = "蔬菜直供商",
            Code = "SUP_001",
            ContactName = "供应商联系人",
            ContactPhone = "13800000000"
        };
        var purchaser = new Purchaser { Id = Guid.NewGuid(), Name = "采购员甲", Code = "PUR_001" };

        await context.GoodsTypes.AddAsync(goodsType);
        await context.Goods.AddRangeAsync(goods, otherGoods);
        await context.GoodsUnits.AddRangeAsync(baseUnit, otherUnit);
        await context.Suppliers.AddAsync(supplier);
        await context.Purchasers.AddAsync(purchaser);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new CatalogSeed(goods.Id, baseUnit.Id, otherUnit.Id, supplier.Id, purchaser.Id);
    }

    private static async Task<PurchasePlan> SeedPlanAsync(
        ApplicationDbContext context,
        CatalogSeed seed,
        decimal quantity)
    {
        var plan = new PurchasePlan
        {
            Id = Guid.NewGuid(),
            PlanNo = $"PP{Guid.NewGuid():N}",
            PlanDate = new DateTime(2026, 7, 7, 8, 0, 0, DateTimeKind.Utc),
            PurchasePattern = PurchasePattern.SupplierDirect,
            PurchaseStatus = PurchasePlanStatus.Unpublished,
            SupplierId = seed.SupplierId,
            SupplierNameSnapshot = "蔬菜直供商",
            PurchaserId = seed.PurchaserId,
            PurchaserNameSnapshot = "采购员甲",
            Details =
            [
                new PurchasePlanDetail
                {
                    Id = Guid.NewGuid(),
                    GoodsId = seed.GoodsId,
                    GoodsNameSnapshot = "番茄",
                    GoodsCodeSnapshot = "TOMATO",
                    PurchaseUnitId = seed.BaseUnitId,
                    PurchaseUnitNameSnapshot = "千克",
                    RequiredQuantity = quantity,
                    PlannedQuantity = quantity,
                    PurchasedQuantity = 0m
                }
            ]
        };
        await context.PurchasePlans.AddAsync(plan);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return plan;
    }

    private sealed record CatalogSeed(
        Guid GoodsId,
        Guid BaseUnitId,
        Guid OtherGoodsUnitId,
        Guid SupplierId,
        Guid PurchaserId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => CurrentUserId;

        public string? GetUserName() => "purchase-order-user";

        public string? GetEmail() => "purchase-order-user@example.com";

        public string? GetRole() => "operator";

        public IReadOnlyList<string> GetRoles() => ["operator"];

        public bool HasClaim(string claimType, string claimValue) => false;
    }

    private sealed class RecordingUnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        public bool HasActiveTransaction { get; private set; }

        public int BeginCount { get; private set; }

        public int CommitCount { get; private set; }

        public int RollbackCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return context.SaveChangesAsync(cancellationToken);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = true;
            BeginCount++;
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
