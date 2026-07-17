using Application.DTOs.Finance;
using Application.interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validator.Finance;
using AutoMapper;
using Domain.Entities.Customers;
using Domain.Entities.Finance;
using Domain.Entities.Orders;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SkyRoc.Tests.Testing;
using Xunit;

namespace SkyRoc.Tests.Finance;

/// <summary>
/// 验证客户结款服务按账单余额创建凭证、区分部分和全部结款，并支持作废回滚。
/// </summary>
public class CustomerSettlementServiceTests
{
    [Fact]
    public async Task CreateAsync_SettlesBillsWithPaymentAndDiscount_AndUpdatesBillStatus()
    {
        await using var context = CreateDbContext();
        var seed = await SeedBillsAsync(context);
        var service = CreateService(context);

        var result = await service.CreateAsync(new CreateCustomerSettlementDto
        {
            SettlementDate = new DateTime(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc),
            SerialNo = "BANK-001",
            Details =
            [
                new CreateCustomerSettlementDetailDto
                {
                    CustomerBillId = seed.PartialBillId,
                    PaymentAmount = 30m,
                    DiscountAmount = 5m
                },
                new CreateCustomerSettlementDetailDto
                {
                    CustomerBillId = seed.FullBillId,
                    PaymentAmount = 40m,
                    DiscountAmount = 0m
                }
            ]
        });

        Assert.Equal(CustomerSettlementStatus.PartiallySettled, result.SettlementStatus);
        Assert.Equal(100m, result.ShouldAmount);
        Assert.Equal(70m, result.PaymentAmount);
        Assert.Equal(5m, result.DiscountAmount);
        Assert.Equal(75m, result.AppliedAmount);
        Assert.Equal(25m, result.RemainingAmount);
        Assert.Equal(2, result.Details.Count);

        var partialBill = await context.CustomerBills.SingleAsync(x => x.Id == seed.PartialBillId);
        var fullBill = await context.CustomerBills.SingleAsync(x => x.Id == seed.FullBillId);
        Assert.Equal(35m, partialBill.SettledAmount);
        Assert.Equal(CustomerBillStatus.PartiallySettled, partialBill.BillStatus);
        Assert.Equal(40m, fullBill.SettledAmount);
        Assert.Equal(CustomerBillStatus.Settled, fullBill.BillStatus);
    }

