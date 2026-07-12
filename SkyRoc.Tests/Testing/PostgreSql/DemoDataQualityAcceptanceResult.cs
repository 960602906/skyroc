namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     长期联调数据验收的机器可读结果。
/// </summary>
public sealed record DemoDataQualityAcceptanceResult(bool IsReady, IReadOnlyList<string> Findings);
