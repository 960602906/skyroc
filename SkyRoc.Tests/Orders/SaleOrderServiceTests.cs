using Application.DTOs.Orders;
using Application.interfaces;
using Application.Mappers;
using Application.QueryParameters;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Constants;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Orders;

public class SaleOrderServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task CreateAsync_saves_order_and_details_in_transaction_with_snapshots_and_totals()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreateService(context, unitOfWork);

        var result = await service.CreateAsync(new CreateSaleOrderDto
        {
            CustomerId = seed.CustomerId,
            OrderDate = new DateTime(2026, 7, 3, 8, 0, 0, DateTimeKind.Utc),
            ContactName = "张老师",
            Details =
            [
                new CreateSaleOrderDetailDto
                {
                    GoodsId = seed.GoodsId,
                    GoodsUnitId = seed.BoxUnitId,
                    Quantity = 2m,
                    FixedGoodsUnitId = seed.BaseUnitId,
                    FixedPrice = 3m
                }
            ]
        });

        Assert.StartsWith("SO", result.OrderNo);
        Assert.Equal("学校客户", result.CustomerName);
        Assert.Equal(30m, result.OrderPrice);
        Assert.Equal(30m, result.SettlementPrice);
        var detail = Assert.Single(result.Details);
        Assert.Equal("番茄", detail.GoodsName);
        Assert.Equal("箱", detail.GoodsUnitName);
        Assert.Equal(10m, detail.BaseQuantity);
        Assert.Equal(30m, detail.TotalPrice);
        Assert.Equal(CurrentUserId, result.CreateBy);
        Assert.Equal(CurrentUserId, detail.CreateBy);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);
        Assert.Equal(1, await context.SaleOrders.CountAsync());
        Assert.Equal(1, await context.SaleOrderDetails.CountAsync());
    }

    [Fact]
    public async Task GetPagedAsync_and_GetByIdAsync_return_order_with_details()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var service = CreateService(context, new RecordingUnitOfWork(context));
        var created = await CreateOrderAsync(service, seed, 1m, seed.BaseUnitId);

        var page = await service.GetPagedAsync(new SaleOrderQueryParameters
        {
            Current = 1,
            Size = 10,
            Keyword = created.OrderNo
        });
        var detail = await service.GetByIdAsync(created.Id);

        Assert.Equal(1, page.Total);
        Assert.Equal(created.Id, Assert.Single(page.Records!).Id);
        Assert.Single(detail.Details);
    }

    [Fact]
    public async Task UpdateAsync_replaces_details_and_recalculates_totals_atomically()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreateService(context, unitOfWork);
        var created = await service.CreateAsync(new CreateSaleOrderDto
        {
            CustomerId = seed.CustomerId,
            OrderDate = new DateTime(2026, 7, 3, 8, 0, 0, DateTimeKind.Utc),
            Details =
            [
                CreateDetail(seed, 1m, seed.BaseUnitId),
                CreateDetail(seed, 2m, seed.BaseUnitId)
            ]
        });
        var retainedDetailId = created.Details[0].Id;
        var removedDetailId = created.Details[1].Id;

        var result = await service.UpdateAsync(new UpdateSaleOrderDto
        {
            Id = created.Id,
            CustomerId = seed.CustomerId,
            OrderDate = created.OrderDate,
            Remark = "已调整",
            Details =
            [
                new UpdateSaleOrderDetailDto
                {
                    Id = retainedDetailId,
                    GoodsId = seed.GoodsId,
                    GoodsUnitId = seed.BoxUnitId,
                    Quantity = 1m,
                    FixedGoodsUnitId = seed.BaseUnitId,
                    FixedPrice = 4m
                },
                new UpdateSaleOrderDetailDto
                {
                    GoodsId = seed.GoodsId,
                    GoodsUnitId = seed.BaseUnitId,
                    Quantity = 2m,
                    FixedGoodsUnitId = seed.BaseUnitId,
                    FixedPrice = 4m
                }
            ]
        });

        Assert.True(result.UpdateStatus);
        Assert.Equal("已调整", result.Remark);
        Assert.Equal(28m, result.OrderPrice);
        Assert.Equal(2, result.Details.Count);
        Assert.Contains(result.Details, x => x.Id == retainedDetailId && x.TotalPrice == 20m);
        Assert.DoesNotContain(result.Details, x => x.Id == removedDetailId);
        Assert.False(await context.SaleOrderDetails.AnyAsync(x => x.Id == removedDetailId));
        Assert.Equal(2, unitOfWork.BeginCount);
        Assert.Equal(2, unitOfWork.CommitCount);
        Assert.Equal(CurrentUserId, result.UpdateBy);
    }

    [Fact]
    public async Task UpdateAsync_rejects_detail_from_another_order_without_starting_transaction()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreateService(context, unitOfWork);
        var first = await CreateOrderAsync(service, seed, 1m, seed.BaseUnitId);
        var second = await CreateOrderAsync(service, seed, 1m, seed.BaseUnitId);
        var beginCount = unitOfWork.BeginCount;

        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.UpdateAsync(new UpdateSaleOrderDto
            {
                Id = first.Id,
                CustomerId = seed.CustomerId,
                OrderDate = first.OrderDate,
                Details =
                [
                    new UpdateSaleOrderDetailDto
                    {
                        Id = second.Details[0].Id,
                        GoodsId = seed.GoodsId,
                        GoodsUnitId = seed.BaseUnitId,
                        Quantity = 1m,
                        FixedGoodsUnitId = seed.BaseUnitId,
                        FixedPrice = 3m
                    }
                ]
            }));

        Assert.Equal("部分订单明细不存在或不属于当前订单", exception.Message);
        Assert.Equal(beginCount, unitOfWork.BeginCount);
    }

    [Fact]
    public async Task DeleteAsync_removes_order_and_cascaded_details_in_transaction()
    {
        await using var context = CreateDbContext();
        var seed = await SeedCatalogAsync(context);
        var unitOfWork = new RecordingUnitOfWork(context);
        var service = CreateService(context, unitOfWork);
        var created = await CreateOrderAsync(service, seed, 1m, seed.BaseUnitId);

        var result = await service.DeleteAsync(created.Id);

        Assert.True(result);
        Assert.False(await context.SaleOrders.AnyAsync(x => x.Id == created.Id));
        Assert.False(await context.SaleOrderDetails.AnyAsync(x => x.SaleOrderId == created.Id));
        Assert.Equal(2, unitOfWork.BeginCount);
        Assert.Equal(2, unitOfWork.CommitCount);
    }

    private static CreateSaleOrderDetailDto CreateDetail(CatalogSeed seed, decimal quantity, Guid fixedUnitId)
    {
        return new CreateSaleOrderDetailDto
        {
            GoodsId = seed.GoodsId,
            GoodsUnitId = seed.BaseUnitId,
            Quantity = quantity,
            FixedGoodsUnitId = fixedUnitId,
            FixedPrice = 3m
        };
    }

    private static Task<SaleOrderDto> CreateOrderAsync(
        ISaleOrderService service,
        CatalogSeed seed,
        decimal quantity,
        Guid fixedUnitId)
    {
        return service.CreateAsync(new CreateSaleOrderDto
        {
            CustomerId = seed.CustomerId,
            OrderDate = new DateTime(2026, 7, 3, 8, 0, 0, DateTimeKind.Utc),
            Details = [CreateDetail(seed, quantity, fixedUnitId)]
        });
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static SaleOrderService CreateService(ApplicationDbContext context, IUnitOfWork unitOfWork)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<OrderMappingProfile>()).CreateMapper();
        return new SaleOrderService(
            new SaleOrderRepository(context),
            new SaleOrderDetailRepository(context),
            new CustomerRepository(context),
            new QuotationRepository(context),
            new WareRepository(context),
            new GoodsRepository(context),
            new GoodsUnitRepository(context),
            unitOfWork,
            mapper,
            new FakeCurrentUserService(),
            new CreateSaleOrderValidator(),
            new UpdateSaleOrderValidator(),
            NullLogger<SaleOrderService>.Instance);
    }

    private static async Task<CatalogSeed> SeedCatalogAsync(ApplicationDbContext context)
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

        await context.Customers.AddAsync(customer);
        await context.GoodsTypes.AddAsync(goodsType);
        await context.Goods.AddAsync(goods);
        await context.GoodsUnits.AddRangeAsync(baseUnit, boxUnit);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new CatalogSeed(customer.Id, goods.Id, baseUnit.Id, boxUnit.Id);
    }

    private sealed record CatalogSeed(Guid CustomerId, Guid GoodsId, Guid BaseUnitId, Guid BoxUnitId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => CurrentUserId;

        public string? GetUserName() => "order-user";

        public string? GetEmail() => "order-user@example.com";

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
