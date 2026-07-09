using Application.DTOs.Finance;
using Application.interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validator.Finance;
using AutoMapper;
using Domain.Entities.Finance;
using Domain.Entities.Purchases;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SkyRoc.Tests.Finance;

/// <summary>
/// 验证供应商结算服务按待结单据余额创建结算单、区分部分和全部结款，并支持作废回滚。
/// </summary>
public class SupplierSettlementServiceTests
{
    [Fact]
    public async Task CreateAsync_SettlesBillsWithPaymentAndDiscount_AndUpdatesBillStatus()
    {
        await using var context = CreateDbContext();
        var seed = await SeedBillsAsync(context);
        var service = CreateService(context);

        var result = await service.CreateAsync(new CreateSupplierSettlementDto
        {
            SettlementDate = new DateTime(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc),
            SerialNo = "BANK-SUP-001",
            Details =
            [
                new CreateSupplierSettlementDetailDto
                {
                    SupplierBillId = seed.PartialBillId,
                    PaymentAmount = 30m,
                    DiscountAmount = 5m
                },
                new CreateSupplierSettlementDetailDto
                {
                    SupplierBillId = seed.FullBillId,
                    PaymentAmount = 40m,
                    DiscountAmount = 0m
                }
            ]
        });

        Assert.Equal(SupplierSettlementStatus.PartiallySettled, result.SettlementStatus);
        Assert.Equal(100m, result.ShouldAmount);
        Assert.Equal(70m, result.PaymentAmount);
        Assert.Equal(5m, result.DiscountAmount);
        Assert.Equal(75m, result.AppliedAmount);
        Assert.Equal(25m, result.RemainingAmount);
        Assert.Equal(2, result.Details.Count);

        var partialBill = await context.SupplierBills.SingleAsync(x => x.Id == seed.PartialBillId);
        var fullBill = await context.SupplierBills.SingleAsync(x => x.Id == seed.FullBillId);
        Assert.Equal(35m, partialBill.SettledAmount);
        Assert.Equal(SupplierBillStatus.PartiallySettled, partialBill.BillStatus);
        Assert.Equal(40m, fullBill.SettledAmount);
        Assert.Equal(SupplierBillStatus.Settled, fullBill.BillStatus);
    }

    [Fact]
    public async Task CreateAsync_RejectsCrossSupplierAndOverSettlement()
    {
        await using var context = CreateDbContext();
        var seed = await SeedBillsAsync(context);
        var otherBillId = await SeedOtherSupplierBillAsync(context);
        var service = CreateService(context);

        await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CreateAsync(new CreateSupplierSettlementDto
            {
                Details =
                [
                    new CreateSupplierSettlementDetailDto { SupplierBillId = seed.PartialBillId, PaymentAmount = 1m },
                    new CreateSupplierSettlementDetailDto { SupplierBillId = otherBillId, PaymentAmount = 1m }
                ]
            }));

        var exception = await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.CreateAsync(new CreateSupplierSettlementDto
            {
                Details =
                [
                    new CreateSupplierSettlementDetailDto
                    {
                        SupplierBillId = seed.PartialBillId,
                        PaymentAmount = 61m
                    }
                ]
            }));

        Assert.Contains("不能超过待结余额", exception.Message);
        Assert.Empty(context.SupplierSettlements);
    }

    [Fact]
    public async Task VoidAsync_RollsBackBillSettledAmountAndPreventsRepeatVoid()
    {
        await using var context = CreateDbContext();
        var seed = await SeedBillsAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(new CreateSupplierSettlementDto
        {
            Details =
            [
                new CreateSupplierSettlementDetailDto
                {
                    SupplierBillId = seed.FullBillId,
                    PaymentAmount = 40m
                }
            ]
        });

        var voided = await service.VoidAsync(created.Id, new VoidSupplierSettlementDto { Remark = "流水录错" });

        Assert.Equal(SupplierSettlementStatus.Voided, voided.SettlementStatus);
        Assert.Equal("finance-user", voided.VoidedByName);
        var fullBill = await context.SupplierBills.SingleAsync(x => x.Id == seed.FullBillId);
        Assert.Equal(0m, fullBill.SettledAmount);
        Assert.Equal(SupplierBillStatus.Pending, fullBill.BillStatus);

        await Assert.ThrowsAsync<Application.Exceptions.BusinessException>(() =>
            service.VoidAsync(created.Id, new VoidSupplierSettlementDto { Remark = "重复作废" }));
    }

    [Fact]
    public async Task CreateAsync_HandlesNullDetailsAsValidationError()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<Application.Exceptions.ValidationException>(() =>
            service.CreateAsync(new CreateSupplierSettlementDto { Details = null! }));

        Assert.Empty(context.SupplierSettlements);
    }

    private static SupplierSettlementService CreateService(ApplicationDbContext context)
    {
        var mapper = new MapperConfiguration(config => config.AddProfile<FinanceMappingProfile>()).CreateMapper();
        var detailValidator = new CreateSupplierSettlementDetailValidator();
        return new SupplierSettlementService(
            new SupplierBillRepository(context),
            new SupplierSettlementRepository(context),
            new RecordingUnitOfWork(context),
            mapper,
            new FakeCurrentUserService(),
            new CreateSupplierSettlementValidator(detailValidator),
            new VoidSupplierSettlementValidator(),
            NullLogger<SupplierSettlementService>.Instance);
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
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "供应商A", Code = "SUP-A" };
        var partialBill = CreateBill(supplier, SupplierBillSourceType.PurchaseStockIn, "IN-001", 60m, 60m);
        var fullBill = CreateBill(supplier, SupplierBillSourceType.PurchaseStockIn, "IN-002", 40m, 40m);

        await context.AddRangeAsync(supplier, partialBill, fullBill);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new SettlementSeed(partialBill.Id, fullBill.Id);
    }

    private static async Task<Guid> SeedOtherSupplierBillAsync(ApplicationDbContext context)
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "供应商B", Code = "SUP-B" };
        var bill = CreateBill(supplier, SupplierBillSourceType.PurchaseStockIn, "IN-OTHER", 10m, 10m);
        await context.AddRangeAsync(supplier, bill);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return bill.Id;
    }

    private static SupplierBill CreateBill(
        Supplier supplier,
        SupplierBillSourceType sourceType,
        string sourceNo,
        decimal documentAmount,
        decimal payableAmount)
    {
        return new SupplierBill
        {
            Id = Guid.NewGuid(),
            BillNo = $"SB-{sourceNo}",
            SupplierId = supplier.Id,
            SupplierNameSnapshot = supplier.Name,
            SourceType = sourceType,
            StockInOrderId = Guid.NewGuid(),
            SourceDocumentNoSnapshot = sourceNo,
            BillDate = DateTime.UtcNow,
            DocumentAmount = documentAmount,
            PayableAmount = payableAmount,
            BillStatus = SupplierBillStatus.Pending
        };
    }

    private sealed record SettlementSeed(Guid PartialBillId, Guid FullBillId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("55555555-5555-5555-5555-555555555555");
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
