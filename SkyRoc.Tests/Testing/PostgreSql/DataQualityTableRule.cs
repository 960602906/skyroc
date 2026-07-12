namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     一张业务表的联调数据数量验收规则。
/// </summary>
public sealed record DataQualityTableRule(
    DataQualityTableCategory Category,
    int MinimumRows,
    int? TargetMaximumRows,
    string Rationale);
