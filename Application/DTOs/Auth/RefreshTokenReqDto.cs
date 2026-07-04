namespace Application.DTOs.Auth;

/// <summary>
///     刷新令牌或注销请求。
/// </summary>
public class RefreshTokenReqDto
{
    /// <summary>
    ///     刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}