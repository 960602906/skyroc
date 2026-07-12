namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     一张业务表的列、约束和数量目标分类。
/// </summary>
public sealed record MetadataTableInventory(
    string TableName,
    string? Comment,
    IReadOnlyList<MetadataColumnInventory> Columns,
    IReadOnlyList<string> ForeignKeyNames,
    IReadOnlyList<string> UniqueConstraintNames,
    DataQualityTableRule? Rule);
