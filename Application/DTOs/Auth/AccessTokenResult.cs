using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Auth;

/// <summary>
///     JWT 生成结果
/// </summary>
public record AccessTokenResult(
    string Token,
    string Jti,
    [property: JsonConverter(typeof(FixedDateTimeJsonConverter))]
    DateTime ExpiresAt);
