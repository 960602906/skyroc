namespace Application.DTOs.Auth;

/// <summary>
///     存放在 Redis 中的刷新令牌元数据
/// </summary>
public class RefreshTokenCacheDto
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
