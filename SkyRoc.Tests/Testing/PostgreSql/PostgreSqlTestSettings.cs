namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     真实 PostgreSQL 业务测试的显式安全配置。
/// </summary>
public sealed record PostgreSqlTestSettings(
    string EnvironmentName,
    string ConnectionString,
    string ExpectedDatabaseName,
    string ReportDirectory);
