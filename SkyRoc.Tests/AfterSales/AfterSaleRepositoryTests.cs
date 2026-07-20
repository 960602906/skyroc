using Application.QueryParameters.AfterSales;
using Domain.Entities.AfterSales;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SkyRoc.Tests.AfterSales;

/// <summary>
/// 验证售后列表专用投影的筛选、稳定分页与操作状态摘要。
/// </summary>
public class AfterSaleRepositoryTests
{
    /// <summary>
    /// 同创建时间记录必须按主键稳定倒序分页，并返回最新审核动作和取货任务标记。
    /// </summary>
    [Fact]
    public async Task GetListPageAsync_ReturnsStableLightweightPageAndOperationFlags()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAsync(context);
        var repository = new AfterSaleRepository(context);

        var firstPage = await repository.GetListPageAsync(null, 1, 2);
        var secondPage = await repository.GetListPageAsync(null, 2, 2);
        var emptyPage = await repository.GetListPageAsync(null, 3, 2);

        Assert.Equal(3, firstPage.Total);
        Assert.Equal([seed.SecondId, seed.FirstId], firstPage.Data.Select(x => x.Id));
        Assert.Equal(seed.OlderId, Assert.Single(secondPage.Data).Id);
        Assert.Empty(emptyPage.Data);

        var firstItem = firstPage.Data[0];
        Assert.Equal(AfterSaleAuditAction.Reject, firstItem.LatestAuditAction);
        Assert.True(firstItem.HasPickupTasks);
        Assert.Collection(
            firstItem.Goods,
            goods =>
            {
                Assert.Equal(AfterSaleType.RefundOnly, goods.AfterSaleType);
                Assert.Equal(AfterSaleHandleType.GoodsDiscount, goods.HandleType);
                Assert.Equal(1.23456m, goods.RefundAmount);
            },
            goods =>
            {
                Assert.Equal(AfterSaleType.ReturnAndRefund, goods.AfterSaleType);
                Assert.Equal(AfterSaleHandleType.Exchange, goods.HandleType);
                Assert.Equal(2.34567m, goods.RefundAmount);
            });
        Assert.Null(firstPage.Data[1].LatestAuditAction);
        Assert.False(firstPage.Data[1].HasPickupTasks);
    }

    /// <summary>
    /// 售后列表投影必须沿用现有关键字、状态、来源和商品处理条件。
    /// </summary>
    [Fact]
    public async Task GetListPageAsync_AppliesExistingBusinessFilters()
    {
        await using var context = CreateDbContext();
        var seed = await SeedAsync(context);
        var repository = new AfterSaleRepository(context);
        var parameters = new AfterSaleQueryParameters
        {
            Keyword = "学校",
            DateStart = seed.CreateTime.AddMinutes(-1),
            DateEnd = seed.CreateTime.AddMinutes(1),
            AfterStatus = AfterSaleStatus.Draft,
            CustomerId = seed.CustomerId,
            SaleOrderId = seed.SaleOrderId,
            AfterSaleType = AfterSaleType.ReturnAndRefund,
            HandleType = AfterSaleHandleType.Exchange
        };

        var result = await repository.GetListPageAsync(parameters.QueryBuild(), 1, 10);

        var item = Assert.Single(result.Data);
        Assert.Equal(seed.SecondId, item.Id);
        Assert.Equal(1, result.Total);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<AfterSaleListSeed> SeedAsync(ApplicationDbContext context)
    {
        var createTime = new DateTime(2026, 7, 20, 3, 0, 0, DateTimeKind.Utc);
        var customerId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var saleOrderId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var firstId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var olderId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var first = CreateAfterSale(firstId, "AF-001", createTime, customerId, saleOrderId);
        var second = CreateAfterSale(secondId, "AF-002", createTime, customerId, saleOrderId);
        var older = CreateAfterSale(olderId, "AF-OLD", createTime.AddDays(-1), Guid.NewGuid(), null);
        var firstGoods = CreateGoods(Guid.Parse("40000000-0000-0000-0000-000000000001"), firstId);
        var secondGoods1 = CreateGoods(Guid.Parse("40000000-0000-0000-0000-000000000002"), secondId);
        var secondGoods2 = CreateGoods(
            Guid.Parse("40000000-0000-0000-0000-000000000003"),
            secondId,
            AfterSaleType.ReturnAndRefund,
            AfterSaleHandleType.Exchange,
            2.34567m);
        var submitLog = CreateAuditLog(
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            secondId,
            AfterSaleAuditAction.Submit,
            createTime.AddMinutes(1));
        var rejectLog = CreateAuditLog(
            Guid.Parse("50000000-0000-0000-0000-000000000002"),
            secondId,
            AfterSaleAuditAction.Reject,
            createTime.AddMinutes(2));
        var pickupTask = new PickupTask
        {
            Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
            TaskNo = "PU-001",
            AfterSaleId = secondId,
            AfterSaleGoodsId = secondGoods1.Id,
            PickupAddressSnapshot = "学校正门"
        };

        await context.AddRangeAsync(
            first,
            second,
            older,
            firstGoods,
            secondGoods1,
            secondGoods2,
            submitLog,
            rejectLog,
            pickupTask);
        await context.SaveChangesAsync();
        first.CreateTime = createTime;
        second.CreateTime = createTime;
        older.CreateTime = createTime.AddDays(-1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new AfterSaleListSeed(firstId, secondId, olderId, customerId, saleOrderId, createTime);
    }

    private static AfterSale CreateAfterSale(
        Guid id,
        string afterSaleNo,
        DateTime createTime,
        Guid customerId,
        Guid? saleOrderId)
    {
        return new AfterSale
        {
            Id = id,
            AfterSaleNo = afterSaleNo,
            SaleOrderId = saleOrderId,
            SaleOrderNoSnapshot = saleOrderId.HasValue ? "SO-001" : null,
            CustomerId = customerId,
            CustomerNameSnapshot = saleOrderId.HasValue ? "学校客户" : "普通客户",
            Source = "后台建单",
            AfterStatus = AfterSaleStatus.Draft,
            OrderPrice = 10m,
            SettlementPrice = 8m,
            ContactNameSnapshot = "王老师",
            ContactPhoneSnapshot = "13800000000",
            CreateTime = createTime
        };
    }

    private static AfterSaleGoods CreateGoods(
        Guid id,
        Guid afterSaleId,
        AfterSaleType afterSaleType = AfterSaleType.RefundOnly,
        AfterSaleHandleType handleType = AfterSaleHandleType.GoodsDiscount,
        decimal refundAmount = 1.23456m)
    {
        return new AfterSaleGoods
        {
            Id = id,
            AfterSaleId = afterSaleId,
            GoodsId = Guid.NewGuid(),
            GoodsNameSnapshot = "番茄",
            GoodsCodeSnapshot = "TOMATO",
            GoodsUnitId = Guid.NewGuid(),
            GoodsUnitNameSnapshot = "千克",
            ConversionRate = 1m,
            AfterSaleType = afterSaleType,
            ActualRefundQuantity = 1m,
            BaseRefundQuantity = 1m,
            RefundAmount = refundAmount,
            HandleType = handleType,
            ReasonType = AfterSaleReasonType.QualityIssue
        };
    }

    private static AfterSaleAuditLog CreateAuditLog(
        Guid id,
        Guid afterSaleId,
        AfterSaleAuditAction action,
        DateTime auditTime)
    {
        return new AfterSaleAuditLog
        {
            Id = id,
            AfterSaleId = afterSaleId,
            Action = action,
            PreviousStatus = AfterSaleStatus.PendingAudit,
            CurrentStatus = AfterSaleStatus.Draft,
            AuditUserNameSnapshot = "审核员",
            AuditTime = auditTime,
            CreateTime = auditTime
        };
    }

    private sealed record AfterSaleListSeed(
        Guid FirstId,
        Guid SecondId,
        Guid OlderId,
        Guid CustomerId,
        Guid SaleOrderId,
        DateTime CreateTime);
}
