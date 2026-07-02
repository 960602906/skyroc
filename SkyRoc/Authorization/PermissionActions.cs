namespace SkyRoc.Authorization;

/// <summary>
///     基础资料通用操作名称。
/// </summary>
public static class PermissionActions
{
    /// <summary>读取。</summary>
    public const string Read = "read";

    /// <summary>创建。</summary>
    public const string Create = "create";

    /// <summary>更新。</summary>
    public const string Update = "update";

    /// <summary>删除。</summary>
    public const string Delete = "delete";

    /// <summary>全部受支持的通用操作。</summary>
    public static IReadOnlyCollection<string> Defined { get; } =
    [
        Read,
        Create,
        Update,
        Delete
    ];
}
