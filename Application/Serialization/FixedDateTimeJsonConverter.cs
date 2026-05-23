using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Serialization;

internal static class DateTimeJsonFormats
{
    public const string Default = "yyyy-MM-dd HH:mm:ss";

    public static readonly string[] Supported =
    [
        Default,
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
        "O"
    ];
}

public sealed class FixedDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"无法将 {reader.TokenType} 转换为日期时间。");

        var dateString = reader.GetString();
        if (string.IsNullOrWhiteSpace(dateString))
            throw new JsonException("日期时间字符串不能为空。");

        if (DateTime.TryParseExact(
                dateString,
                DateTimeJsonFormats.Supported,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                out var date))
            return date;

        throw new JsonException($"无法解析日期时间: {dateString}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateTimeJsonFormats.Default, CultureInfo.InvariantCulture));
    }
}
