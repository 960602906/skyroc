using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     将同一数据质量结果输出为 JSON 和便于人工复核的 Markdown。
/// </summary>
public static class DataQualityReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    ///     原子写入本轮报告目录，并返回两个报告的完整路径。
    /// </summary>
    public static async Task<DataQualityReportPaths> WriteAsync(
        DataQualityReport report,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("A report output directory is required.", nameof(outputDirectory));

        Directory.CreateDirectory(outputDirectory);
        var safeRunId = string.Concat(report.RunId.Where(character => char.IsLetterOrDigit(character) || character == '-'));
        var jsonPath = Path.GetFullPath(Path.Combine(outputDirectory, $"{safeRunId}-quality.json"));
        var markdownPath = Path.GetFullPath(Path.Combine(outputDirectory, $"{safeRunId}-quality.md"));

        await File.WriteAllTextAsync(
            jsonPath,
            JsonSerializer.Serialize(report, JsonOptions),
            Encoding.UTF8,
            cancellationToken);
        await File.WriteAllTextAsync(
            markdownPath,
            BuildMarkdown(report),
            Encoding.UTF8,
            cancellationToken);

        return new DataQualityReportPaths(jsonPath, markdownPath);
    }

    private static string BuildMarkdown(DataQualityReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# SkyRoc 自动业务测试数据质量报告");
        builder.AppendLine();
        builder.AppendLine($"- 批次：`{report.RunId}`");
        builder.AppendLine($"- 数据库：`{report.DatabaseName}`");
        builder.AppendLine($"- 生成时间（UTC）：`{report.GeneratedAtUtc:O}`");
        AppendSimpleTable(builder, "逐表记录数", "表", "记录数", report.TableCounts);
        AppendSimpleTable(
            builder,
            "字段填充率",
            "字段",
            "填充率",
            report.FieldFillRates.ToDictionary(
                pair => pair.Key,
                pair => $"{pair.Value.ToString("0.####", CultureInfo.InvariantCulture)}%"));

        builder.AppendLine("## 状态分布");
        builder.AppendLine();
        if (report.StatusDistributions.Count == 0)
            builder.AppendLine("- 无状态字段记录。");
        foreach (var distribution in report.StatusDistributions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{distribution.Key}`：{string.Join(", ", distribution.Value.Select(pair => $"{pair.Key}={pair.Value}"))}");
        }

        AppendFindingList(builder, "孤儿外键", report.OrphanForeignKeys);
        AppendFindingList(builder, "重复业务编码", report.DuplicateBusinessCodes);
        AppendFindingList(builder, "临时数据残留", report.TemporaryResidues);
        AppendMetadataInventory(builder, report.MetadataInventory, report.MetadataFindings);
        AppendFindingList(builder, "质量规则例外", report.QualityRuleExceptions);
        AppendDemoDataAcceptance(builder, report.DemoDataAcceptance);

        builder.AppendLine("## 业务一致性");
        builder.AppendLine();
        foreach (var check in report.BusinessConsistencyChecks.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            builder.AppendLine($"- {(check.Value ? "通过" : "失败")} `{check.Key}`");

        return builder.ToString();
    }

    private static void AppendSimpleTable<TValue>(
        StringBuilder builder,
        string heading,
        string keyHeading,
        string valueHeading,
        IReadOnlyDictionary<string, TValue> values)
    {
        builder.AppendLine();
        builder.AppendLine($"## {heading}");
        builder.AppendLine();
        builder.AppendLine($"| {keyHeading} | {valueHeading} |");
        builder.AppendLine("| --- | ---: |");
        foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            builder.AppendLine($"| `{pair.Key}` | {pair.Value} |");
    }

    private static void AppendFindingList(StringBuilder builder, string heading, IReadOnlyList<string> findings)
    {
        builder.AppendLine();
        builder.AppendLine($"## {heading}");
        builder.AppendLine();
        if (findings.Count == 0)
        {
            builder.AppendLine("- 未发现。");
            return;
        }

        foreach (var finding in findings)
            builder.AppendLine($"- {finding}");
    }

    private static void AppendDemoDataAcceptance(
        StringBuilder builder,
        DemoDataQualityAcceptanceResult acceptance)
    {
        builder.AppendLine();
        builder.AppendLine("## 长期联调数据验收");
        builder.AppendLine();
        builder.AppendLine($"- 结果：{(acceptance.IsReady ? "通过" : "未通过")}");
        if (acceptance.Findings.Count == 0)
            return;

        foreach (var finding in acceptance.Findings)
            builder.AppendLine($"- {finding}");
    }

    private static void AppendMetadataInventory(
        StringBuilder builder,
        IReadOnlyList<MetadataTableInventory> tables,
        IReadOnlyList<string> findings)
    {
        builder.AppendLine();
        builder.AppendLine("## 元数据盘点");
        builder.AppendLine();
        builder.AppendLine("| 表 | 分类 | 列数 | 外键 | 唯一约束 | 说明 |");
        builder.AppendLine("| --- | --- | ---: | ---: | ---: | --- |");
        foreach (var table in tables.OrderBy(item => item.TableName, StringComparer.Ordinal))
        {
            var rule = table.Rule;
            builder.AppendLine($"| `{table.TableName}` | {rule?.Category} | {table.Columns.Count} | {table.ForeignKeyNames.Count} | {table.UniqueConstraintNames.Count} | {rule?.Rationale ?? "未配置"} |");
        }

        AppendFindingList(builder, "元数据盘点问题", findings);
    }
}
