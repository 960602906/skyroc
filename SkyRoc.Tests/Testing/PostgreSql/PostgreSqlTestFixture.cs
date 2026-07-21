using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using Xunit;
using Menu = Domain.Entities.Menu;

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
    ///     在测试集合开始时删除并重建专用测试库，确保每轮测试从干净状态启动，并写入系统预置角色。
    /// </summary>
    public async Task InitializeAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        // 写入系统预置角色（Admin/User），让权限测试可以复用这些角色
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role
                {
                    Name = "管理员",
                    Code = "Admin",
                    Desc = "系统管理员，拥有所有权限"
                },
                new Role
                {
                    Name = "用户",
                    Code = "User",
                    Desc = "普通用户，拥有基本权限"
                });
            await context.SaveChangesAsync();
        }

        // 写入初始 admin 用户，供 DemoDataGenerator 作为审计用户基础
        if (!await context.Users.AnyAsync())
        {
            context.Users.Add(new User
            {
                Username = "admin",
                NickName = "系统管理员",
                Email = "admin@test.example",
                Gender = GenderType.Male,
                PasswordHash = PasswordHasher.Hash("Admin@123")
            });
            await context.SaveChangesAsync();
        }

        // 写入必需的基础菜单（home, manage, manage_role, manage_user），供 DemoDataGenerator 的角色权限关联使用
        if (!await context.Menus.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var homeMenu = new Menu
            {
                Name = "home",
                Path = "/home",
                Component = "page.(base)_home",
                Title = "home",
                I18NKey = "route.(base)_home",
                Order = 1,
                Status = Status.Enable,
                CreateTime = now
            };
            var manageMenu = new Menu
            {
                Name = "manage",
                Path = "/manage",
                Component = "page.(base)_manage",
                Title = "manage",
                I18NKey = "route.(base)_manage",
                Order = 8,
                Status = Status.Enable,
                CreateTime = now
            };
            context.Menus.AddRange(homeMenu, manageMenu);
            await context.SaveChangesAsync();

            context.Menus.AddRange(
                new Menu
                {
                    Name = "manage_user",
                    Path = "/manage/user",
                    Component = "page.(base)_manage_user",
                    Title = "manage_user",
                    I18NKey = "route.(base)_manage_user",
                    Order = 1,
                    KeepAlive = true,
                    ParentId = manageMenu.Id,
                    Status = Status.Enable,
                    CreateTime = now
                },
                new Menu
                {
                    Name = "manage_role",
                    Path = "/manage/role",
                    Component = "page.(base)_manage_role",
                    Title = "manage_role",
                    I18NKey = "route.(base)_manage_role",
                    Order = 2,
                    ParentId = manageMenu.Id,
                    Status = Status.Enable,
                    CreateTime = now
                });
            await context.SaveChangesAsync();
        }

        // 预生成长期联调数据，供所有依赖受管稳定键的测试使用
        await GenerateDemoDataAsync();
    }

    /// <summary>
    ///     测试集合结束时无需额外清理，数据库会在下次运行时重建。
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
    /// <param name="configureTestServices">可选的测试服务替换/装饰回调（例如故障注入门闩）。</param>
    public PostgreSqlWebApplicationFactory CreateWebApplicationFactory(
        Action<IServiceCollection>? configureTestServices = null)
    {
        return new PostgreSqlWebApplicationFactory(Settings, ObjectStorage, configureTestServices);
    }

    /// <summary>
    ///     在单连接事务中执行探针，并无论成功或异常都回滚全部写入。
    /// </summary>
    public async Task ExecuteInRollbackTransactionAsync(Func<ApplicationDbContext, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var context = CreateDbContext();
        // 与生产 EnableRetryOnFailure 对齐：手动事务必须包在执行策略内
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
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
        });
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
    ///     清理历史轮次遗留的 <c>SKYROC-AUTOTEST-</c> 临时残留，并回填登录审计空 IP。
    ///     仅供 T14 质量门禁收口使用。
    /// </summary>
    public async Task CleanupStaleAutotestBatchesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = CreateDbContext();
        await new PostgreSqlStaleBatchCleaner(Settings).CleanAsync(context, cancellationToken);
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
