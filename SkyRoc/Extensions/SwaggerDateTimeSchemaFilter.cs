using System.Reflection;
using System.Text.Json.Serialization;
using Application.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SkyRoc.Extensions;

/// <summary>
///     将带固定格式 JSON 转换器的日期字段标注为 yyyy-MM-dd HH:mm:ss 字符串。
/// </summary>
public sealed class SwaggerDateTimeSchemaFilter : ISchemaFilter
{
    private const string Example = "2026-07-04 15:30:00";

    /// <inheritdoc />
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.MemberInfo is not PropertyInfo property)
            return;

        var converterType = property.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType;
        if (converterType != typeof(FixedDateTimeJsonConverter)
            && converterType != typeof(FixedNullableDateTimeJsonConverter))
            return;

        schema.Type = "string";
        schema.Format = null;
        schema.Example = new OpenApiString(Example);

        const string formatHint = "格式：yyyy-MM-dd HH:mm:ss（UTC）。";
        schema.Description = string.IsNullOrWhiteSpace(schema.Description)
            ? $"日期时间字符串，{formatHint}"
            : $"{schema.Description.TrimEnd()} {formatHint}";
    }
}
