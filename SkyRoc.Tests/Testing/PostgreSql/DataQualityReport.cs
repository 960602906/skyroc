namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     自动业务测试的基础数据质量报告模型。
/// </summary>
public sealed class DataQualityReport
{
    private DataQualityReport()
    {
    }

    /// <summary>
    ///     本轮测试批次。
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    ///     报告生成 UTC 时间。
    /// </summary>
    public required DateTime GeneratedAtUtc { get; init; }

    /// <summary>
    ///     已通过白名单验证的数据库名。
    /// </summary>
    public required string DatabaseName { get; init; }

    /// <summary>
    ///     每张业务表的记录数。
    /// </summary>
    public required IReadOnlyDictionary<string, long> TableCounts { get; init; }

    /// <summary>
    ///     按“表.列”记录的非空且非空白填充率百分比。
    /// </summary>
    public required IReadOnlyDictionary<string, decimal> FieldFillRates { get; init; }

    /// <summary>
    ///     按“表.状态列”记录的状态值数量分布。
    /// </summary>
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, long>> StatusDistributions { get; init; }

    /// <summary>
    ///     检出的孤儿外键描述。
    /// </summary>
    public required IReadOnlyList<string> OrphanForeignKeys { get; init; }

    /// <summary>
    ///     检出的重复业务编码描述。
    /// </summary>
    public required IReadOnlyList<string> DuplicateBusinessCodes { get; init; }

    /// <summary>
    ///     检出的自动测试临时数据残留描述。
    /// </summary>
    public required IReadOnlyList<string> TemporaryResidues { get; init; }

    /// <summary>
    ///     迁移、回滚、清理和基础业务不变量检查结果。
    /// </summary>
    public required IReadOnlyDictionary<string, bool> BusinessConsistencyChecks { get; init; }

    /// <summary>
    ///     创建 T0 基础设施验收报告。
    /// </summary>
    public static DataQualityReport CreateInfrastructureReport(
        string runId,
        string databaseName,
        IReadOnlyDictionary<string, long> tableCounts,
        IReadOnlyDictionary<string, decimal> fieldFillRates,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, long>> statusDistributions,
        IReadOnlyList<string> orphanForeignKeys,
        IReadOnlyList<string> duplicateBusinessCodes,
        IReadOnlyList<string> temporaryResidues,
        IReadOnlyDictionary<string, bool> businessConsistencyChecks)
    {
        return new DataQualityReport
        {
            RunId = runId,
            GeneratedAtUtc = DateTime.UtcNow,
            DatabaseName = databaseName,
            TableCounts = tableCounts,
            FieldFillRates = fieldFillRates,
            StatusDistributions = statusDistributions,
            OrphanForeignKeys = orphanForeignKeys,
            DuplicateBusinessCodes = duplicateBusinessCodes,
            TemporaryResidues = temporaryResidues,
            BusinessConsistencyChecks = businessConsistencyChecks
        };
    }
}
