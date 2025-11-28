namespace Application.DTOs.Auth;

/// <summary>
///     登录请求 DTO
/// </summary>
public class LoginReqDto
{
    /// <summary>
    ///     用户名
    /// </summary>
    public required string Username { get; set; } = string.Empty;

    /// <summary>
    ///     密码
    /// </summary>
    public required string Password { get; set; } = string.Empty;
}