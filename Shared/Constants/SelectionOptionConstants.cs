namespace Shared.Constants;

/// <summary>
///     下拉选择项查询的统一数量和文本边界。
/// </summary>
public static class SelectionOptionConstants
{
    /// <summary>
    ///     远程搜索默认返回数量。
    /// </summary>
    public const int DefaultSearchLimit = 20;

    /// <summary>
    ///     单次远程搜索允许返回的最大数量。
    /// </summary>
    public const int MaxSearchLimit = 50;

    /// <summary>
    ///     单次允许解析的已选主键数量。
    /// </summary>
    public const int MaxResolveCount = 100;

    /// <summary>
    ///     有界选项允许返回的最大数量；超过时拒绝静默截断。
    /// </summary>
    public const int MaxBoundedCount = 500;

    /// <summary>
    ///     搜索关键词允许的最大长度。
    /// </summary>
    public const int MaxKeywordLength = 100;
}
