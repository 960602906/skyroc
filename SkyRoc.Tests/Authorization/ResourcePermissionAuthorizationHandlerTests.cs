using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared.Constants;
using SkyRoc.Authorization;
using Xunit;

namespace SkyRoc.Tests.Authorization;

public class ResourcePermissionAuthorizationHandlerTests
{
    private readonly ResourcePermissionAuthorizationHandler _handler = new();

    [Fact]
    public async Task HandleAsync_Succeeds_WhenUserHasResourceOperationPermission()
    {
        var requirement = new ResourcePermissionRequirement(PermissionActions.Read);
        var context = CreateContext(requirement, PermissionCodes.Business.Goods.Read);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Succeeds_WhenUserHasWildcardPermission()
    {
        var requirement = new ResourcePermissionRequirement(PermissionActions.Delete);
        var context = CreateContext(requirement, PermissionCodes.All);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_DoesNotSucceed_WhenUserLacksResourceOperationPermission()
    {
        var requirement = new ResourcePermissionRequirement(PermissionActions.Delete);
        var context = CreateContext(requirement, PermissionCodes.Business.Goods.Read);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static AuthorizationHandlerContext CreateContext(
        ResourcePermissionRequirement requirement,
        params string[] permissions)
    {
        var claims = permissions.Select(permission =>
            new Claim(AuthConstants.PermissionClaimType, permission));
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(
                new PermissionResourceAttribute(PermissionCodes.Business.Goods.Resource)),
            "goods-test"));

        return new AuthorizationHandlerContext([requirement], user, httpContext);
    }
}
