namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     一列持久化字段的模型约束与数据适用性。
/// </summary>
public sealed record MetadataColumnInventory(
    string ColumnName,
    bool IsNullable,
    int? Precision,
    int? Scale,
    string? DefaultValueSql,
    string? Comment,
    DataQualityFieldApplicability Applicability);
