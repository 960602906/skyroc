using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SkyRoc.ModelBinding;

/// <summary>
/// 为 DateTime / DateTime? 注册 <see cref="UtcDateTimeModelBinder"/>。
/// </summary>
public sealed class UtcDateTimeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;
        if (modelType == typeof(DateTime))
            return new UtcDateTimeModelBinder(isNullable: false);

        if (modelType == typeof(DateTime?))
            return new UtcDateTimeModelBinder(isNullable: true);

        return null;
    }
}
