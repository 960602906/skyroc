using Application.DTOs.Orders;
using Application.DTOs.Purchases;
using Application.Exceptions;
using Application.Interfaces;
using Application.Mappers;
using Application.QueryParameters;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Customers;
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

public class PurchasePlanServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("10000000-0000-0000-0000-000000000009");

    [Fact]
    public async Task CreateAsync_persists_plan_with_snapshots_and_defaults()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreatePurchasePlanService(context, unitOfWork);

        var result = await service.CreateAsync(new CreatePurchasePlanDto
        {
            PlanDate = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            PurchasePattern = PurchasePattern.MarketSelfPurchase,
            SupplierId = seed.SupplierId,
            PurchaserId = seed.PurchaserId,
            Remark = "  手工计划  ",
            Details =
            [
                new CreatePurchasePlanDetailDto
                {
                    GoodsId = seed.GoodsId,
                    PurchaseUnitId = seed.BaseUnitId,
                    PlannedQuantity = 12m
                }
            ]
        });

        Assert.StartsWith("PP", result.PlanNo);
        Assert.Equal(PurchasePattern.MarketSelfPurchase, result.PurchasePattern);
        Assert.Equal(PurchasePlanStatus.Unpublished, result.PurchaseStatus);
        Assert.Equal("蔬菜直供商", result.SupplierName);
        Assert.Equal("采购员甲", result.PurchaserName);
        Assert.Equal("手工计划", result.Remark);
        Assert.Equal(CurrentUserId, result.CreateBy);
        var detail = Assert.Single(result.Details);
        Assert.Equal("番茄", detail.GoodsName);
        Assert.Equal("千克", detail.PurchaseUnitName);
        Assert.Equal(12m, detail.PlannedQuantity);
        Assert.Equal(12m, detail.RequiredQuantity);
        Assert.Equal(0m, detail.PurchasedQuantity);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(1, await context.PurchasePlans.CountAsync());
        Assert.Equal(1, await context.PurchasePlanDetails.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_rejects_purchase_unit_from_another_goods()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreatePurchasePlanService(context, unitOfWork);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(new CreatePurchasePlanDto
        {
            PlanDate = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            Details =
            [
                new CreatePurchasePlanDetailDto
                {
                    GoodsId = seed.GoodsId,
                    PurchaseUnitId = seed.OtherGoodsUnitId,
                    PlannedQuantity = 1m
                }
            ]
        }));

        Assert.Contains("采购单位不属于商品", exception.Message);
        Assert.Equal(0, await context.PurchasePlans.CountAsync());
    }

    [Fact]
    public async Task GenerateFromOrdersAsync_creates_plan_and_marks_order_from_approved_order()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var orderService = CreateOrderService(context, unitOfWork);
        var planService = CreatePurchasePlanService(context, unitOfWork);
        var order = await CreateApprovedOrderAsync(orderService, seed);

        var results = await planService.GenerateFromOrdersAsync(new GeneratePurchasePlanFromOrdersDto
        {
            OrderIds = [order.Id],
            Remark = "订单转采购"
        });

        var plan = Assert.Single(results);
        Assert.StartsWith("PP", plan.PlanNo);
        Assert.Equal(PurchasePattern.SupplierDirect, plan.PurchasePattern);
        Assert.Equal("订单转采购", plan.Remark);
        var detail = Assert.Single(plan.Details);
        Assert.Equal("番茄", detail.GoodsName);
        Assert.Equal("千克", detail.PurchaseUnitName);
        // 订单 2 箱 * 5 = 10 千克（基础单位）。
        Assert.Equal(10m, detail.PlannedQuantity);
        Assert.Equal(10m, detail.RequiredQuantity);
        var relation = Assert.Single(detail.OrderRelations);
        Assert.Equal(order.Id, relation.SaleOrderId);
        Assert.Equal(order.OrderNo, relation.SaleOrderNo);
        Assert.Equal(10m, relation.RequiredQuantity);

        var reloadedOrder = await context.SaleOrders
            .Include(x => x.Details)
            .SingleAsync(x => x.Id == order.Id);
        Assert.True(reloadedOrder.HasPurchasePlan);
        Assert.All(reloadedOrder.Details, x => Assert.True(x.HasPurchasePlan));
    }

    [Fact]
    public async Task GenerateFromOrdersAsync_aggregates_multiple_details_of_same_goods()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var orderService = CreateOrderService(context, unitOfWork);
        var planService = CreatePurchasePlanService(context, unitOfWork);
        var order = await CreateApprovedOrderAsync(orderService, seed, includeSecondLine: true);

        var results = await planService.GenerateFromOrdersAsync(new GeneratePurchasePlanFromOrdersDto
        {
            OrderIds = [order.Id]
        });

        var plan = Assert.Single(results);
        var detail = Assert.Single(plan.Details);
        // 2 箱 * 5 + 3 千克 = 13 千克。
        Assert.Equal(13m, detail.PlannedQuantity);
        Assert.Equal(2, detail.OrderRelations.Count);
    }

    [Fact]
    public async Task GenerateFromOrdersAsync_rejects_unapproved_order_without_persisting()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var orderService = CreateOrderService(context, unitOfWork);
        var planService = CreatePurchasePlanService(context, unitOfWork);
        var order = await CreateOrderAsync(orderService, seed);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            planService.GenerateFromOrdersAsync(new GeneratePurchasePlanFromOrdersDto
            {
                OrderIds = [order.Id]
            }));

        Assert.Contains("未审核通过", exception.Message);
        Assert.Equal(0, await context.PurchasePlans.CountAsync());
    }

    [Fact]
    public async Task GenerateFromOrdersAsync_rejects_order_that_already_has_plan()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var orderService = CreateOrderService(context, unitOfWork);
        var planService = CreatePurchasePlanService(context, unitOfWork);
        var order = await CreateApprovedOrderAsync(orderService, seed);
        await planService.GenerateFromOrdersAsync(new GeneratePurchasePlanFromOrdersDto { OrderIds = [order.Id] });

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            planService.GenerateFromOrdersAsync(new GeneratePurchasePlanFromOrdersDto { OrderIds = [order.Id] }));

        Assert.Contains("已生成过采购计划", exception.Message);
        Assert.Equal(1, await context.PurchasePlans.CountAsync());
    }

    [Fact]
    public async Task GetPagedAsync_and_GetByIdAsync_return_plan_with_details()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreatePurchasePlanService(context, unitOfWork);
        var created = await service.CreateAsync(new CreatePurchasePlanDto
        {
            PlanDate = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            Details =
            [
                new CreatePurchasePlanDetailDto
                {
                    GoodsId = seed.GoodsId,
                    PurchaseUnitId = seed.BaseUnitId,
                    PlannedQuantity = 4m
                }
            ]
        });

        var page = await service.GetPagedAsync(new PurchasePlanQueryParameters
        {
            Current = 1,
            Size = 10,
            Keyword = created.PlanNo
        });
        var detail = await service.GetByIdAsync(created.Id);

        Assert.Equal(1, page.Total);
        Assert.Equal(created.Id, Assert.Single(page.Records!).Id);
        Assert.Single(detail.Details);
    }

    [Fact]
    public async Task GetByIdAsync_throws_when_plan_missing()
    {
        await using var context = CreateDbContext();
        var service = CreatePurchasePlanService(context, new RecordingUnitOfWork(context));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AssignSupplierAsync_and_AssignPurchaserAsync_update_snapshots_for_multiple_plans()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreatePurchasePlanService(context, unitOfWork);
        var first = await CreateManualPlanAsync(service, seed, 4m);
        var second = await CreateManualPlanAsync(service, seed, 6m);

        var supplierResults = await service.AssignSupplierAsync(new AssignPurchasePlanSupplierDto
        {
            PlanIds = [first.Id, second.Id],
            SupplierId = seed.SupplierId
        });
        var purchaserResults = await service.AssignPurchaserAsync(new AssignPurchasePlanPurchaserDto
        {
            PlanIds = [first.Id, second.Id],
            PurchaserId = seed.PurchaserId
        });

        Assert.All(supplierResults, plan => Assert.Equal("蔬菜直供商", plan.SupplierName));
        Assert.All(purchaserResults, plan => Assert.Equal("采购员甲", plan.PurchaserName));
        Assert.All(purchaserResults, plan => Assert.Equal(CurrentUserId, plan.UpdateBy));
    }

    [Fact]
    public async Task MergeAsync_combines_compatible_plans_details_and_order_relations()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var orderService = CreateOrderService(context, unitOfWork);
        var planService = CreatePurchasePlanService(context, unitOfWork);
        var firstOrder = await CreateApprovedOrderAsync(orderService, seed);
        var secondOrder = await CreateApprovedOrderAsync(orderService, seed);
        var sourcePlans = await planService.GenerateFromOrdersAsync(new GeneratePurchasePlanFromOrdersDto
        {
            OrderIds = [firstOrder.Id, secondOrder.Id]
        });

        await planService.AssignSupplierAsync(new AssignPurchasePlanSupplierDto
        {
            PlanIds = sourcePlans.Select(plan => plan.Id).ToList(),
            SupplierId = seed.SupplierId
        });
        await planService.AssignPurchaserAsync(new AssignPurchasePlanPurchaserDto
        {
            PlanIds = sourcePlans.Select(plan => plan.Id).ToList(),
            PurchaserId = seed.PurchaserId
        });

        var result = await planService.MergeAsync(new MergePurchasePlansDto
        {
            PlanIds = sourcePlans.Select(plan => plan.Id).ToList(),
            Remark = "合并采购"
        });

        Assert.DoesNotContain(result.Id, sourcePlans.Select(plan => plan.Id));
        Assert.Equal("蔬菜直供商", result.SupplierName);
        Assert.Equal("采购员甲", result.PurchaserName);
        Assert.Equal("合并采购", result.Remark);
        var detail = Assert.Single(result.Details);
        Assert.Equal(20m, detail.RequiredQuantity);
        Assert.Equal(20m, detail.PlannedQuantity);
        Assert.Equal(2, detail.OrderRelations.Count);
        Assert.Equal(1, await context.PurchasePlans.CountAsync());
    }

    [Fact]
    public async Task SplitByOrdersAsync_moves_selected_order_and_preserves_totals()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var orderService = CreateOrderService(context, unitOfWork);
        var planService = CreatePurchasePlanService(context, unitOfWork);
        var firstOrder = await CreateApprovedOrderAsync(orderService, seed);
        var secondOrder = await CreateApprovedOrderAsync(orderService, seed);
        var sourcePlans = await planService.GenerateFromOrdersAsync(new GeneratePurchasePlanFromOrdersDto
        {
            OrderIds = [firstOrder.Id, secondOrder.Id]
        });
        var merged = await planService.MergeAsync(new MergePurchasePlansDto
        {
            PlanIds = sourcePlans.Select(plan => plan.Id).ToList()
        });

        var splittableOrders = await planService.GetSplittableOrdersAsync(merged.Id);
        var split = await planService.SplitByOrdersAsync(new SplitPurchasePlanByOrdersDto
        {
            PlanId = merged.Id,
            SaleOrderIds = [firstOrder.Id],
            Remark = "按订单拆分"
        });
        var source = await planService.GetByIdAsync(merged.Id);

        Assert.Equal(2, splittableOrders.Count);
        Assert.Equal("按订单拆分", split.Remark);
        Assert.Equal(10m, Assert.Single(split.Details).PlannedQuantity);
        Assert.Equal(firstOrder.Id, Assert.Single(Assert.Single(split.Details).OrderRelations).SaleOrderId);
        Assert.Equal(10m, Assert.Single(source.Details).PlannedQuantity);
        Assert.Equal(secondOrder.Id, Assert.Single(Assert.Single(source.Details).OrderRelations).SaleOrderId);
    }

    [Fact]
    public async Task SplitByQuantityAsync_allocates_required_quantity_and_order_relation_proportionally()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var orderService = CreateOrderService(context, unitOfWork);
        var planService = CreatePurchasePlanService(context, unitOfWork);
        var order = await CreateApprovedOrderAsync(orderService, seed);
        var source = Assert.Single(await planService.GenerateFromOrdersAsync(new GeneratePurchasePlanFromOrdersDto
        {
            OrderIds = [order.Id]
        }));
        var sourceDetail = Assert.Single(source.Details);

        var split = await planService.SplitByQuantityAsync(new SplitPurchasePlanByQuantityDto
        {
            PlanId = source.Id,
            Details = [new PurchasePlanQuantitySplitItemDto { DetailId = sourceDetail.Id, Quantity = 4m }]
        });
        var remaining = await planService.GetByIdAsync(source.Id);

        var splitDetail = Assert.Single(split.Details);
        Assert.Equal(4m, splitDetail.PlannedQuantity);
        Assert.Equal(4m, splitDetail.RequiredQuantity);
        Assert.Equal(4m, Assert.Single(splitDetail.OrderRelations).RequiredQuantity);
        var remainingDetail = Assert.Single(remaining.Details);
        Assert.Equal(6m, remainingDetail.PlannedQuantity);
        Assert.Equal(6m, remainingDetail.RequiredQuantity);
        Assert.Equal(6m, Assert.Single(remainingDetail.OrderRelations).RequiredQuantity);
    }

    [Fact]
    public async Task Plan_mutations_reject_plan_that_has_generated_purchase_order()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreatePurchasePlanService(context, unitOfWork);
        var created = await CreateManualPlanAsync(service, seed, 5m);
        var entity = await context.PurchasePlans.SingleAsync(plan => plan.Id == created.Id);
        entity.PurchaseStatus = PurchasePlanStatus.Generated;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.AssignSupplierAsync(
            new AssignPurchasePlanSupplierDto
            {
                PlanIds = [created.Id],
                SupplierId = seed.SupplierId
            }));

        Assert.Contains("已生成采购单", exception.Message);
    }

    private static Task<PurchasePlanDto> CreateManualPlanAsync(
        IPurchasePlanService service,
        CatalogSeed seed,
        decimal quantity)
    {
        return service.CreateAsync(new CreatePurchasePlanDto
        {
            PlanDate = new DateTime(2026, 7, 5, 8, 0, 0, DateTimeKind.Utc),
            Details =
            [
                new CreatePurchasePlanDetailDto
                {
                    GoodsId = seed.GoodsId,
                    PurchaseUnitId = seed.BaseUnitId,
                    PlannedQuantity = quantity
                }
            ]
        });
    }

    private static Task<SaleOrderDto> CreateOrderAsync(ISaleOrderService service, CatalogSeed seed, bool includeSecondLine = false)
    {
        var details = new List<CreateSaleOrderDetailDto>
        {
            new()
            {
                GoodsId = seed.GoodsId,
                GoodsUnitId = seed.BoxUnitId,
                Quantity = 2m,
                FixedGoodsUnitId = seed.BaseUnitId,
                FixedPrice = 3m
            }
        };
        if (includeSecondLine)
        {
            details.Add(new CreateSaleOrderDetailDto
            {
                GoodsId = seed.GoodsId,
                GoodsUnitId = seed.BaseUnitId,
                Quantity = 3m,
                FixedGoodsUnitId = seed.BaseUnitId,
                FixedPrice = 3m
            });
        }

        return service.CreateAsync(new CreateSaleOrderDto
        {
            CustomerId = seed.CustomerId,
            OrderDate = new DateTime(2026, 7, 3, 8, 0, 0, DateTimeKind.Utc),
            ReceiveDate = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            Details = details
        });
    }

    private static async Task<SaleOrderDto> CreateApprovedOrderAsync(
        ISaleOrderService service,
        CatalogSeed seed,
        bool includeSecondLine = false)
    {
        var order = await CreateOrderAsync(service, seed, includeSecondLine);
        await service.ApproveAsync(order.Id, "审核通过");
        return order;
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PurchasePlanService CreatePurchasePlanService(ApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<PurchasePlanMappingProfile>()).CreateMapper();
        return new PurchasePlanService(
            new PurchasePlanRepository(context),
            new SaleOrderRepository(context),
            new SupplierRepository(context),
            new PurchaserRepository(context),
            new GoodsRepository(context),
            new GoodsUnitRepository(context),
            unitOfWork,
            mapper,
            new FakeCurrentUserService(),
            DocumentNoGeneratorTestDouble.Instance,
            new CreatePurchasePlanValidator(),
            new GeneratePurchasePlanFromOrdersValidator(),
            NullLogger<PurchasePlanService>.Instance);
    }

    private static SaleOrderService CreateOrderService(ApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<OrderMappingProfile>()).CreateMapper();
        return new SaleOrderService(
            new SaleOrderRepository(context),
            new SaleOrderDetailRepository(context),
            new OrderAuditLogRepository(context),
            new CustomerRepository(context),
            new QuotationRepository(context),
            new WareRepository(context),
            new GoodsRepository(context),
            new GoodsUnitRepository(context),
            unitOfWork,
            mapper,
            new FakeCurrentUserService(),
            DocumentNoGeneratorTestDouble.Instance,
            new CreateSaleOrderValidator(),
            new UpdateSaleOrderValidator(),
            NullLogger<SaleOrderService>.Instance);
    }

    private static async Task<CatalogSeed> SeedCatalogAsync(ApplicationDbContext context)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "学校客户", Code = "SCHOOL_001" };
        var goodsType = new GoodsType { Id = Guid.NewGuid(), Name = "蔬菜", Code = "VEGETABLE" };
        var goods = new GoodsEntity { Id = Guid.NewGuid(), Name = "番茄", Code = "TOMATO", GoodsTypeId = goodsType.Id };
        var baseUnit = new GoodsUnit
        {
            Id = Guid.NewGuid(),
            GoodsId = goods.Id,
            Name = "千克",
            Code = "KG",
            ConversionRate = 1m,
            IsBaseUnit = true
        };
        var boxUnit = new GoodsUnit
        {
            Id = Guid.NewGuid(),
            GoodsId = goods.Id,
            Name = "箱",
            Code = "BOX",
            ConversionRate = 5m
        };
        goods.BaseUnitId = baseUnit.Id;

        var otherGoods = new GoodsEntity { Id = Guid.NewGuid(), Name = "白菜", Code = "CABBAGE", GoodsTypeId = goodsType.Id };
        var otherGoodsUnit = new GoodsUnit
        {
            Id = Guid.NewGuid(),
            GoodsId = otherGoods.Id,
            Name = "千克",
            Code = "KG",
            ConversionRate = 1m,
            IsBaseUnit = true
        };
        otherGoods.BaseUnitId = otherGoodsUnit.Id;

        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "蔬菜直供商", Code = "SUP_001" };
        var purchaser = new Purchaser { Id = Guid.NewGuid(), Name = "采购员甲", Code = "PUR_001" };

        await context.Customers.AddAsync(customer);
        await context.GoodsTypes.AddAsync(goodsType);
        await context.Goods.AddRangeAsync(goods, otherGoods);
        await context.GoodsUnits.AddRangeAsync(baseUnit, boxUnit, otherGoodsUnit);
        await context.Suppliers.AddAsync(supplier);
        await context.Purchasers.AddAsync(purchaser);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new CatalogSeed(
            customer.Id,
            goods.Id,
            baseUnit.Id,
            boxUnit.Id,
            otherGoodsUnit.Id,
            supplier.Id,
            purchaser.Id);
    }

    private sealed record CatalogSeed(
        Guid CustomerId,
        Guid GoodsId,
        Guid BaseUnitId,
        Guid BoxUnitId,
        Guid OtherGoodsUnitId,
        Guid SupplierId,
        Guid PurchaserId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => CurrentUserId;

        public string? GetUserName() => "purchase-user";

        public string? GetEmail() => "purchase-user@example.com";

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
            Assert.False(HasActiveTransaction);
            HasActiveTransaction = true;
            BeginCount++;
            return Task.CompletedTask;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            Assert.True(HasActiveTransaction);
            await context.SaveChangesAsync(cancellationToken);
            HasActiveTransaction = false;
            CommitCount++;
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            Assert.True(HasActiveTransaction);
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
