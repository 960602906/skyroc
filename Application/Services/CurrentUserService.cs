using System.Security.Claims;
using Application.interfaces;
using Microsoft.AspNetCore.Http;

namespace Application.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? GetUserId()
    {
        var userId = httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userId is null ? null : Guid.Parse(userId);
    }

    public string? GetUserName()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
    }

    public string? GetEmail()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
    }

    public string? GetRole()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
    }

    public bool HasClaim(string claimType, string claimValue)
    {
        return httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value == claimValue;
    }
}