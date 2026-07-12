using System.Text.Json;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     验证基础数据质量报告同时生成机器可读与 Markdown 版本并包含全部门禁栏目。
/// </summary>
public class DataQualityReportWriterTests
{
    /// <summary>
    ///     报告写入器应创建目录、JSON 和 Markdown，并保留所有质量栏目。
    /// </summary>
    [Fact]
    public async Task WriteAsync_CreatesMachineReadableAndMarkdownReports()
    {
        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            "skyroc-quality-report-tests",
            Guid.NewGuid().ToString("N"));
        var report = DataQualityReport.CreateInfrastructureReport(
            "SKYROC-AUTOTEST-20260712-ABCDEF12",
            "skyroc_business_test",
            new Dictionary<string, long> { ["company"] = 20 },
            new Dictionary<string, decimal> { ["company.name"] = 100m },
            new Dictionary<string, IReadOnlyDictionary<string, long>>
            {
                ["company.status"] = new Dictionary<string, long> { ["1"] = 20 }
            },
            [],
            [],
            [],
            new Dictionary<string, bool>
            {
                ["migrationHistoryMatchesModel"] = true,
                ["temporaryBatchResidueIsZero"] = true
            });

        try
        {
            var paths = await DataQualityReportWriter.WriteAsync(report, outputDirectory);

            Assert.True(File.Exists(paths.JsonPath));
            Assert.True(File.Exists(paths.MarkdownPath));
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(paths.JsonPath));
            var root = document.RootElement;
            Assert.True(root.TryGetProperty("tableCounts", out _));
            Assert.True(root.TryGetProperty("fieldFillRates", out _));
            Assert.True(root.TryGetProperty("statusDistributions", out _));
            Assert.True(root.TryGetProperty("orphanForeignKeys", out _));
            Assert.True(root.TryGetProperty("duplicateBusinessCodes", out _));
            Assert.True(root.TryGetProperty("temporaryResidues", out _));
            Assert.True(root.TryGetProperty("businessConsistencyChecks", out _));
            Assert.True(root.TryGetProperty("metadataInventory", out _));
            Assert.True(root.TryGetProperty("metadataFindings", out _));
            Assert.True(root.TryGetProperty("qualityRuleExceptions", out _));
            Assert.True(root.TryGetProperty("demoDataAcceptance", out _));

            var markdown = await File.ReadAllTextAsync(paths.MarkdownPath);
            Assert.Contains("逐表记录数", markdown, StringComparison.Ordinal);
            Assert.Contains("字段填充率", markdown, StringComparison.Ordinal);
            Assert.Contains("状态分布", markdown, StringComparison.Ordinal);
            Assert.Contains("孤儿外键", markdown, StringComparison.Ordinal);
            Assert.Contains("重复业务编码", markdown, StringComparison.Ordinal);
            Assert.Contains("临时数据残留", markdown, StringComparison.Ordinal);
            Assert.Contains("元数据盘点", markdown, StringComparison.Ordinal);
            Assert.Contains("元数据盘点问题", markdown, StringComparison.Ordinal);
            Assert.Contains("质量规则例外", markdown, StringComparison.Ordinal);
            Assert.Contains("长期联调数据验收", markdown, StringComparison.Ordinal);
            Assert.Contains("业务一致性", markdown, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
        }
    }
}
