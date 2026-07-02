using System.IdentityModel.Tokens.Jwt;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Authorization;

public class JwtServicePermissionTests
{
    [Fact]
    public void GenerateAccessToken_AddsDistinctPermissionClaims()
    {
        var settings = Options.Create(new JwtSettings
        {
            SecretKey = "test-only-secret-key-with-at-least-32-bytes",
            Issuer = "SkyRoc.Tests",
            Audience = "SkyRoc.Tests",
            ExpirationMinutes = 30
        });
        var service = new JwtService(settings);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "permission-user",
            NickName = "Permission User",
            Email = "permission@example.com",
            Gender = GenderType.Male,
            PasswordHash = "not-used"
        };

        var result = service.GenerateAccessToken(
            user,
            [SeedConstants.UserRoleCode],
            [PermissionCodes.System.Users.Read, PermissionCodes.System.Users.Read],
            Guid.NewGuid().ToString());

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        var permissions = token.Claims
            .Where(claim => claim.Type == AuthConstants.PermissionClaimType)
            .Select(claim => claim.Value)
            .ToList();

        Assert.Equal([PermissionCodes.System.Users.Read], permissions);
    }
}
