using Xunit;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     串行共享专用 PostgreSQL 测试夹具，避免临时批次互相干扰。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlTestCollection : ICollectionFixture<PostgreSqlTestFixture>
{
    /// <summary>
    ///     xUnit 集合名称。
    /// </summary>
    public const string Name = "PostgreSQL business tests";
}
