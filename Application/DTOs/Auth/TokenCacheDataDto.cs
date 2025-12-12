namespace Application.DTOs.Auth;

public class TokenCacheDataDto
{
 
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];
    public DateTime LoginTime { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string TokenJti { get; set; } = string.Empty; // JWT ID
}