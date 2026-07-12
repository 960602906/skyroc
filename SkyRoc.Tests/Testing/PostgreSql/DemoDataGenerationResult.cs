namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     描述一次长期联调数据生成中各层新增和复用的受管记录数量。
/// </summary>
public sealed record DemoDataGenerationResult(
    IReadOnlyDictionary<string, int> CreatedByLayer,
    IReadOnlyDictionary<string, int> ReusedByLayer);
