namespace SkyRoc.Authorization;

/// <summary>
///     声明基础资料控制器使用的权限资源前缀。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class PermissionResourceAttribute : Attribute
{
    /// <summary>
    ///     创建权限资源声明。
    /// </summary>
    public PermissionResourceAttribute(string resource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        Resource = resource;
    }

    /// <summary>
    ///     module:resource 格式的权限资源前缀。
    /// </summary>
    public string Resource { get; }
}
