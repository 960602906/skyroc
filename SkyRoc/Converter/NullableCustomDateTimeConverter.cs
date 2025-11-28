using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyRoc.Converter;
/// <summary>
/// 可空日期时间转换器
/// </summary>
public class NullableCustomDateTimeConverter(string format): JsonConverter<DateTime?>
{
    /// <summary>
    ///  无参构造函数
    /// </summary>
    public NullableCustomDateTimeConverter() : this("yyyy-MM-dd HH:mm:ss")
    {
    }
    /// <summary>
    ///     读取并转换可空日期时间字符串
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            var dateString = reader.GetString();
            if (DateTime.TryParse(dateString, out var date))
            {
                return date;
            }
        }
        return null;
    }
    
    /// <summary>
    ///     时间写入格式化的可空日期时间字符串
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(format));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}