using System.Security.Claims;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Shared.Constants;

namespace Application.Services;

/// <inheritdoc />
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    /// <inheritdoc />
    public Guid? GetUserId()
    {
        var userId = httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userId is null ? null : Guid.Parse(userId);
    }

    /// <inheritdoc />
    public string? GetUserName()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <inheritdoc />
    public string? GetEmail()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <inheritdoc />
    public string? GetRole()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirst(AuthConstants.CurrentRoleIdClaimType)?.Value;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRoles()
    {
        return httpContextAccessor.HttpContext?.User?
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList() ?? [];
    }

    /// <inheritdoc />
    public bool HasClaim(string claimType, string claimValue)
    {
        return httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value == claimValue;
    }
}
