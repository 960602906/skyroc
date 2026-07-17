using Application.DTOs.Auth;

namespace Application.Interfaces;

/// <summary>
///     Token 缓存服务：把 Access Token 与 Refresh Token 都放在 Redis，不再写库
/// </summary>
public interface ITokenCacheService
{
    // ========== Access Token ==========

    /// <summary>
    ///     保存访问令牌（按 JTI 作为唯一键）
    /// </summary>
    Task SaveAccessTokenAsync(AccessTokenCacheDto data, TimeSpan? expire = null);

    /// <summary>
    ///     根据 JTI 读取访问令牌信息
    /// </summary>
    Task<AccessTokenCacheDto?> GetAccessTokenAsync(string jti);

    /// <summary>
    ///     校验访问令牌是否仍然有效（存在于 Redis 即视为有效）
    /// </summary>
    Task<bool> IsAccessTokenValidAsync(string jti);

    /// <summary>
    ///     撤销单个访问令牌
    /// </summary>
    Task RevokeAccessTokenAsync(string jti);

    // ========== Refresh Token ==========

    /// <summary>
    ///     保存刷新令牌
    /// </summary>
    Task SaveRefreshTokenAsync(RefreshTokenCacheDto data, TimeSpan? expire = null);

    /// <summary>
    ///     根据 refresh token 读取信息
    /// </summary>
    Task<RefreshTokenCacheDto?> GetRefreshTokenAsync(string refreshToken);

    /// <summary>
    ///     撤销单个刷新令牌
    /// </summary>
    Task RevokeRefreshTokenAsync(string refreshToken);

    // ========== 按用户批量 ==========

    /// <summary>
    ///     撤销指定用户的所有令牌（访问令牌 + 刷新令牌），用于强制登出
    /// </summary>
    Task RevokeAllByUserAsync(Guid userId);
}
