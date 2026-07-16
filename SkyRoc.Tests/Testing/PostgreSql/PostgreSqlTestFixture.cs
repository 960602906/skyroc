using Infrastructure.Data;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Xunit;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     共享专用 PostgreSQL 测试库的迁移、事务、批次清理、Web 宿主与报告能力。
/// </summary>
public sealed class PostgreSqlTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlBatchCleaner _batchCleaner;
    private readonly DatabaseQualityReportGenerator _reportGenerator;

    /// <summary>
    ///     加载并验证专用测试库设置，但构造阶段不建立网络连接。
    /// </summary>
    public PostgreSqlTestFixture()
    {
        Settings = PostgreSqlTestSettingsLoader.Load();
        DatabaseName = DatabaseSafetyGuard.Validate(Settings);
        _batchCleaner = new PostgreSqlBatchCleaner(Settings);
        _reportGenerator = new DatabaseQualityReportGenerator(Settings);
        ObjectStorage = new InMemoryObjectStorage();
    }

    /// <summary>
    ///     跨 Web 宿主共享的进程内对象存储，保证联调文件在多次生成间可复用校验。
    /// </summary>
    public IObjectStorage ObjectStorage { get; }

    /// <summary>
    ///     当前真实 PostgreSQL 测试设置。
    /// </summary>
    public PostgreSqlTestSettings Settings { get; }

    /// <summary>
    ///     已通过精确白名单验证的数据库名。
    /// </summary>
    public string DatabaseName { get; }

    /// <summary>
    ///     在测试集合开始时安全应用所有待执行迁移。
    /// </summary>
    public async Task InitializeAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    ///     夹具不持有跨测试连接，无需删除或重建数据库。
    /// </summary>
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     创建指向白名单库的新 DbContext 连接。
    /// </summary>
    public ApplicationDbContext CreateDbContext()
    {
        DatabaseSafetyGuard.Validate(Settings);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Settings.ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    /// <summary>
    ///     创建使用同一白名单库的真实 Web 测试宿主。
    /// </summary>
    public PostgreSqlWebApplicationFactory CreateWebApplicationFactory()
    {
        return new PostgreSqlWebApplicationFactory(Settings, ObjectStorage);
    }

    /// <summary>
    ///     在单连接事务中执行探针，并无论成功或异常都回滚全部写入。
    /// </summary>
    public async Task ExecuteInRollbackTransactionAsync(Func<ApplicationDbContext, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var context = CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await action(context);
        }
        finally
        {
            await transaction.RollbackAsync();
            context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    ///     按登记逆序精确清理本轮跨连接临时记录。
    /// </summary>
    public async Task CleanupBatchAsync(BatchCleanupRegistry registry)
    {
        await using var context = CreateDbContext();
        await _batchCleaner.CleanupAsync(context, registry);
    }

    /// <summary>
    ///     只读扫描当前数据库并写入 JSON 与 Markdown 基础质量报告。
    /// </summary>
    public async Task<DataQualityReportResult> GenerateQualityReportAsync(string runId)
    {
        await using var context = CreateDbContext();
        var report = await _reportGenerator.GenerateAsync(context, runId);
        var paths = await DataQualityReportWriter.WriteAsync(report, Settings.ReportDirectory);
        return new DataQualityReportResult(report, paths);
    }

    /// <summary>
    ///     在通过数据库白名单守卫后幂等补齐长期前端联调数据。
    /// </summary>
    /// <param name="cancellationToken">取消生成的令牌。</param>
    /// <returns>本轮各生成层的新增与复用数量。</returns>
    public Task<DemoDataGenerationResult> GenerateDemoDataAsync(CancellationToken cancellationToken = default)
    {
        return new DemoDataGenerator(this).GenerateAsync(cancellationToken);
    }
}
