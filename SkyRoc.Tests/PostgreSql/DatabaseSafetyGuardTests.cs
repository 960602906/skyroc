using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     验证真实 PostgreSQL 测试在建立连接前会拒绝不明确或越界的数据库目标。
/// </summary>
public class DatabaseSafetyGuardTests
{
    /// <summary>
    ///     明确的 Testing 环境、SkyRoc 测试库名和完全匹配白名单应通过安全校验。
    /// </summary>
    [Fact]
    public void Validate_ReturnsDatabaseName_WhenTargetIsExplicitlyAllowlisted()
    {
        var settings = CreateSettings(
            "Testing",
            "Host=localhost;Database=skyroc_business_test;Username=runner;Password=secret",
            "skyroc_business_test");

        var databaseName = DatabaseSafetyGuard.Validate(settings);

        Assert.Equal("skyroc_business_test", databaseName);
    }

    /// <summary>
    ///     空连接串必须在网络连接前被拒绝。
    /// </summary>
    [Fact]
    public void Validate_RejectsMissingConnectionString()
    {
        var settings = CreateSettings("Testing", string.Empty, "skyroc_business_test");

        var exception = Assert.Throws<InvalidOperationException>(() => DatabaseSafetyGuard.Validate(settings));

        Assert.Contains("connection", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Production 等非测试环境不得运行数据库测试或清理。
    /// </summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    [InlineData("")]
    public void Validate_RejectsNonTestingEnvironment(string environmentName)
    {
        var settings = CreateSettings(
            environmentName,
            "Host=localhost;Database=skyroc_business_test;Username=runner;Password=secret",
            "skyroc_business_test");

        var exception = Assert.Throws<InvalidOperationException>(() => DatabaseSafetyGuard.Validate(settings));

        Assert.Contains("Testing", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     实际数据库名与白名单不一致时必须 fail-closed。
    /// </summary>
    [Fact]
    public void Validate_RejectsDatabaseOutsideAllowlist()
    {
        var settings = CreateSettings(
            "Testing",
            "Host=localhost;Database=neondb;Username=runner;Password=secret",
            "skyroc_business_test");

        var exception = Assert.Throws<InvalidOperationException>(() => DatabaseSafetyGuard.Validate(settings));

        Assert.Contains("neondb", exception.Message, StringComparison.Ordinal);
        Assert.Contains("skyroc_business_test", exception.Message, StringComparison.Ordinal);
    }

    private static PostgreSqlTestSettings CreateSettings(
        string environmentName,
        string connectionString,
        string expectedDatabaseName)
    {
        return new PostgreSqlTestSettings(
            environmentName,
            connectionString,
            expectedDatabaseName,
            Path.Combine("artifacts", "business-test-reports"));
    }
}
