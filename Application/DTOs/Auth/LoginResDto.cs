namespace Application.DTOs.Auth;

/// <summary>
///     登录响应 DTO
/// </summary>
public class LoginResDto
{
    /// <summary>
    ///     访问令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    ///     刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    ///     令牌类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    ///     过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }
}