using Application.DTOs.AfterSales;
using Application.Mappers;
using Application.Services;
using Application.Validator.AfterSales;
using Application.interfaces;
using AutoMapper;
using Domain.Entities.AfterSales;
using Domain.Entities.Customers;
using Domain.Entities.Finance;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Constants;
using SkyRoc.Tests.Testing;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.AfterSales;

/// <summary>
/// 验证售后创建快照、来源数量占用和完整审核状态机。
/// </summary>
public class AfterSaleServiceTests
{
    [Fact]
    public async Task CreateAsync_FromSaleOrder_CreatesDraftWithServerCalculatedSnapshots()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, 6m);
        var service = CreateService(context);

        var result = await service.CreateAsync(CreateRequest(seed, 2m));

        Assert.Equal(AfterSaleStatus.Draft, result.AfterStatus);
        Assert.Equal(seed.OrderId, result.SaleOrderId);
        Assert.Equal("SO-AFTER-001", result.SaleOrderNo);
        Assert.Equal("学校客户", result.CustomerName);
        Assert.Equal(25m, result.OrderPrice);
        Assert.Equal(20m, result.SettlementPrice);
        var item = Assert.Single(result.Goods);
        Assert.Equal(2m, item.ActualRefundQuantity);
        Assert.Equal(2m, item.BaseRefundQuantity);
        Assert.Equal(2.5m, item.UnitPrice);
        Assert.Equal(5m, item.RefundAmount);
        Assert.Equal("番茄", item.GoodsName);
        Assert.Empty(result.AuditLogs);
    }

    [Fact]
    public async Task CreateAsync_RejectsQuantityBeyondAcceptedAndExistingAfterSaleReservations()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, 6m);
        var service = CreateService(context);
        await service.CreateAsync(CreateRequest(seed, 4m));

        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CreateAsync(CreateRequest(seed, 3m)));

        Assert.Contains("累计售后数量", exception.Message);
        Assert.Equal(1, await context.AfterSales.CountAsync());
        Assert.Equal(1, await context.AfterSaleGoods.CountAsync());
    }

    [Theory]
    [InlineData(AfterSaleType.RefundOnly, AfterSaleHandleType.GoodsDiscount, AfterSaleStatus.RefundPending, 2250)]
    [InlineData(AfterSaleType.ReturnAndRefund, AfterSaleHandleType.GoodsDiscount, AfterSaleStatus.ReturnPending, 2250)]
    [InlineData(AfterSaleType.RefundOnly, AfterSaleHandleType.Replenishment, AfterSaleStatus.ReturnPending, 2500)]
    [InlineData(AfterSaleType.ReturnAndRefund, AfterSaleHandleType.Exchange, AfterSaleStatus.ReturnPending, 2500)]
    public async Task ApproveAsync_RoutesRefundReturnReplenishmentAndExchange(
        AfterSaleType afterSaleType,
        AfterSaleHandleType handleType,
        AfterSaleStatus expectedStatus,
        int expectedSettlementCents)
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, null);
        var service = CreateService(context);
        var request = CreateRequest(seed, 1m);
        request.Goods[0].AfterSaleType = afterSaleType;
        request.Goods[0].HandleType = handleType;
        var created = await service.CreateAsync(request);
        await service.SubmitAsync(created.Id, "提交审核");

        var approved = await service.ApproveAsync(created.Id, "同意处理");

        Assert.Equal(expectedStatus, approved.AfterStatus);
        Assert.Equal(expectedSettlementCents / 100m, approved.SettlementPrice);
        Assert.Equal(afterSaleType == AfterSaleType.ReturnAndRefund ? 1 : 0, approved.PickupTasks.Count);
        Assert.Collection(
            approved.AuditLogs,
            log => Assert.Equal(AfterSaleAuditAction.Submit, log.Action),
            log => Assert.Equal(AfterSaleAuditAction.Approve, log.Action));
    }

    [Fact]
    public async Task RejectResubmitAndReverseAsync_PreserveOrderedAuditTrail()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, null);
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest(seed, 1m));
        await service.SubmitAsync(created.Id, null);

        var rejected = await service.RejectAsync(created.Id, "数量需核实");
        Assert.Equal(AfterSaleStatus.Draft, rejected.AfterStatus);
        await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.SubmitAsync(created.Id, null));

        var resubmitted = await service.ResubmitAsync(created.Id, "已核实");
        var approved = await service.ApproveAsync(created.Id, null);
        var reversed = await service.ReverseAsync(created.Id, "财务信息有误");

        Assert.Equal(AfterSaleStatus.PendingAudit, reversed.AfterStatus);
        Assert.Equal(
            [
                AfterSaleAuditAction.Submit,
                AfterSaleAuditAction.Reject,
                AfterSaleAuditAction.Resubmit,
                AfterSaleAuditAction.Approve,
                AfterSaleAuditAction.Reverse
            ],
            reversed.AuditLogs.Select(x => x.Action));
        Assert.Equal(AfterSaleStatus.PendingAudit, resubmitted.AfterStatus);
        Assert.Equal(AfterSaleStatus.RefundPending, approved.AfterStatus);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesRejectedDraftGoodsAndRecalculatesSettlement()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, null);
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest(seed, 1m));
        await service.SubmitAsync(created.Id, null);
        await service.RejectAsync(created.Id, "请修正数量");

        var updated = await service.UpdateAsync(new UpdateAfterSaleDto
        {
            Id = created.Id,
            ContactName = "李老师",
            PickupAddress = "学校后门",
            Goods = CreateRequest(seed, 3m).Goods
        });

        Assert.Equal(17.5m, updated.SettlementPrice);
        Assert.Equal("李老师", updated.ContactName);
        Assert.Equal("学校后门", updated.PickupAddress);
        Assert.Equal(3m, Assert.Single(updated.Goods).ActualRefundQuantity);
        Assert.Equal(1, await context.AfterSaleGoods.CountAsync());
    }

    [Fact]
    public async Task CompleteAsync_AllowsApprovedProcessingOnlyAndPreventsReverseAfterCompletion()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, null);
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest(seed, 1m));

        await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CompleteAsync(created.Id));
        await service.SubmitAsync(created.Id, null);
        await service.ApproveAsync(created.Id, null);
        var completed = await service.CompleteAsync(created.Id);

        Assert.Equal(AfterSaleStatus.Completed, completed.AfterStatus);
        await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.ReverseAsync(created.Id, "尝试撤销"));
    }

    [Fact]
    public async Task CompleteAsync_AppendsCustomerBillAdjustment_WhenSignedOrderAlreadyHasBill()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, 10m, SaleOrderStatus.Signed);
        var saleOrder = await new SaleOrderRepository(context).GetByIdAsync(seed.OrderId);
        await new CustomerBillService(
            new CustomerBillRepository(context),
            new AfterSaleRepository(context),
            new FakeCurrentUserService(),
            DocumentNoGeneratorTestDouble.Instance).SyncOrderAcceptanceAsync(saleOrder!);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest(seed, 2m));
        await service.SubmitAsync(created.Id, null);
        await service.ApproveAsync(created.Id, null);

        await service.CompleteAsync(created.Id);

        var updatedBill = await context.CustomerBills.Include(x => x.Details).SingleAsync();
        Assert.Equal(25m, updatedBill.OrderAmount);
        Assert.Equal(-5m, updatedBill.AfterSaleAdjustmentAmount);
        Assert.Equal(20m, updatedBill.ReceivableAmount);
        Assert.Equal(2, updatedBill.Details.Count);
        var adjustment = Assert.Single(
            updatedBill.Details,
            x => x.SourceType == CustomerBillDetailSourceType.AfterSaleAdjustment);
        Assert.Equal(-2m, adjustment.Quantity);
        Assert.Equal(-5m, adjustment.Amount);
    }

    [Fact]
    public async Task ReverseAsync_RejectsAfterSaleWithDownstreamPickupTask()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, null);
        var service = CreateService(context);
        var request = CreateRequest(seed, 1m);
        request.Goods[0].AfterSaleType = AfterSaleType.ReturnAndRefund;
        var created = await service.CreateAsync(request);
        await service.SubmitAsync(created.Id, null);
        var approved = await service.ApproveAsync(created.Id, null);
        Assert.Single(approved.PickupTasks);

        var completeException = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CompleteAsync(created.Id));
        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.ReverseAsync(created.Id, "尝试撤销"));

        Assert.Contains("未完成的取货任务", completeException.Message);
        Assert.Contains("已生成取货任务", exception.Message);
        Assert.Equal(AfterSaleStatus.ReturnPending, (await service.GetByIdAsync(created.Id)).AfterStatus);
    }

    [Fact]
    public async Task ApproveAsync_RetryReturnsSamePickupTaskWithoutDuplicateAuditLog()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, null);
        var service = CreateService(context);
        var request = CreateRequest(seed, 1m);
        request.Goods[0].AfterSaleType = AfterSaleType.ReturnAndRefund;
        var created = await service.CreateAsync(request);
        await service.SubmitAsync(created.Id, null);

        var first = await service.ApproveAsync(created.Id, "同意退货");
        var repeated = await service.ApproveAsync(created.Id, "网络重试");

        Assert.Equal(Assert.Single(first.PickupTasks).Id, Assert.Single(repeated.PickupTasks).Id);
        Assert.Equal(2, repeated.AuditLogs.Count);
        Assert.Equal(1, await context.PickupTasks.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_RejectsDraftThatHasSubmissionHistory()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, null);
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest(seed, 1m));
        await service.SubmitAsync(created.Id, null);
        await service.RejectAsync(created.Id, "不符合条件");

        await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.DeleteAsync(created.Id));

        Assert.True(await context.AfterSales.AnyAsync(x => x.Id == created.Id));
    }

    [Fact]
    public async Task CreateAsync_RejectsQuantityThatRoundsToZero()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, null);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CreateAsync(CreateRequest(seed, 0.0000004m)));

        Assert.Contains("舍入后必须大于零", exception.Message);
        Assert.Empty(context.AfterSales);
    }

    [Fact]
    public async Task ApproveAsync_RejectsRemarkBeyondDatabaseLimit()
    {
        await using var context = CreateDbContext();
        var seed = await SeedOrderAsync(context, 10m, 25m, null);
        var service = CreateService(context);
        var created = await service.CreateAsync(CreateRequest(seed, 1m));
        await service.SubmitAsync(created.Id, null);

        await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.ApproveAsync(created.Id, new string('x', 501)));

        var unchanged = await service.GetByIdAsync(created.Id);
        Assert.Equal(AfterSaleStatus.PendingAudit, unchanged.AfterStatus);
        Assert.Single(unchanged.AuditLogs);
    }

    [Fact]
    public void AfterSaleMappingProfile_IsValid()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<AfterSaleMappingProfile>());
        configuration.AssertConfigurationIsValid();
    }

    private static CreateAfterSaleDto CreateRequest(OrderSeed seed, decimal quantity)
    {
        return new CreateAfterSaleDto
        {
            SaleOrderId = seed.OrderId,
            CustomerId = seed.CustomerId,
            Source = "后台建单",
            ContactName = "王老师",
            ContactPhone = "13800000000",
            PickupAddress = "学校正门",
            Goods =
            [
                new CreateAfterSaleGoodsDto
                {
                    SaleOrderDetailId = seed.OrderDetailId,
                    ActualRefundQuantity = quantity,
                    AfterSaleType = AfterSaleType.RefundOnly,
                    ReasonType = AfterSaleReasonType.QualityIssue,
                    HandleType = AfterSaleHandleType.GoodsDiscount
                }
            ]
        };
    }

    private static AfterSaleService CreateService(ApplicationDbContext context)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<AfterSaleMappingProfile>()).CreateMapper();
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
                new FakeCurrentUserService(),
                DocumentNoGeneratorTestDouble.Instance),
            new CustomerRepository(context),
            new GoodsRepository(context),
            new GoodsUnitRepository(context),
            new SupplierRepository(context),
            new DepartmentRepository(context),
            new RecordingUnitOfWork(context),
            mapper,
            new FakeCurrentUserService(),
            DocumentNoGeneratorTestDouble.Instance,
            new CreateAfterSaleValidator(goodsValidator),
            new UpdateAfterSaleValidator(goodsValidator),
            NullLogger<AfterSaleService>.Instance);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<OrderSeed> SeedOrderAsync(
        ApplicationDbContext context,
        decimal quantity,
        decimal totalPrice,
        decimal? acceptedBaseQuantity,
        SaleOrderStatus orderStatus = SaleOrderStatus.SortingPending)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "学校客户", Code = "SCHOOL" };
        var goodsType = new GoodsType { Id = Guid.NewGuid(), Name = "蔬菜", Code = "VEG" };
        var goods = new GoodsEntity
        {
            Id = Guid.NewGuid(),
            Name = "番茄",
            Code = "TOMATO",
            GoodsTypeId = goodsType.Id
        };
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
            OrderNo = "SO-AFTER-001",
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            CustomerCodeSnapshot = customer.Code,
            OrderDate = new DateTime(2026, 7, 6, 8, 0, 0, DateTimeKind.Utc),
            OrderStatus = orderStatus,
            OrderPrice = totalPrice,
            SettlementPrice = totalPrice
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
            Quantity = quantity,
            BaseQuantity = quantity,
            BaseUnitId = unit.Id,
            BaseUnitNameSnapshot = unit.Name,
            UnitConversion = 1m,
            FixedPrice = totalPrice / quantity,
            FixedGoodsUnitId = unit.Id,
            FixedGoodsUnitNameSnapshot = unit.Name,
            TotalPrice = totalPrice,
            CustomerCheckBaseQuantity = acceptedBaseQuantity
        };
        order.Details.Add(detail);

        await context.Customers.AddAsync(customer);
        await context.GoodsTypes.AddAsync(goodsType);
        await context.Goods.AddAsync(goods);
        await context.GoodsUnits.AddAsync(unit);
        await context.SaleOrders.AddAsync(order);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new OrderSeed(order.Id, detail.Id, customer.Id, goods.Id, unit.Id);
    }

    private sealed record OrderSeed(Guid OrderId, Guid OrderDetailId, Guid CustomerId, Guid GoodsId, Guid GoodsUnitId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string? GetUserName() => "after-sales-user";
        public string? GetEmail() => "after-sales@example.com";
        public string? GetRole() => "operator";
        public IReadOnlyList<string> GetRoles() => ["operator"];
        public bool HasClaim(string claimType, string claimValue) => false;
    }

    private sealed class RecordingUnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        public bool HasActiveTransaction { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            context.SaveChangesAsync(cancellationToken);

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
        public void ClearChangeTracking() => context.ChangeTracker.Clear();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
