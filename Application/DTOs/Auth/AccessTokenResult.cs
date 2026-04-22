namespace Application.DTOs.Auth;

/// <summary>
///     JWT 生成结果
/// </summary>
public record AccessTokenResult(string Token, string Jti, DateTime ExpiresAt);
