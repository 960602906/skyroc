namespace Application.DTOs.Auth;

public class RefreshTokenReqDto
{
    /// <summary>
    ///     刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}