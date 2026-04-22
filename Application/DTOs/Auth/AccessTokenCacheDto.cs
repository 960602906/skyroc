namespace Application.DTOs.Auth;

/// <summary>
///     存放在 Redis 中的访问令牌元数据
/// </summary>
public class AccessTokenCacheDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];
    public string Jti { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceInfo { get; set; }
}
