using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Serialization;

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
