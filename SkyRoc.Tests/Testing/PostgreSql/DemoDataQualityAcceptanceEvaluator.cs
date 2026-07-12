using System.Globalization;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     将数据质量报告转换为长期联调数据是否可交付的明确验收结论。
/// </summary>
public static class DemoDataQualityAcceptanceEvaluator
{
    /// <summary>
    ///     检查数量下限、始终适用字段、关系完整性、临时残留和业务一致性门禁。
    /// </summary>
    public static DemoDataQualityAcceptanceResult Evaluate(DataQualityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var findings = new List<string>();
        foreach (var table in report.MetadataInventory.OrderBy(item => item.TableName, StringComparer.Ordinal))
        {
            if (table.Rule is { MinimumRows: > 0 } rule)
            {
                var actualCount = report.TableCounts.GetValueOrDefault(table.TableName);
                if (actualCount < rule.MinimumRows)
                    findings.Add($"{table.TableName}：记录数 {actualCount}，小于验收下限 {rule.MinimumRows}");
            }

            foreach (var column in table.Columns.Where(column =>
                         column.Applicability == DataQualityFieldApplicability.AlwaysRequired))
            {
                var fieldName = $"{table.TableName}.{column.ColumnName}";
                var fillRate = report.FieldFillRates.GetValueOrDefault(fieldName);
                if (fillRate < 100m)
                {
                    findings.Add(
                        $"{fieldName}：适用字段填充率 {fillRate.ToString("0.####", CultureInfo.InvariantCulture)}%，应为 100%");
                }
            }
        }

        findings.AddRange(report.OrphanForeignKeys.Select(item => $"存在孤儿外键：{item}"));
        findings.AddRange(report.DuplicateBusinessCodes.Select(item => $"存在重复业务编码：{item}"));
        findings.AddRange(report.TemporaryResidues.Select(item => $"存在自动测试临时数据残留：{item}"));
        findings.AddRange(report.BusinessConsistencyChecks
            .Where(check => !check.Value)
            .OrderBy(check => check.Key, StringComparer.Ordinal)
            .Select(check => $"业务一致性检查失败：{check.Key}"));

        return new DemoDataQualityAcceptanceResult(findings.Count == 0, findings);
    }
}
