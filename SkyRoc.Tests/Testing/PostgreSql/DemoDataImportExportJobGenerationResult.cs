namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     描述长期联调导入导出任务的新增与复用数量。
/// </summary>
internal sealed record DemoDataImportExportJobGenerationResult(
    int CreatedJobs,
    int ReusedJobs);
