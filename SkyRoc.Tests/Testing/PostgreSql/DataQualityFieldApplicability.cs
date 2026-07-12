namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     字段在联调数据中必须填写的业务条件。
/// </summary>
public enum DataQualityFieldApplicability
{
    /// <summary>尚未定义，属于阻断性配置错误。</summary>
    NotConfigured,
    /// <summary>所有记录均必须填写。</summary>
    AlwaysRequired,
    /// <summary>由当前业务场景决定是否适用。</summary>
    BusinessConditional,
    /// <summary>仅在对应状态或操作发生后适用。</summary>
    StateConditional
}
