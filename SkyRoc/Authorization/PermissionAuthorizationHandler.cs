using Microsoft.AspNetCore.Authorization;
using Shared.Constants;

namespace SkyRoc.Authorization;

/// <summary>
///     根据 JWT 权限声明处理 API 权限要求。
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return Task.CompletedTask;

        var permissions = context.User.FindAll(AuthConstants.PermissionClaimType);
        if (permissions.Any(claim =>
                string.Equals(claim.Value, PermissionCodes.All, StringComparison.Ordinal)
                || string.Equals(claim.Value, requirement.PermissionCode, StringComparison.Ordinal)))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
