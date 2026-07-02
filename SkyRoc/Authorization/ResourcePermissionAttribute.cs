using Microsoft.AspNetCore.Authorization;

namespace SkyRoc.Authorization;

/// <summary>
///     根据控制器权限资源和当前操作生成细粒度权限要求。
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class ResourcePermissionAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    /// <summary>
    ///     创建资源操作授权声明。
    /// </summary>
    public ResourcePermissionAttribute(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (!PermissionActions.Defined.Contains(action, StringComparer.Ordinal))
            throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported permission action.");

        Action = action;
    }

    /// <summary>
    ///     当前操作名称。
    /// </summary>
    public string Action { get; }

    /// <inheritdoc />
    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new ResourcePermissionRequirement(Action);
    }
}
