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
        Assert.Contains("JSON", guide, StringComparison.Ordinal);
        Assert.Contains("Markdown", guide, StringComparison.Ordinal);
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
