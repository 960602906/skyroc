using Application.DTOs.Auth;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;

namespace Application.Services;

/// <summary>
///     基于 <see cref="ICacheService" /> 的 Token 缓存服务
/// </summary>
public class TokenCacheService(
    ICacheService cache,
    IOptions<JwtSettings> jwtSettings,
    ILogger<TokenCacheService> logger)
    : ITokenCacheService
{
    private const string AccessPrefix = "access:";
    private const string RefreshPrefix = "refresh:";
    private const string UserAccessSetPrefix = "user:access:";
    private const string UserRefreshSetPrefix = "user:refresh:";

    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    /// <inheritdoc />
    public async Task SaveAccessTokenAsync(AccessTokenCacheDto data, TimeSpan? expire = null)
    {
        var ttl = expire ?? TimeSpan.FromMinutes(_jwtSettings.ExpirationMinutes);
        var key = AccessPrefix + data.Jti;
        var setKey = UserAccessSetPrefix + data.UserId;
        await cache.SetAsync(key, data, ttl);
        await cache.SetAddAsync(setKey, data.Jti, ttl);
        logger.LogInformation("Access token cached. User={UserId} Jti={Jti}", data.UserId, data.Jti);
    }

    /// <inheritdoc />
    public Task<AccessTokenCacheDto?> GetAccessTokenAsync(string jti)
    {
        return cache.GetAsync<AccessTokenCacheDto>(AccessPrefix + jti);
    }

    /// <inheritdoc />
    public Task<bool> IsAccessTokenValidAsync(string jti)
    {
        return cache.ExistsAsync(AccessPrefix + jti);
    }

    /// <inheritdoc />
    public async Task RevokeAccessTokenAsync(string jti)
    {
        var data = await GetAccessTokenAsync(jti);
        await cache.RemoveAsync(AccessPrefix + jti);
        if (data is not null)
            await cache.SetRemoveAsync(UserAccessSetPrefix + data.UserId, jti);
        logger.LogInformation("Access token revoked. Jti={Jti}", jti);
    }

    /// <inheritdoc />
    public async Task SaveRefreshTokenAsync(RefreshTokenCacheDto data, TimeSpan? expire = null)
    {
        var ttl = expire ?? TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);
        var key = RefreshPrefix + data.Token;
        var setKey = UserRefreshSetPrefix + data.UserId;
        await cache.SetAsync(key, data, ttl);
        await cache.SetAddAsync(setKey, data.Token, ttl);
        logger.LogInformation("Refresh token cached. User={UserId}", data.UserId);
    }

    /// <inheritdoc />
    public Task<RefreshTokenCacheDto?> GetRefreshTokenAsync(string refreshToken)
    {
        return cache.GetAsync<RefreshTokenCacheDto>(RefreshPrefix + refreshToken);
    }

    /// <inheritdoc />
    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var data = await GetRefreshTokenAsync(refreshToken);
        await cache.RemoveAsync(RefreshPrefix + refreshToken);
        if (data is not null)
            await cache.SetRemoveAsync(UserRefreshSetPrefix + data.UserId, refreshToken);
        logger.LogInformation("Refresh token revoked.");
    }

    /// <inheritdoc />
    public async Task RevokeAllByUserAsync(Guid userId)
    {
        var accessSetKey = UserAccessSetPrefix + userId;
        var refreshSetKey = UserRefreshSetPrefix + userId;

        var jtis = await cache.SetMembersAsync(accessSetKey);
        foreach (var jti in jtis) await cache.RemoveAsync(AccessPrefix + jti);
        await cache.RemoveAsync(accessSetKey);

        var tokens = await cache.SetMembersAsync(refreshSetKey);
        foreach (var token in tokens) await cache.RemoveAsync(RefreshPrefix + token);
        await cache.RemoveAsync(refreshSetKey);

        logger.LogInformation(
            "All tokens revoked for user {UserId}. Access={AccessCount} Refresh={RefreshCount}",
            userId, jtis.Count, tokens.Count);
    }
}
