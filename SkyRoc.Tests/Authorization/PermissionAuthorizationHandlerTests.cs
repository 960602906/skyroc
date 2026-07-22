using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Shared.Constants;
using SkyRoc.Authorization;
using Xunit;

namespace SkyRoc.Tests.Authorization;

public class PermissionAuthorizationHandlerTests
{
    private readonly PermissionAuthorizationHandler _handler = new();

    [Fact]
    public void DefinedPermissionCodes_AreUniqueAndFollowConvention()
    {
        Assert.Equal(PermissionCodes.Defined.Count, PermissionCodes.Defined.Distinct().Count());
        Assert.All(PermissionCodes.Defined, code =>
            Assert.Matches("^[a-z][a-z0-9-]*:[a-z][a-z0-9-]*:[a-z][a-z0-9-]*$", code));
    }

    [Fact]
    public void DefinedPermissionCodes_ContainAllAiAndMcpPermissions()
    {
        Assert.Contains(PermissionCodes.Ai.UseAssistant, PermissionCodes.Defined);
        Assert.Contains(PermissionCodes.Ai.CreateOrderDraft, PermissionCodes.Defined);
        Assert.Contains(PermissionCodes.Ai.ConfirmActionDraft, PermissionCodes.Defined);
        Assert.Contains(PermissionCodes.Ai.ManageMcpTokens, PermissionCodes.Defined);
    }

    [Fact]
    public async Task HandleAsync_Succeeds_WhenUserHasRequiredPermission()
    {
        var requirement = new PermissionRequirement(PermissionCodes.System.Users.Read);
        var context = CreateContext(requirement, PermissionCodes.System.Users.Read);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Succeeds_WhenUserHasWildcardPermission()
    {
        var requirement = new PermissionRequirement(PermissionCodes.System.Users.Delete);
        var context = CreateContext(requirement, PermissionCodes.All);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_DoesNotSucceed_WhenUserLacksRequiredPermission()
    {
        var requirement = new PermissionRequirement(PermissionCodes.System.Users.Delete);
        var context = CreateContext(requirement, PermissionCodes.System.Users.Read);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_DoesNotSucceed_WhenUserIsAnonymous()
    {
        var requirement = new PermissionRequirement(PermissionCodes.System.Users.Read);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(AuthConstants.PermissionClaimType, PermissionCodes.System.Users.Read)
        ]));
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static AuthorizationHandlerContext CreateContext(
        IAuthorizationRequirement requirement,
        params string[] permissions)
    {
        var claims = permissions
            .Select(permission => new Claim(AuthConstants.PermissionClaimType, permission));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return new AuthorizationHandlerContext([requirement], principal, null);
    }
}
