using Microsoft.AspNetCore.Authorization;

namespace SkyRoc.Authorization;

/// <summary>
///     要求用户具有控制器资源对应的操作权限。
/// </summary>
public sealed class ResourcePermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     创建资源操作权限要求。
    /// </summary>
    public ResourcePermissionRequirement(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        Action = action;
    }

    /// <summary>
    ///     当前操作名称。
    /// </summary>
    public string Action { get; }
}
