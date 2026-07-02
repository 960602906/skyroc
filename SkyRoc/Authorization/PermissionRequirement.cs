using Microsoft.AspNetCore.Authorization;

namespace SkyRoc.Authorization;

/// <summary>
///     要求当前用户具有指定的 API 权限编码。
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     创建权限要求。
    /// </summary>
    public PermissionRequirement(string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        PermissionCode = permissionCode;
    }

    /// <summary>
    ///     接口要求的权限编码。
    /// </summary>
    public string PermissionCode { get; }
}
