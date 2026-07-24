using System.Text.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using Xunit;

namespace SkyRoc.Tests.Serialization;

/// <summary>
/// 校验默认 System.Text.Json 时间序列化（UTC 输出带 Z）以及查询侧 UTC 规范化。
/// </summary>
public class DateTimeJsonConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void AccessTokenCacheDto_ShouldSerialize_DateTimeFields_AsIso8601Utc()
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

        Assert.Contains("\"loginTime\":\"2026-05-23T14:30:45Z\"", json);
        Assert.Contains("\"expiresAt\":\"2026-05-23T16:30:45Z\"", json);
    }

    [Fact]
    public void BaseDto_ShouldDeserialize_NullableDateTimeFields_FromIso8601Utc()
    {
        const string json = """
                            {
                              "id":"11111111-1111-1111-1111-111111111111",
                              "createTime":"2026-05-23T14:30:45Z",
                              "updateTime":"2026-05-24T08:15:00Z"
                            }
                            """;

        var dto = JsonSerializer.Deserialize<TestDto>(json, JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal(new DateTime(2026, 5, 23, 14, 30, 45, DateTimeKind.Utc), dto!.CreateTime);
        Assert.Equal(DateTimeKind.Utc, dto.CreateTime!.Value.Kind);
        Assert.Equal(new DateTime(2026, 5, 24, 8, 15, 0, DateTimeKind.Utc), dto.UpdateTime);
        Assert.Equal(DateTimeKind.Utc, dto.UpdateTime!.Value.Kind);
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

    [Fact]
    public void BaseDto_ShouldDeserialize_NullableDateTimeFields_FromDateOnlyFormat()
    {
        const string json = """
                            {
                              "id":"11111111-1111-1111-1111-111111111111",
                              "createTime":"2026-07-01",
                              "updateTime":"2026-07-31"
                            }
                            """;

        var dto = JsonSerializer.Deserialize<TestDto>(json, JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal(new DateTime(2026, 7, 1), dto!.CreateTime);
        Assert.Equal(new DateTime(2026, 7, 31), dto.UpdateTime);
    }

    [Fact]
    public void RequiredDateTime_ShouldDeserialize_Iso8601Utc()
    {
        const string json = """
                            {
                              "effectiveStart":"2026-07-01T00:00:00Z"
                            }
                            """;

        var dto = JsonSerializer.Deserialize<RequiredDateDto>(json, JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), dto!.EffectiveStart);
        Assert.Equal(DateTimeKind.Utc, dto.EffectiveStart.Kind);
    }

    [Fact]
    public void NormalizeToUtc_ShouldMarkUnspecifiedAsUtc()
    {
        var source = new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Unspecified);

        var utc = Application.Serialization.DateTimeJsonFormats.NormalizeToUtc(source);

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(source.Ticks, utc.Ticks);
    }

    [Fact]
    public void AsUtcQueryEndInclusive_ShouldExpandDateOnlyToEndOfDay()
    {
        var dateOnly = new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Unspecified);

        var end = Application.Serialization.DateTimeJsonFormats.AsUtcQueryEndInclusive(dateOnly);

        Assert.NotNull(end);
        Assert.Equal(DateTimeKind.Utc, end!.Value.Kind);
        Assert.Equal(new DateTime(2026, 7, 19, 23, 59, 59, DateTimeKind.Utc).Date, end.Value.Date);
        Assert.True(end.Value > new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc));
        Assert.True(end.Value < new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SaleOrderQueryBuild_WithUnspecifiedDateRange_ShouldNotThrowWhenCompiled()
    {
        var parameters = new Application.QueryParameters.SaleOrderQueryParameters
        {
            DateStart = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Unspecified),
            DateEnd = new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Unspecified)
        };

        var predicate = parameters.QueryBuild().Compile();
        var sample = new Domain.Entities.Orders.SaleOrder
        {
            OrderDate = new DateTime(2026, 7, 10, 8, 0, 0, DateTimeKind.Utc),
            OrderNo = "SO1",
            CustomerNameSnapshot = "c",
            CustomerCodeSnapshot = "c1"
        };

        Assert.True(predicate(sample));
    }

    private sealed class TestDto : BaseDto;

    private sealed class RequiredDateDto
    {
        public DateTime EffectiveStart { get; set; }
    }
}
