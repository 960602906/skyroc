using Npgsql;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     在建立连接或执行清理前验证环境和数据库精确白名单。
/// </summary>
public static class DatabaseSafetyGuard
{
    /// <summary>
    ///     验证测试配置并返回连接串中的实际数据库名。
    /// </summary>
    public static string Validate(PostgreSqlTestSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!string.Equals(settings.EnvironmentName, "Testing", StringComparison.Ordinal))
            throw new InvalidOperationException("PostgreSQL business tests require the exact Testing environment.");

        if (string.IsNullOrWhiteSpace(settings.ConnectionString)
            || settings.ConnectionString.Contains("__SET_IN_ENV__", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The PostgreSQL test connection string is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.ExpectedDatabaseName))
            throw new InvalidOperationException("The PostgreSQL test database allowlist is empty.");

        NpgsqlConnectionStringBuilder connectionBuilder;
        try
        {
            connectionBuilder = new NpgsqlConnectionStringBuilder(settings.ConnectionString);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("The PostgreSQL test connection string is invalid.", exception);
        }

        var actualDatabaseName = connectionBuilder.Database;
        if (string.IsNullOrWhiteSpace(actualDatabaseName))
            throw new InvalidOperationException("The PostgreSQL test connection string does not specify a database.");

        if (!string.Equals(actualDatabaseName, settings.ExpectedDatabaseName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Database '{actualDatabaseName}' is outside the exact test allowlist '{settings.ExpectedDatabaseName}'.");
        }

        return actualDatabaseName;
    }
}
