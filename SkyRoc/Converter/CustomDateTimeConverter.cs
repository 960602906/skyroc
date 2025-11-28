using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkyRoc.Converter;
/// <summary>
/// 自定义日期时间格式化转换器
/// </summary>
public class CustomDateTimeConverter(string format): JsonConverter<DateTime>
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public CustomDateTimeConverter() : this("yyyy-MM-dd HH:mm:ss")
    {
    }
    /// <summary>
    ///  读取并转换日期时间字符串
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="JsonException"></exception>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var dateString = reader.GetString();
            if (DateTime.TryParse(dateString, out var date))
            {
                return date;
            }
        }
            
        throw new JsonException($"无法解析日期时间: {reader.GetString()}");
    }
    
    /// <summary>
    ///  写入格式化的日期时间字符串
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(format));
    }
}