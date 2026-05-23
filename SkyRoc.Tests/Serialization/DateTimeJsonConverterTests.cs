using System.Text.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using Xunit;

namespace SkyRoc.Tests.Serialization;

public class DateTimeJsonConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void AccessTokenCacheDto_ShouldSerialize_DateTimeFields_AsFixedFormat()
    {
        var dto = new AccessTokenCacheDto
        {
            UserId = Guid.NewGuid(),
            Username = "tester",
            Email = "tester@example.com",
            Roles = ["admin"],
            Jti = "jti-1",
            LoginTime = new DateTime(2026, 5, 23, 14, 30, 45, DateTimeKind.Utc),
            ExpiresAt = new DateTime(2026, 5, 23, 16, 30, 45, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        Assert.Contains("\"loginTime\":\"2026-05-23 14:30:45\"", json);
        Assert.Contains("\"expiresAt\":\"2026-05-23 16:30:45\"", json);
    }

    [Fact]
    public void BaseDto_ShouldDeserialize_NullableDateTimeFields_FromFixedFormat()
    {
        const string json = """
                            {
                              "id":"11111111-1111-1111-1111-111111111111",
                              "createTime":"2026-05-23 14:30:45",
                              "updateTime":"2026-05-24 08:15:00"
                            }
                            """;

        var dto = JsonSerializer.Deserialize<TestDto>(json, JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal(new DateTime(2026, 5, 23, 14, 30, 45), dto!.CreateTime);
        Assert.Equal(new DateTime(2026, 5, 24, 8, 15, 0), dto.UpdateTime);
    }

    [Fact]
    public void BaseDto_ShouldReject_InvalidNullableDateTime()
    {
        const string json = """
                            {
                              "id":"11111111-1111-1111-1111-111111111111",
                              "createTime":"not-a-date"
                            }
                            """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestDto>(json, JsonOptions));
    }

    private sealed class TestDto : BaseDto;
}
