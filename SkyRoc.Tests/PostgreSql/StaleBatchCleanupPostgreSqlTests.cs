using Microsoft.EntityFrameworkCore;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     T14 切片：受控清理历史 <c>SKYROC-AUTOTEST-</c> 残留后恢复最终质量门禁。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class StaleBatchCleanupPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    /// <summary>
    ///     按外键逆序删除历史临时前缀行，再核对最终质量报告全绿。
    /// </summary>
    [Fact]
    public async Task StaleBatchCleanup_RemovesHistoricalResidueAndRestoresQualityGate_OnPostgreSql()
    {
        await fixture.CleanupStaleAutotestBatchesAsync();

        var batch = TestBatchContext.Create();
        var result = await fixture.GenerateQualityReportAsync(batch.Id);
        var report = result.Report;

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

        Assert.True(report.BusinessConsistencyChecks["temporaryBatchResidueIsZero"]);
        Assert.True(report.BusinessConsistencyChecks["stockBatchQuantitiesAreNonNegative"]);
        Assert.True(report.BusinessConsistencyChecks["efModelMatchesPostgreSqlCatalog"]);

        var nonEmptyTables = report.TableCounts.Count(pair => pair.Value > 0);
        Assert.True(nonEmptyTables >= 80, $"非空业务表数量不足：实际 {nonEmptyTables}");

        await using var context = fixture.CreateDbContext();
        Assert.False(
            await context.LoginLogs.AsNoTracking()
                .AnyAsync(item => item.IpAddress == null || item.IpAddress.Trim() == string.Empty),
            "sys_login_log 仍存在空 IP，质量填充率会失败");
    }
}
