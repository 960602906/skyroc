using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.DTOs.Auth;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Common;
using Shared.Constants;

namespace Application.Services;

/// <summary>
///     JWT 服务
/// </summary>
public class JwtService(IOptions<JwtSettings> jwtSettings) : IJwtService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    /// <inheritdoc />
    public AccessTokenResult GenerateAccessToken(
        User user,
        IEnumerable<string> roleCodes,
        IEnumerable<string> permissionCodes,
        string? currentRoleId)
    {
        var jti = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jti)
        };
        claims.AddRange(roleCodes.Select(roleCode => new Claim(ClaimTypes.Role, roleCode)));
        claims.AddRange(permissionCodes
            .Distinct(StringComparer.Ordinal)
            .Select(permissionCode => new Claim(AuthConstants.PermissionClaimType, permissionCode)));

        if (!string.IsNullOrWhiteSpace(currentRoleId))
            claims.Add(new Claim(AuthConstants.CurrentRoleIdClaimType, currentRoleId));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new AccessTokenResult(_tokenHandler.WriteToken(token), jti, expiresAt);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[AuthConstants.RefreshTokenByteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <inheritdoc />
    public bool ValidateToken(string token)
    {
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        try
        {
            _tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Guid? GetUserIdFromToken(string token)
    {
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        try
        {
            var principal = _tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId)) return userId;
            return null;
        }
        catch
        {
            return null;
        }
    }
}
