namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     配送异常长期联调数据生成结果，区分必要的异常配送任务与异常事实新增、复用数量。
/// </summary>
internal sealed record DemoDataDeliveryExceptionGenerationResult(
    int CreatedTasks,
    int ReusedTasks,
    int CreatedExceptions,
    int ReusedExceptions);
