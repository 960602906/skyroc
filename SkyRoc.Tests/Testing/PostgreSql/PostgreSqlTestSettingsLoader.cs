using System.Text.Json;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     从受版本控制的非敏感约定和项目连接配置加载 PostgreSQL 测试设置。
/// </summary>
public static class PostgreSqlTestSettingsLoader
{
    /// <summary>
    ///     加载测试环境、精确数据库白名单、报告目录和连接串。
    /// </summary>
    public static PostgreSqlTestSettings Load()
    {
        var repositoryRoot = FindRepositoryRoot();
        using var testSettingsDocument = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(repositoryRoot, "SkyRoc.Tests", "postgresql-testsettings.json")));
        var root = testSettingsDocument.RootElement;

        var connectionString = Environment.GetEnvironmentVariable("SKYROC_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            using var applicationSettingsDocument = JsonDocument.Parse(File.ReadAllText(
                Path.Combine(repositoryRoot, "SkyRoc", "appsettings.json")));
            connectionString = applicationSettingsDocument.RootElement
                .GetProperty("ConnectionStrings")
                .GetProperty("DefaultConnection")
                .GetString();
        }

        var environmentName = Environment.GetEnvironmentVariable("SKYROC_TEST_ENVIRONMENT")
                              ?? root.GetProperty("environmentName").GetString()
                              ?? string.Empty;
        var expectedDatabaseName = root.GetProperty("expectedDatabaseName").GetString() ?? string.Empty;
        var reportDirectory = Path.GetFullPath(Path.Combine(
            repositoryRoot,
            root.GetProperty("reportDirectory").GetString() ?? string.Empty));

        return new PostgreSqlTestSettings(
            environmentName,
            connectionString ?? string.Empty,
            expectedDatabaseName,
            reportDirectory);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SkyRoc.sln")))
            directory = directory.Parent;

        return directory?.FullName
               ?? throw new InvalidOperationException("Unable to locate the SkyRoc repository root.");
    }
}
