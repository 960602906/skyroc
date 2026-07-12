namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     一次质量报告写入产生的机器可读与 Markdown 文件路径。
/// </summary>
public sealed record DataQualityReportPaths(string JsonPath, string MarkdownPath);
