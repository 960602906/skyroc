using System.Text.Json;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     防止 PostgreSQL 自动业务测试入口丢失白名单校验或引入破坏性数据库命令。
/// </summary>
public class PostgreSqlInfrastructureDocumentationTests
{
    /// <summary>
    ///     一键入口必须先校验 Testing 环境和精确库名，再执行迁移与真实数据库专项测试。
    /// </summary>
    [Fact]
    public void RunnerScript_ValidatesTargetBeforeMigrationAndContainsNoDestructiveCommands()
    {
        var script = File.ReadAllText(GetRepositoryFile("scripts", "Invoke-PostgreSqlBusinessTests.ps1"));
        var validationPosition = script.IndexOf("expectedDatabaseName", StringComparison.Ordinal);
        var migrationPosition = script.IndexOf("dotnet ef database update", StringComparison.Ordinal);

        Assert.True(validationPosition >= 0);
        Assert.True(migrationPosition > validationPosition);
        Assert.Contains("Testing", script, StringComparison.Ordinal);
        Assert.Contains("PostgreSqlInfrastructureTests", script, StringComparison.Ordinal);
        Assert.Contains("DatabaseMetadataInventoryTests", script, StringComparison.Ordinal);
        Assert.DoesNotContain("TRUNCATE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnsureDeleted", script, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     T14 一键验收必须先白名单校验，再迁移、has-pending-model-changes、FullAcceptance 与质量门禁；
    ///     完整 SkyRoc.Tests 仅能通过显式开关触发，且不得包含破坏性数据库命令。
    /// </summary>
    [Fact]
    public void FullAcceptanceScript_GatesPendingModelChangesBeforeSuiteAndContainsNoDestructiveCommands()
    {
        var script = File.ReadAllText(GetRepositoryFile("scripts", "Invoke-SkyRocFullAcceptance.ps1"));
        var validationPosition = script.IndexOf("expectedDatabaseName", StringComparison.Ordinal);
        var migrationPosition = script.IndexOf("dotnet ef database update", StringComparison.Ordinal);
        var pendingModelPosition = script.IndexOf(
            "dotnet ef migrations has-pending-model-changes",
            StringComparison.Ordinal);
        var dotnetTestPosition = script.IndexOf("dotnet test", StringComparison.Ordinal);
        var fullSuiteSwitchPosition = script.IndexOf("IncludeFullTestSuite", StringComparison.Ordinal);

        Assert.True(validationPosition >= 0);
        Assert.True(migrationPosition > validationPosition);
        Assert.True(pendingModelPosition > migrationPosition);
        Assert.True(dotnetTestPosition > pendingModelPosition);
        Assert.True(fullSuiteSwitchPosition >= 0);
        Assert.Contains("Testing", script, StringComparison.Ordinal);
        Assert.Contains("FullAcceptancePostgreSqlTests", script, StringComparison.Ordinal);
        Assert.Contains("DatabaseMetadataInventoryTests", script, StringComparison.Ordinal);
        Assert.Contains("dotnet format", script, StringComparison.Ordinal);
        Assert.DoesNotContain("TRUNCATE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnsureDeleted", script, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     非敏感测试约定必须固定 Testing 环境、用户确认的 skyroc 白名单和报告目录。
    /// </summary>
    [Fact]
    public void TestSettings_DeclareOwnerConfirmedDatabaseAllowlist()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            GetRepositoryFile("SkyRoc.Tests", "postgresql-testsettings.json")));
        var root = document.RootElement;

        Assert.Equal("Testing", root.GetProperty("environmentName").GetString());
        Assert.Equal("skyroc", root.GetProperty("expectedDatabaseName").GetString());
        Assert.Equal("artifacts/business-test-reports", root.GetProperty("reportDirectory").GetString());
    }

    /// <summary>
    ///     PostgreSQL 共享夹具只能应用迁移；不得删除或重建长期联调库，也不得在每个测试集合初始化时隐式生成全量数据。
    /// </summary>
    [Fact]
    public void PostgreSqlFixture_OnlyMigratesAndDoesNotDeleteOrImplicitlyGenerateDemoData()
    {
        var fixtureSource = File.ReadAllText(GetRepositoryFile(
            "SkyRoc.Tests",
            "Testing",
            "PostgreSql",
            "PostgreSqlTestFixture.cs"));

        Assert.Contains("await context.Database.MigrateAsync();", fixtureSource, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureDeleted", fixtureSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnsureCreated", fixtureSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("await GenerateDemoDataAsync();", fixtureSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     历史临时批次清理器只能删除带批次前缀及其外键依赖的数据，不得全表修复非受管登录日志。
    /// </summary>
    [Fact]
    public void StaleBatchCleaner_DoesNotUpdateNonManagedLoginLogs()
    {
        var cleanerSource = File.ReadAllText(GetRepositoryFile(
            "SkyRoc.Tests",
            "Testing",
            "PostgreSql",
            "PostgreSqlStaleBatchCleaner.cs"));

        Assert.DoesNotContain("UPDATE \"sys_login_log\"", cleanerSource, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     中文说明必须明确长期数据保护、批次清理、报告和运行命令。
    /// </summary>
    [Fact]
    public void ChineseGuide_DocumentsSafetyAndExecutionContract()
    {
        var guide = File.ReadAllText(GetRepositoryFile("docs", "testing", "PostgreSQL自动业务测试.md"));

        Assert.Contains("长期联调数据", guide, StringComparison.Ordinal);
        Assert.Contains("SKYROC-AUTOTEST-", guide, StringComparison.Ordinal);
        Assert.Contains("事务", guide, StringComparison.Ordinal);
        Assert.Contains("逆序", guide, StringComparison.Ordinal);
        Assert.Contains("Invoke-PostgreSqlBusinessTests.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("Invoke-SkyRocFullAcceptance.ps1", guide, StringComparison.Ordinal);
        Assert.Contains("has-pending-model-changes", guide, StringComparison.Ordinal);
        Assert.Contains("FullAcceptancePostgreSqlTests", guide, StringComparison.Ordinal);
        Assert.Contains("JSON", guide, StringComparison.Ordinal);
        Assert.Contains("Markdown", guide, StringComparison.Ordinal);
        Assert.Contains("前端联调数据说明.md", guide, StringComparison.Ordinal);
    }

    /// <summary>
    ///     联调手册不得写入密码或连接串，但必须说明稳定键前缀、账号来源与安全边界。
    /// </summary>
    [Fact]
    public void FrontendIntegrationGuide_DocumentsManagedKeysWithoutSecrets()
    {
        var guide = File.ReadAllText(GetRepositoryFile("docs", "testing", "前端联调数据说明.md"));

        Assert.Contains("SKYROC-DEMO-", guide, StringComparison.Ordinal);
        Assert.Contains("SYSTEM-USER", guide, StringComparison.Ordinal);
        Assert.Contains("幂等", guide, StringComparison.Ordinal);
        Assert.Contains("skyroc", guide, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", guide, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", guide, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AdminPassword", guide, StringComparison.Ordinal);
        Assert.DoesNotContain("SkyRocSystem", guide, StringComparison.Ordinal);
    }

    /// <summary>
    ///     联调手册必须为主正向、逆向和运营权限场景给出完整稳定键及单据定位字段，
    ///     使前端能够从受管数据精确进入已验收业务链路而不依赖模糊名称或临时批次。
    /// </summary>
    [Fact]
    public void FrontendIntegrationGuide_IndexesMainBusinessFlowsByStableKeys()
    {
        var guide = File.ReadAllText(GetRepositoryFile("docs", "testing", "前端联调数据说明.md"));

        Assert.Contains("销售订单 `inner_remark`", guide, StringComparison.Ordinal);
        Assert.Contains("SKYROC-DEMO-PURCHASE-PLAN-001", guide, StringComparison.Ordinal);
        Assert.Contains("SKYROC-DEMO-PURCHASE-STOCK-IN-001", guide, StringComparison.Ordinal);
        Assert.Contains("SKYROC-DEMO-DELIVERY-TASK-001", guide, StringComparison.Ordinal);
        Assert.Contains("SKYROC-DEMO-SALES-RETURN-STOCK-IN-001", guide, StringComparison.Ordinal);
        Assert.Contains("SKYROC-DEMO-SYSTEM-ROLE-001", guide, StringComparison.Ordinal);
        Assert.Contains("禁止以 `SKYROC-DEMO-` 前缀模糊查询后批量写入或删除", guide, StringComparison.Ordinal);
    }

    /// <summary>
    ///     开发进度文档必须固定当前断点、测试数量基线、数据库基线和最近验收日期，
    ///     以便 T14 收口时不再把测试事实回写到实现进度中。
    /// </summary>
    [Fact]
    public void ProgressBaseline_DocumentsAcceptanceCountsAndDatabaseBaseline()
    {
        var progress = File.ReadAllText(GetRepositoryFile("docs", "开发进度.md"));

        Assert.Contains("## 验收与测试基线", progress, StringComparison.Ordinal);
        Assert.Contains("最近验收日期 | 2026-07-23", progress, StringComparison.Ordinal);
        Assert.Contains("完整测试数量基线 | 558 项（2026-07-22 完整套件已完成运行", progress, StringComparison.Ordinal);
        Assert.Contains("数据库基线 | 专用库 `skyroc`", progress, StringComparison.Ordinal);
        Assert.Contains("T14 勾选状态 | **已勾选**", progress, StringComparison.Ordinal);
        Assert.Contains("ProgressBaseline_DocumentsAcceptanceCountsAndDatabaseBaseline", progress, StringComparison.Ordinal);
    }

    private static string GetRepositoryFile(params string[] pathSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SkyRoc.sln")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        return Path.Combine([directory.FullName, .. pathSegments]);
    }
}
