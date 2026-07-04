using System.Text;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SkyRoc.Extensions;

/// <summary>
///     为整型枚举 Schema 补充取值说明，便于 Swagger 阅读。
/// </summary>
public sealed class SwaggerEnumSchemaFilter : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var enumType = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
        if (!enumType.IsEnum)
            return;

        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(schema.Description))
        {
            builder.AppendLine(schema.Description.Trim());
            builder.AppendLine();
        }

        builder.AppendLine("枚举值：");
        foreach (var name in Enum.GetNames(enumType))
        {
            var field = enumType.GetField(name)!;
            var value = Convert.ToInt64(field.GetValue(null));
            builder.Append("- ").Append(value).Append(" = ").AppendLine(name);
        }

        schema.Description = builder.ToString().TrimEnd();
    }
}
