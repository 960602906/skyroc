using Application.DTOs.Delivery;
using Application.Exceptions;
using Application.interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Customers;
using Domain.Entities.Delivery;
using Domain.Entities.Orders;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Delivery;

/// <summary>
/// 覆盖配送任务生成、司机分配、路线规划、异常登记及非法状态保护。
/// </summary>
public class DeliveryTaskServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task GenerateFromStockOutAsync_CreatesSnapshotAndIsIdempotent_WhenSaleOutboundIsAudited()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedSaleOutboundAsync(context);
        var service = CreateTaskService(context);

        var first = await service.GenerateFromStockOutAsync(seed.StockOutOrderId);
        var second = await service.GenerateFromStockOutAsync(seed.StockOutOrderId);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(DeliveryTaskStatus.PendingAssign, first.DeliveryStatus);
        Assert.Equal("第一食堂", first.CustomerName);
        Assert.Equal("李老师", first.ContactName);
        Assert.Equal("上海市浦东新区一号门", first.DeliveryAddress);
        Assert.Equal(seed.StockOutOrderId, first.StockOutOrderId);
        Assert.Equal(CurrentUserId, (await context.DeliveryTasks.SingleAsync()).CreateBy);
        Assert.Equal(1, await context.DeliveryTasks.CountAsync());
    }

    [Fact]
    public async Task GenerateFromStockOutAsync_RejectsUnauditedOrNonSaleOutbound()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedSaleOutboundAsync(context);
        var stockOut = await context.StockOutOrders.SingleAsync(x => x.Id == seed.StockOutOrderId);
        stockOut.BusinessStatus = StockDocumentStatus.Draft;
        await context.SaveChangesAsync();
        var service = CreateTaskService(context);

        var unaudited = await Assert.ThrowsAsync<BusinessException>(
            () => service.GenerateFromStockOutAsync(seed.StockOutOrderId));
        Assert.Contains("审核通过", unaudited.Message);

        stockOut.BusinessStatus = StockDocumentStatus.Audited;
        stockOut.OrderType = StockOutOrderType.Other;
        await context.SaveChangesAsync();
        var nonSale = await Assert.ThrowsAsync<BusinessException>(
            () => service.GenerateFromStockOutAsync(seed.StockOutOrderId));
        Assert.Contains("销售出库单", nonSale.Message);
    }

    [Fact]
    public async Task AssignDriverAsync_SavesDriverAndCarrierSnapshots_AndDriverQueryReturnsTask()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedSaleOutboundAsync(context);
        var carrier = new Carrier { Id = Guid.NewGuid(), Name = "城配物流", Code = "CITY" };
        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            Name = "张师傅",
            Code = "D001",
            Phone = "13900001111",
            CarrierId = carrier.Id
        };
        await context.AddRangeAsync(carrier, driver);
        await context.SaveChangesAsync();
        var service = CreateTaskService(context);
        var task = await service.GenerateFromStockOutAsync(seed.StockOutOrderId);

        var assigned = Assert.Single(await service.AssignDriverAsync(new AssignDeliveryDriverDto
        {
            TaskIds = [task.Id],
            DriverId = driver.Id
        }));
        var driverPage = await service.GetDriverTasksAsync(new Application.QueryParameters.DeliveryTaskQueryParameters());

        Assert.Equal(DeliveryTaskStatus.Assigned, assigned.DeliveryStatus);
        Assert.Equal("张师傅", assigned.DriverName);
        Assert.Equal("城配物流", assigned.CarrierName);
        Assert.NotNull(assigned.AssignedTime);
        Assert.Single(driverPage.Records!);
    }

    [Fact]
    public async Task AssignDriverAsync_RejectsDisabledDriverWithoutChangingTask()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedSaleOutboundAsync(context);
        var driver = new Driver { Id = Guid.NewGuid(), Name = "停用司机", Code = "D002", Status = Status.Disable };
        await context.Drivers.AddAsync(driver);
        await context.SaveChangesAsync();
        var service = CreateTaskService(context);
        var task = await service.GenerateFromStockOutAsync(seed.StockOutOrderId);

        await Assert.ThrowsAsync<BusinessException>(() => service.AssignDriverAsync(new AssignDeliveryDriverDto
        {
            TaskIds = [task.Id],
            DriverId = driver.Id
        }));

        Assert.Null((await context.DeliveryTasks.SingleAsync()).DriverId);
    }

    [Fact]
    public async Task BatchOperations_RejectMoreThanOneHundredTasks()
    {
        await using var context = CreateDbContext();
        var service = CreateTaskService(context);
        var taskIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();

        await Assert.ThrowsAsync<ValidationException>(() => service.AssignDriverAsync(new AssignDeliveryDriverDto
        {
            TaskIds = taskIds,
            DriverId = Guid.NewGuid()
        }));
        await Assert.ThrowsAsync<ValidationException>(() => service.IntelligentPlanAsync(
            new IntelligentPlanDeliveryTasksDto { TaskIds = taskIds }));
    }

    [Fact]
    public async Task BatchOperations_RejectNullTaskCollectionsAsValidationErrors()
    {
        await using var context = CreateDbContext();
        var service = CreateTaskService(context);

        await Assert.ThrowsAsync<ValidationException>(() => service.AssignDriverAsync(new AssignDeliveryDriverDto
        {
            TaskIds = null!,
            DriverId = Guid.NewGuid()
        }));
        await Assert.ThrowsAsync<ValidationException>(() => service.IntelligentPlanAsync(
            new IntelligentPlanDeliveryTasksDto { TaskIds = null! }));
    }

    [Fact]
    public async Task IntelligentPlanAsync_SelectsEnabledRouteByRouteAndCustomerSort()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedSaleOutboundAsync(context);
        var firstRoute = new DeliveryRoute { Id = Guid.NewGuid(), Name = "优先路线", Code = "R1", Sort = 1 };
        var secondRoute = new DeliveryRoute { Id = Guid.NewGuid(), Name = "备用路线", Code = "R2", Sort = 2 };
        await context.DeliveryRoutes.AddRangeAsync(firstRoute, secondRoute);
        await context.CustomerRoutes.AddRangeAsync(
            new CustomerRoute { Id = Guid.NewGuid(), RouteId = secondRoute.Id, CustomerId = seed.CustomerId, Sort = 1 },
            new CustomerRoute { Id = Guid.NewGuid(), RouteId = firstRoute.Id, CustomerId = seed.CustomerId, Sort = 8 });
        await context.SaveChangesAsync();
        var service = CreateTaskService(context);
        var task = await service.GenerateFromStockOutAsync(seed.StockOutOrderId);

        var planned = Assert.Single(await service.IntelligentPlanAsync(new IntelligentPlanDeliveryTasksDto
        {
            TaskIds = [task.Id]
        }));

        Assert.Equal(firstRoute.Id, planned.RouteId);
        Assert.Equal("优先路线", planned.RouteName);
        Assert.Equal(8, planned.RouteSequence);
        Assert.NotNull(planned.PlannedTime);
    }

    [Fact]
    public async Task IntelligentPlanAsync_RejectsMissingCustomerRouteWithoutPartialUpdate()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedSaleOutboundAsync(context);
        var service = CreateTaskService(context);
        var task = await service.GenerateFromStockOutAsync(seed.StockOutOrderId);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.IntelligentPlanAsync(
            new IntelligentPlanDeliveryTasksDto { TaskIds = [task.Id] }));

        Assert.Contains("未配置启用配送路线", exception.Message);
        Assert.Null((await context.DeliveryTasks.AsNoTracking().SingleAsync()).RouteId);
    }

    [Fact]
    public async Task CreateExceptionAsync_DerivesTaskRelationsAndMarksTaskException()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedSaleOutboundAsync(context);
        var driver = new Driver { Id = Guid.NewGuid(), Name = "王师傅", Code = "D003" };
        await context.Drivers.AddAsync(driver);
        await context.SaveChangesAsync();
        var taskService = CreateTaskService(context);
        var task = await taskService.GenerateFromStockOutAsync(seed.StockOutOrderId);
        await taskService.AssignDriverAsync(new AssignDeliveryDriverDto
        {
            TaskIds = [task.Id],
            DriverId = driver.Id
        });
        var exceptionService = CreateExceptionService(context);

        var result = await exceptionService.CreateAsync(new CreateDeliveryExceptionDto
        {
            DeliveryTaskId = task.Id,
            Description = "  客户临时闭店，无法交付  "
        });

        Assert.Equal(task.Id, result.DeliveryTaskId);
        Assert.Equal(driver.Id, result.DriverId);
        Assert.Equal(seed.CustomerId, result.CustomerId);
        Assert.Equal("客户临时闭店，无法交付", result.Description);
        Assert.Equal(DeliveryExceptionStatus.Pending, result.HandleStatus);
        Assert.Equal(DeliveryTaskStatus.Exception, (await context.DeliveryTasks.SingleAsync()).DeliveryStatus);
    }

    [Fact]
    public async Task CreateExceptionAsync_RejectsUnassignedTask()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAuditedSaleOutboundAsync(context);
        var task = await CreateTaskService(context).GenerateFromStockOutAsync(seed.StockOutOrderId);
        var service = CreateExceptionService(context);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(
            new CreateDeliveryExceptionDto { DeliveryTaskId = task.Id, Description = "无法联系客户" }));

        Assert.Contains("分配司机", exception.Message);
        Assert.Empty(await context.DeliveryExceptions.ToListAsync());
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static DeliveryTaskService CreateTaskService(ApplicationDbContext context)
    {
        return new DeliveryTaskService(
            new DeliveryTaskRepository(context),
            new StockOutOrderRepository(context),
            new SaleOrderRepository(context),
            new DriverRepository(context),
            new DeliveryRouteRepository(context),
            new RecordingUnitOfWork(context),
            CreateMapper(),
            new FakeCurrentUserService(),
            new AssignDeliveryDriverValidator(),
            new IntelligentPlanDeliveryTasksValidator(),
            NullLogger<DeliveryTaskService>.Instance);
    }

    private static DeliveryExceptionService CreateExceptionService(ApplicationDbContext context)
    {
        return new DeliveryExceptionService(
            new DeliveryExceptionRepository(context),
            new DeliveryTaskRepository(context),
            new RecordingUnitOfWork(context),
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateDeliveryExceptionValidator(),
            NullLogger<DeliveryExceptionService>.Instance);
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<DeliveryTaskMappingProfile>()).CreateMapper();
    }

    private static async Task<DeliverySeed> SeedAuditedSaleOutboundAsync(ApplicationDbContext context)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "第一食堂", Code = "C001" };
        var ware = new Ware { Id = Guid.NewGuid(), Name = "中心仓", Code = "W001" };
        var saleOrder = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO20260704001",
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            CustomerCodeSnapshot = customer.Code,
            ContactNameSnapshot = "李老师",
            ContactPhoneSnapshot = "13800001234",
            DeliveryAddressSnapshot = "上海市浦东新区一号门",
            OrderDate = new DateTime(2026, 7, 4, 1, 0, 0, DateTimeKind.Utc)
        };
        var stockOut = new StockOutOrder
        {
            Id = Guid.NewGuid(),
            OutNo = "OUT20260704001",
            OrderType = StockOutOrderType.Sale,
            BusinessStatus = StockDocumentStatus.Audited,
            WareId = ware.Id,
            WareNameSnapshot = ware.Name,
            SaleOrderId = saleOrder.Id,
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            OutTime = new DateTime(2026, 7, 4, 2, 0, 0, DateTimeKind.Utc)
        };
        await context.AddRangeAsync(customer, ware, saleOrder, stockOut);
        await context.SaveChangesAsync();
        return new DeliverySeed(customer.Id, stockOut.Id);
    }

    private sealed record DeliverySeed(Guid CustomerId, Guid StockOutOrderId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => CurrentUserId;
        public string? GetUserName() => "delivery-user";
        public string? GetEmail() => "delivery@example.com";
        public string? GetRole() => "dispatcher";
        public IReadOnlyList<string> GetRoles() => ["dispatcher"];
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
        public void ClearChangeTracking() => context.ChangeTracker.Clear();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
