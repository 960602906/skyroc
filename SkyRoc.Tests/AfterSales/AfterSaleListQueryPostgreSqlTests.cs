using System.Data.Common;
using System.Diagnostics;
using Domain.Entities.AfterSales;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SkyRoc.Tests.AfterSales;

/// <summary>
/// 在显式提供的只读 PostgreSQL 环境中验证售后分页的数据库命令数量和 SQL 形状。
/// </summary>
public class AfterSaleListQueryPostgreSqlTests(ITestOutputHelper output)
{
    /// <summary>
    /// 一次售后分页必须固定为总数查询和单条列表数据查询，且不再加载订单明细或入库聚合。
    /// </summary>
    [Fact]
    public async Task GetListPageAsync_ExecutesTwoCommandsWithoutDetailAggregateJoins()
    {
        var connectionString = Environment.GetEnvironmentVariable("SKYROC_QUERY_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw SkipException.ForSkip(
                "需要显式设置 SKYROC_QUERY_TEST_CONNECTION_STRING 才运行只读 PostgreSQL 查询形状测试。");
        }

        var interceptor = new RecordingCommandInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var context = new ApplicationDbContext(options);

        await new AfterSaleRepository(context).GetListPageAsync(null, 1, 10);

        Assert.Equal(2, interceptor.Commands.Count);
        var dataSql = interceptor.Commands[1];
        var normalizedDataSql = dataSql.Replace("\"", string.Empty, StringComparison.Ordinal);
        Assert.Contains("after_sale_goods", normalizedDataSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sale_order_detail", normalizedDataSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stock_in_order", normalizedDataSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("join after_sale_audit_log", normalizedDataSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("join pickup_task", normalizedDataSql, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 同库预热后，新查询相对旧的完整聚合拆分查询，其分页总耗时 p50/p95 至少下降百分之四十。
    /// </summary>
    [Fact]
    public async Task GetListPageAsync_ReducesWarmDatabaseWaitPercentiles()
    {
        var connectionString = GetRequiredConnectionString();
        var timingInterceptor = new TimingCommandInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(timingInterceptor)
            .Options;
        await using var context = new ApplicationDbContext(options);
        var repository = new AfterSaleRepository(context);

        for (var iteration = 0; iteration < 30; iteration++)
        {
            await MeasureCommandsAsync(timingInterceptor, () => QueryLegacyPageAsync(context));
            await MeasureCommandsAsync(
                timingInterceptor,
                () => repository.GetListPageAsync(null, 1, 10));
        }

        var legacySamples = new List<double>();
        var optimizedSamples = new List<double>();
        for (var iteration = 0; iteration < 30; iteration++)
        {
            legacySamples.Add(await MeasureCommandsAsync(
                timingInterceptor,
                () => QueryLegacyPageAsync(context)));
            optimizedSamples.Add(await MeasureCommandsAsync(
                timingInterceptor,
                () => repository.GetListPageAsync(null, 1, 10)));
        }

        var legacyP50 = Percentile(legacySamples, 0.50);
        var legacyP95 = Percentile(legacySamples, 0.95);
        var optimizedP50 = Percentile(optimizedSamples, 0.50);
        var optimizedP95 = Percentile(optimizedSamples, 0.95);
        output.WriteLine(
            "旧查询累计 DbCommand p50={0:F2}ms, p95={1:F2}ms；新查询 p50={2:F2}ms, p95={3:F2}ms。",
            legacyP50,
            legacyP95,
            optimizedP50,
            optimizedP95);

        Assert.True(
            optimizedP50 <= legacyP50 * 0.60,
            $"新查询 p50 {optimizedP50:F2}ms 未比旧查询 {legacyP50:F2}ms 下降至少 40%。");
        Assert.True(
            optimizedP95 <= legacyP95 * 0.60,
            $"新查询 p95 {optimizedP95:F2}ms 未比旧查询 {legacyP95:F2}ms 下降至少 40%。");
    }

    private static async Task QueryLegacyPageAsync(ApplicationDbContext context)
    {
        var query = context.Set<AfterSale>()
            .AsNoTracking()
            .Include(x => x.SaleOrder)
                .ThenInclude(x => x!.Details)
            .Include(x => x.Goods)
            .Include(x => x.AuditLogs)
            .Include(x => x.PickupTasks)
                .ThenInclude(x => x.StockInDetail)
                    .ThenInclude(x => x!.StockInOrder)
            .AsSplitQuery();

        await query.CountAsync();
        await query
            .OrderByDescending(x => x.CreateTime)
            .ThenByDescending(x => x.Id)
            .Take(10)
            .ToListAsync();
    }

    private static async Task<double> MeasureCommandsAsync(
        TimingCommandInterceptor interceptor,
        Func<Task> action)
    {
        interceptor.Reset();
        await action();
        return interceptor.TotalMilliseconds;
    }

    private static async Task<double> MeasureCommandsAsync<T>(
        TimingCommandInterceptor interceptor,
        Func<Task<T>> action)
    {
        interceptor.Reset();
        await action();
        return interceptor.TotalMilliseconds;
    }

    private static double Percentile(IReadOnlyCollection<double> samples, double percentile)
    {
        var orderedSamples = samples.Order().ToArray();
        var index = Math.Max(0, (int)Math.Ceiling(orderedSamples.Length * percentile) - 1);
        return orderedSamples[index];
    }

    private static string GetRequiredConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("SKYROC_QUERY_TEST_CONNECTION_STRING");
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw SkipException.ForSkip(
                "需要显式设置 SKYROC_QUERY_TEST_CONNECTION_STRING 才运行只读 PostgreSQL 查询测试。")
            : connectionString;
    }

    private sealed class RecordingCommandInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class TimingCommandInterceptor : DbCommandInterceptor
    {
        private readonly List<double> durations = [];
        private readonly Dictionary<Guid, long> startTimestamps = [];

        public double TotalMilliseconds => durations.Sum();

        public void Reset()
        {
            durations.Clear();
            startTimestamps.Clear();
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            startTimestamps[eventData.CommandId] = Stopwatch.GetTimestamp();
            return ValueTask.FromResult(result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (startTimestamps.Remove(eventData.CommandId, out var startTimestamp))
            {
                durations.Add(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
            }

            return ValueTask.FromResult(result);
        }
    }
}
