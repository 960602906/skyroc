namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     汇总受管外部报送日志的新增与复用数量。
/// </summary>
internal sealed record DemoDataExternalPushLogGenerationResult(
    int CreatedLogs,
    int ReusedLogs);
