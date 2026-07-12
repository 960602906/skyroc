namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     一次质量扫描的结构化结果与落盘文件路径。
/// </summary>
public sealed record DataQualityReportResult(DataQualityReport Report, DataQualityReportPaths Paths);
