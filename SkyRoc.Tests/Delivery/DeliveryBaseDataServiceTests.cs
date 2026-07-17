using Application.DTOs.Delivery;
using Application.Exceptions;
using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Customers;
using Domain.Entities.Delivery;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Delivery;

/// <summary>
/// 覆盖承运商、司机、配送路线基础资料的 CRUD 行为以及配送异常仓储的增删改查。
/// </summary>
public class DeliveryBaseDataServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Create_carrier_should_persist_contact_details_and_audit()
    {
        await using var context = CreateDbContext();

        var result = await CreateCarrierService(context).CreateAsync(new CreateCarrierDto
        {
            Name = "顺丰同城",
            Code = "SF_CITY",
            ContactName = "王调度",
            ContactPhone = "13800002222",
            Address = "上海市浦东新区"
        });

        var saved = await context.Carriers.SingleAsync(x => x.Id == result.Id);
        Assert.Equal("王调度", saved.ContactName);
        Assert.Equal("13800002222", saved.ContactPhone);
        Assert.Equal("上海市浦东新区", saved.Address);
        Assert.Equal(Status.Enable, saved.Status);
        Assert.Equal(CurrentUserId, saved.CreateBy);
    }

    [Fact]
    public async Task Create_carrier_should_reject_duplicate_code()
    {
        await using var context = CreateDbContext();
        var service = CreateCarrierService(context);
        await service.CreateAsync(new CreateCarrierDto { Name = "顺丰同城", Code = "SF_CITY" });

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(
            new CreateCarrierDto { Name = "顺丰快递", Code = "SF_CITY" }));

        Assert.Contains("编码", exception.Message);
    }

    [Fact]
    public async Task Create_driver_should_bind_carrier_and_expose_carrier_name()
    {
        await using var context = CreateDbContext();
        var carrier = new Carrier { Id = Guid.NewGuid(), Name = "顺丰同城", Code = "SF_CITY" };
        await context.Carriers.AddAsync(carrier);
        await context.SaveChangesAsync();
        var service = CreateDriverService(context);

        var created = await service.CreateAsync(new CreateDriverDto
        {
            Name = "张师傅",
            Code = "DRIVER_ZHANG",
            Phone = "13900003333",
            CarrierId = carrier.Id,
            PlateNumber = "沪A12345",
            LicenseNo = "310101199001011234"
        });
        var detail = await service.GetByIdAsync(created.Id);

        var saved = await context.Drivers.SingleAsync(x => x.Id == created.Id);
        Assert.Equal(carrier.Id, saved.CarrierId);
        Assert.Equal("沪A12345", saved.PlateNumber);
        Assert.Equal("顺丰同城", detail.CarrierName);
        Assert.Equal(CurrentUserId, saved.CreateBy);
    }

    [Fact]
    public async Task Create_driver_should_reject_missing_carrier()
    {
        await using var context = CreateDbContext();
        var service = CreateDriverService(context);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(new CreateDriverDto
        {
            Name = "李师傅",
            Code = "DRIVER_LI",
            CarrierId = Guid.NewGuid()
        }));

        Assert.Equal("所属承运商不存在", exception.Message);
    }

    [Fact]
    public async Task Toggle_driver_status_should_persist_disabled_state()
    {
        await using var context = CreateDbContext();
        var service = CreateDriverService(context);
        var driver = await service.CreateAsync(new CreateDriverDto { Name = "赵师傅", Code = "DRIVER_ZHAO" });

        var result = await service.ToggleStatusAsync(driver.Id, Status.Disable);

        Assert.Equal(Status.Disable, result.Status);
        var saved = await context.Drivers.SingleAsync(x => x.Id == driver.Id);
        Assert.Equal(Status.Disable, saved.Status);
        Assert.Equal(CurrentUserId, saved.UpdateBy);
    }

    [Fact]
    public async Task Update_driver_without_status_should_preserve_existing_status()
    {
        await using var context = CreateDbContext();
        var service = CreateDriverService(context);
        var driver = await service.CreateAsync(new CreateDriverDto { Name = "钱师傅", Code = "DRIVER_QIAN" });
        await service.ToggleStatusAsync(driver.Id, Status.Disable);

        await service.UpdateAsync(driver.Id, new UpdateDriverDto
        {
            Id = driver.Id,
            Name = "钱师傅",
            Code = "DRIVER_QIAN",
            Phone = "13700004444"
        });

        var saved = await context.Drivers.SingleAsync(x => x.Id == driver.Id);
        Assert.Equal(Status.Disable, saved.Status);
        Assert.Equal("13700004444", saved.Phone);
    }

    [Fact]
    public async Task Delete_driver_should_reject_driver_referenced_by_delivery_task()
    {
        await using var context = CreateDbContext();
        var service = CreateDriverService(context);
        var driver = await service.CreateAsync(new CreateDriverDto { Name = "配送司机", Code = "DRIVER_TASK" });
        await context.DeliveryTasks.AddAsync(new DeliveryTask
        {
            Id = Guid.NewGuid(),
            TaskNo = "DT20260704002",
            StockOutOrderId = Guid.NewGuid(),
            SaleOrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CustomerNameSnapshot = "测试客户",
            WareId = Guid.NewGuid(),
            WareNameSnapshot = "测试仓库",
            DriverId = driver.Id,
            DriverNameSnapshot = driver.Name,
            DeliveryStatus = DeliveryTaskStatus.Assigned,
            OutTime = new DateTime(2026, 7, 4, 2, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(driver.Id));

        Assert.Contains("已关联配送任务", exception.Message);
        Assert.NotNull(await context.Drivers.SingleOrDefaultAsync(x => x.Id == driver.Id));
    }

    [Fact]
    public async Task Create_route_should_persist_initial_customer_relations()
    {
        await using var context = CreateDbContext();
        var customers = await SeedCustomersAsync(context, 2);
        var service = CreateDeliveryRouteService(context);

        var created = await service.CreateAsync(new CreateDeliveryRouteDto
        {
            Name = "浦东一号线",
            Code = "PD_ROUTE_1",
            Description = "覆盖浦东张江片区",
            Sort = 1,
            CustomerIds = [customers[0], customers[1], customers[0], Guid.Empty]
        });
        var detail = await service.GetByIdAsync(created.Id);

        var relations = await context.CustomerRoutes.Where(x => x.RouteId == created.Id).ToListAsync();
        Assert.Equal(2, relations.Count);
        Assert.Equal(customers.OrderBy(x => x), detail.CustomerIds!.OrderBy(x => x));
        Assert.Equal(CurrentUserId, (await context.DeliveryRoutes.SingleAsync()).CreateBy);
    }

    [Fact]
    public async Task Update_route_should_replace_customer_relations()
    {
        await using var context = CreateDbContext();
        var customers = await SeedCustomersAsync(context, 2);
        var service = CreateDeliveryRouteService(context);
        var created = await service.CreateAsync(new CreateDeliveryRouteDto
        {
            Name = "浦东一号线",
            Code = "PD_ROUTE_1",
            Sort = 1,
            CustomerIds = [customers[0]]
        });

        await service.UpdateAsync(created.Id, new UpdateDeliveryRouteDto
        {
            Id = created.Id,
            Name = "浦东一号线",
            Code = "PD_ROUTE_1",
            Sort = 2,
            CustomerIds = [customers[1]]
        });

        var relation = await context.CustomerRoutes.SingleAsync(x => x.RouteId == created.Id);
        Assert.Equal(customers[1], relation.CustomerId);
        Assert.Equal(2, (await context.DeliveryRoutes.SingleAsync()).Sort);
    }

    [Fact]
    public async Task Dispatch_customers_should_replace_relations_and_reject_missing_customer()
    {
        await using var context = CreateDbContext();
        var customers = await SeedCustomersAsync(context, 2);
        var service = CreateDeliveryRouteService(context);
        var route = await service.CreateAsync(new CreateDeliveryRouteDto
        {
            Name = "浦东一号线",
            Code = "PD_ROUTE_1",
            Sort = 1,
            CustomerIds = [customers[0]]
        });

        var dispatched = await service.DispatchCustomersAsync(route.Id, [customers[1]]);
        Assert.Equal([customers[1]], dispatched.CustomerIds);
        Assert.Equal(CurrentUserId, (await context.DeliveryRoutes.SingleAsync()).UpdateBy);

        var cleared = await service.DispatchCustomersAsync(route.Id, []);
        Assert.Empty(cleared.CustomerIds!);
        Assert.Empty(await context.CustomerRoutes.Where(x => x.RouteId == route.Id).ToListAsync());

        await Assert.ThrowsAsync<BusinessException>(() => service.DispatchCustomersAsync(route.Id, [Guid.NewGuid()]));
    }

    [Fact]
    public async Task Delete_route_should_cascade_customer_relations()
    {
        await using var context = CreateDbContext();
        var customers = await SeedCustomersAsync(context, 1);
        var service = CreateDeliveryRouteService(context);
        var route = await service.CreateAsync(new CreateDeliveryRouteDto
        {
            Name = "浦东一号线",
            Code = "PD_ROUTE_1",
            Sort = 1,
            CustomerIds = [customers[0]]
        });

        await service.DeleteAsync(route.Id);

        Assert.Empty(await context.DeliveryRoutes.ToListAsync());
        Assert.Empty(await context.CustomerRoutes.ToListAsync());
    }

    [Fact]
    public async Task Delivery_exception_repository_should_support_crud_and_number_uniqueness_check()
    {
        await using var context = CreateDbContext();
        var repository = new DeliveryExceptionRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var exception = new DeliveryException
        {
            Id = Guid.NewGuid(),
            ExceptionNo = "DE20260704001",
            Description = "客户不在配送地址，无法签收"
        };

        await repository.AddAsync(exception);
        await unitOfWork.SaveChangesAsync();

        Assert.True(await repository.ExistsByExceptionNoAsync("DE20260704001"));
        Assert.False(await repository.ExistsByExceptionNoAsync("DE20260704001", exception.Id));

        var saved = await repository.GetByIdAsync(exception.Id);
        Assert.NotNull(saved);
        Assert.Equal(DeliveryExceptionStatus.Pending, saved!.HandleStatus);

        saved.HandleStatus = DeliveryExceptionStatus.Handled;
        saved.HandleRemark = "已电话联系客户改约";
        saved.HandleTime = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc);
        await repository.UpdateAsync(saved);
        await unitOfWork.SaveChangesAsync();

        var handled = await repository.GetByIdAsync(exception.Id);
        Assert.Equal(DeliveryExceptionStatus.Handled, handled!.HandleStatus);
        Assert.Equal("已电话联系客户改约", handled.HandleRemark);

        await repository.DeleteAsync(handled);
        await unitOfWork.SaveChangesAsync();
        Assert.Null(await repository.GetByIdAsync(exception.Id));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CarrierService CreateCarrierService(ApplicationDbContext context)
    {
        return new CarrierService(
            new CarrierRepository(context),
            new UnitOfWork(context),
            NullLogger<CarrierService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateCarrierValidator(),
            new UpdateCarrierValidator());
    }

    private static DriverService CreateDriverService(ApplicationDbContext context)
    {
        return new DriverService(
            new DriverRepository(context),
            new CarrierRepository(context),
            new UnitOfWork(context),
            NullLogger<DriverService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateDriverValidator(),
            new UpdateDriverValidator());
    }

    private static DeliveryRouteService CreateDeliveryRouteService(ApplicationDbContext context)
    {
        return new DeliveryRouteService(
            new DeliveryRouteRepository(context),
            new CustomerRepository(context),
            new UnitOfWork(context),
            NullLogger<DeliveryRouteService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateDeliveryRouteValidator(),
            new UpdateDeliveryRouteValidator());
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<BaseDataMappingProfile>()).CreateMapper();
    }

    private static async Task<List<Guid>> SeedCustomersAsync(ApplicationDbContext context, int count)
    {
        var customers = Enumerable.Range(1, count)
            .Select(index => new Customer
            {
                Id = Guid.NewGuid(),
                Name = $"客户{index}",
                Code = $"CUSTOMER_{index}"
            })
            .ToList();

        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();
        return customers.Select(x => x.Id).ToList();
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => CurrentUserId;

        public string? GetUserName() => "test-user";

        public string? GetEmail() => "test-user@example.com";

        public string? GetRole() => "test-role";

        public IReadOnlyList<string> GetRoles() => ["admin"];

        public bool HasClaim(string claimType, string claimValue) => false;
    }
}
