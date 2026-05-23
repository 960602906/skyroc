using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Auth;

/// <summary>
///     存放在 Redis 中的刷新令牌元数据
/// </summary>
public class RefreshTokenCacheDto
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;

    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime CreatedAt { get; set; }

    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime ExpiresAt { get; set; }
}
