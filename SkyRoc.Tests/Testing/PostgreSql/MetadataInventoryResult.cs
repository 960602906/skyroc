namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     T1 元数据盘点的可序列化表级结果。
/// </summary>
public sealed record MetadataInventoryResult(
    IReadOnlyList<MetadataTableInventory> Tables,
    IReadOnlyList<string> Findings,
    IReadOnlyDictionary<string, bool> Checks);
