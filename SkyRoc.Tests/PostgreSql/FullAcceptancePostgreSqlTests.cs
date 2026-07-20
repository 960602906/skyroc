using Microsoft.EntityFrameworkCore;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     T14 全量验收切片：核对专用库最终质量报告、联调受管键可用性与无临时残留。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class FullAcceptancePostgreSqlTests(PostgreSqlTestFixture fixture)
{
    /// <summary>
    ///     最终质量报告必须指向白名单库 skyroc、联调验收就绪、元数据/一致性门禁全绿，
    ///     且关键联调稳定键（仓库/商品/系统用户）可精确查询；本切片只读，不写入临时业务单据。
    /// </summary>
    [Fact]
    public async Task FullAcceptance_FinalQualityReportManagedDemoReadyAndNoResidue_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var result = await fixture.GenerateQualityReportAsync(batch.Id);
        var report = result.Report;

        Assert.Equal(fixture.DatabaseName, report.DatabaseName);
        Assert.Equal("skyroc", report.DatabaseName);
        Assert.True(File.Exists(result.Paths.JsonPath));
        Assert.True(File.Exists(result.Paths.MarkdownPath));

        Assert.True(
            report.DemoDataAcceptance.IsReady,
            string.Join("; ", report.DemoDataAcceptance.Findings));
        Assert.Empty(report.OrphanForeignKeys);
        Assert.Empty(report.DuplicateBusinessCodes);
        Assert.Empty(report.TemporaryResidues);
        Assert.Empty(report.MetadataFindings);

        Assert.True(report.BusinessConsistencyChecks["efModelMatchesPostgreSqlCatalog"]);
        Assert.True(report.BusinessConsistencyChecks["allBusinessTablesHaveQualityRules"]);
        Assert.True(report.BusinessConsistencyChecks["allPersistedColumnsHaveApplicabilityRules"]);
        Assert.True(report.BusinessConsistencyChecks["databaseCommentsMatchModel"]);
        Assert.True(report.BusinessConsistencyChecks["foreignKeysMatchModel"]);
        Assert.True(report.BusinessConsistencyChecks["uniqueConstraintsMatchModel"]);
        Assert.True(report.BusinessConsistencyChecks["migrationHistoryMatchesModel"]);
        Assert.True(report.BusinessConsistencyChecks["temporaryBatchResidueIsZero"]);
        Assert.True(report.BusinessConsistencyChecks["stockBatchQuantitiesAreNonNegative"]);

        var nonEmptyTables = report.TableCounts.Count(pair => pair.Value > 0);
        Assert.True(nonEmptyTables >= 80, $"非空业务表数量不足：实际 {nonEmptyTables}");

        var managedWareCode = DemoDataStableKeyCatalog.Create("WARE", 1);
        var managedGoodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
        var managedSystemUsername = DemoDataStableKeyCatalog.Create("SYSTEM-USER", 1);
        var managedCustomerCode = DemoDataStableKeyCatalog.Create("CUSTOMER", 1);
        var managedSupplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);

        await using var context = fixture.CreateDbContext();
        Assert.True(
            await context.Wares.AsNoTracking().AnyAsync(item => item.Code == managedWareCode),
            $"缺少受管仓库 {managedWareCode}，联调数据未就绪");
        Assert.True(
            await context.Goods.AsNoTracking().AnyAsync(item => item.Code == managedGoodsCode),
            $"缺少受管商品 {managedGoodsCode}，联调数据未就绪");
        Assert.True(
            await context.Users.AsNoTracking().AnyAsync(item => item.Username == managedSystemUsername),
            $"缺少受管系统用户 {managedSystemUsername}，联调账号未就绪");
        Assert.True(
            await context.Customers.AsNoTracking().AnyAsync(item => item.Code == managedCustomerCode),
            $"缺少受管客户 {managedCustomerCode}，联调数据未就绪");
        Assert.True(
            await context.Suppliers.AsNoTracking().AnyAsync(item => item.Code == managedSupplierCode),
            $"缺少受管供应商 {managedSupplierCode}，联调数据未就绪");
    }
}
