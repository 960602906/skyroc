using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     验证 T1 元数据盘点将 EF Core 模型、PostgreSQL 目录和统一质量规则作为同一验收门禁。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class DatabaseMetadataInventoryTests(PostgreSqlTestFixture fixture)
{
    /// <summary>
    ///     专用 PostgreSQL 库的业务表、列、约束、注释和字段适用性规则必须全部通过双向盘点。
    /// </summary>
    [Fact]
    public async Task GenerateQualityReportAsync_ValidatesEfModelCatalogAndQualityRules()
    {
        var result = await fixture.GenerateQualityReportAsync(TestBatchContext.Create().Id);

        Assert.True(result.Report.BusinessConsistencyChecks["efModelMatchesPostgreSqlCatalog"]);
        Assert.True(result.Report.BusinessConsistencyChecks["allBusinessTablesHaveQualityRules"]);
        Assert.True(result.Report.BusinessConsistencyChecks["allPersistedColumnsHaveApplicabilityRules"]);
        Assert.True(result.Report.BusinessConsistencyChecks["databaseCommentsMatchModel"]);
        Assert.True(result.Report.BusinessConsistencyChecks["foreignKeysMatchModel"]);
        Assert.True(result.Report.BusinessConsistencyChecks["uniqueConstraintsMatchModel"]);
        Assert.Empty(result.Report.DuplicateBusinessCodes);
    }
}