    [Fact]
    public async Task CreateAsync_RejectsCrossCustomerAndOverSettlement()
    {
        await using var context = CreateDbContext();
        var seed = await SeedBillsAsync(context);
        var otherBillId = await SeedOtherCustomerBillAsync(context);
        var service = CreateService(context);

        await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CreateAsync(new CreateCustomerSettlementDto
            {
                Details =
                [
                    new CreateCustomerSettlementDetailDto { CustomerBillId = seed.PartialBillId, PaymentAmount = 1m },
                    new CreateCustomerSettlementDetailDto { CustomerBillId = otherBillId, PaymentAmount = 1m }
                ]
            }));

        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CreateAsync(new CreateCustomerSettlementDto
            {
                Details =
                [
                    new CreateCustomerSettlementDetailDto
                    {
                        CustomerBillId = seed.PartialBillId,
                        PaymentAmount = 61m
                    }
                ]
            }));

        Assert.Contains("不能超过待结余额", exception.Message);
        Assert.Empty(context.CustomerSettlements);
    }

    [Fact]
    public async Task VoidAsync_RollsBackBillSettledAmountAndPreventsRepeatVoid()
    {
        await using var context = CreateDbContext();
        var seed = await SeedBillsAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(new CreateCustomerSettlementDto
        {
            Details =
            [
                new CreateCustomerSettlementDetailDto
                {
                    CustomerBillId = seed.FullBillId,
                    PaymentAmount = 40m
                }
            ]
        });

        var voided = await service.VoidAsync(created.Id, new VoidCustomerSettlementDto { Remark = "流水录错" });

        Assert.Equal(CustomerSettlementStatus.Voided, voided.SettlementStatus);
        Assert.Equal("finance-user", voided.VoidedByName);
        var fullBill = await context.CustomerBills.SingleAsync(x => x.Id == seed.FullBillId);
        Assert.Equal(0m, fullBill.SettledAmount);
        Assert.Equal(CustomerBillStatus.Pending, fullBill.BillStatus);

        await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.VoidAsync(created.Id, new VoidCustomerSettlementDto { Remark = "重复作废" }));
    }

    [Fact]
    public void FinanceMappingProfile_IsValid()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<FinanceMappingProfile>());
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public async Task CreateAsync_HandlesNullDetailsAsValidationError()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<Application.Exceptions.ValidationException>(() =>
            service.CreateAsync(new CreateCustomerSettlementDto { Details = null! }));

        Assert.Empty(context.CustomerSettlements);
    }

    private static CustomerSettlementService CreateService(ApplicationDbContext context)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<FinanceMappingProfile>()).CreateMapper();
        var detailValidator = new CreateCustomerSettlementDetailValidator();
        return new CustomerSettlementService(
            new CustomerBillRepository(context),
            new CustomerSettlementRepository(context),
            new RecordingUnitOfWork(context),
            mapper,
            new FakeCurrentUserService(),
            DocumentNoGeneratorTestDouble.Instance,
            new CreateCustomerSettlementValidator(detailValidator),
            new VoidCustomerSettlementValidator(),
            NullLogger<CustomerSettlementService>.Instance);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<SettlementSeed> SeedBillsAsync(ApplicationDbContext context)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "学校客户", Code = "SCHOOL" };
        var orderOne = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO-SETTLE-001",
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            CustomerCodeSnapshot = customer.Code,
            OrderDate = DateTime.UtcNow,
            OrderStatus = SaleOrderStatus.Signed,
            OrderPrice = 60m,
            SettlementPrice = 60m
        };
        var orderTwo = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO-SETTLE-002",
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            CustomerCodeSnapshot = customer.Code,
            OrderDate = DateTime.UtcNow,
            OrderStatus = SaleOrderStatus.Signed,
            OrderPrice = 40m,
            SettlementPrice = 40m
        };
        var partialBill = CreateBill(customer, orderOne, 60m);
        var fullBill = CreateBill(customer, orderTwo, 40m);

        await context.AddRangeAsync(customer, orderOne, orderTwo, partialBill, fullBill);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new SettlementSeed(partialBill.Id, fullBill.Id);
    }

    private static async Task<Guid> SeedOtherCustomerBillAsync(ApplicationDbContext context)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "其他客户", Code = "OTHER" };
        var order = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO-SETTLE-OTHER",
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            CustomerCodeSnapshot = customer.Code,
            OrderDate = DateTime.UtcNow,
            OrderStatus = SaleOrderStatus.Signed,
            OrderPrice = 10m,
            SettlementPrice = 10m
        };
        var bill = CreateBill(customer, order, 10m);
        await context.AddRangeAsync(customer, order, bill);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return bill.Id;
    }

    private static CustomerBill CreateBill(Customer customer, SaleOrder order, decimal receivableAmount)
    {
        return new CustomerBill
        {
            Id = Guid.NewGuid(),
            BillNo = $"CB-{order.OrderNo}",
            CustomerId = customer.Id,
            CustomerNameSnapshot = customer.Name,
            SaleOrderId = order.Id,
            SaleOrderNoSnapshot = order.OrderNo,
            BillDate = DateTime.UtcNow,
            OrderAmount = receivableAmount,
            ReceivableAmount = receivableAmount,
            BillStatus = CustomerBillStatus.Pending
        };
    }

    private sealed record SettlementSeed(Guid PartialBillId, Guid FullBillId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("33333333-3333-3333-3333-333333333333");
        public string? GetUserName() => "finance-user";
        public string? GetEmail() => "finance@example.com";
        public string? GetRole() => "finance";
        public IReadOnlyList<string> GetRoles() => ["finance"];
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
