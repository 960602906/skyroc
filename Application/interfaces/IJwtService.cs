using Domain.Entities;

namespace Application.interfaces;

/// <summary>
///     JWT 服务接口
/// </summary>
public interface IJwtService
{
    /// <summary>
    ///     生成访问令牌
    /// </summary>
    string GenerateAccessToken(User user, List<string> roles);

    /// <summary>
    ///     生成刷新令牌
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    ///     验证令牌
    /// </summary>
    bool ValidateToken(string token);

    /// <summary>
    ///     从令牌中获取用户 ID
    /// </summary>
    int? GetUserIdFromToken(string token);
}