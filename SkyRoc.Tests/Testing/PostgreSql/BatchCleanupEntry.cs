namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     一条按实体主键和批次归属字段双重限定的临时数据清理登记。
/// </summary>
public sealed record BatchCleanupEntry(
    Type EntityType,
    Guid EntityId,
    string OwnershipPropertyName,
    string OwnershipValue);
