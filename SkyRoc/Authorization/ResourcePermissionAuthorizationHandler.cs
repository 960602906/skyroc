using Microsoft.AspNetCore.Authorization;
using Shared.Constants;

namespace SkyRoc.Authorization;

/// <summary>
///     校验基础资料通用操作对应的资源权限。
/// </summary>
public sealed class ResourcePermissionAuthorizationHandler
    : AuthorizationHandler<ResourcePermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourcePermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || context.Resource is not HttpContext httpContext)
            return Task.CompletedTask;

        var resource = httpContext.GetEndpoint()?.Metadata.GetMetadata<PermissionResourceAttribute>();
        if (resource is null) return Task.CompletedTask;

        var requiredPermission = $"{resource.Resource}:{requirement.Action}";
        var permissions = context.User.FindAll(AuthConstants.PermissionClaimType);
        if (permissions.Any(claim =>
                string.Equals(claim.Value, PermissionCodes.All, StringComparison.Ordinal)
                || string.Equals(claim.Value, requiredPermission, StringComparison.Ordinal)))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
