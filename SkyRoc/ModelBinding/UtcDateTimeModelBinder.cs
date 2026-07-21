using System.Globalization;
using Application.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SkyRoc.ModelBinding;

/// <summary>
/// 将 query/form 绑定的 DateTime 规范为 UTC，避免 Npgsql timestamptz 写入 Unspecified。
/// </summary>
public sealed class UtcDateTimeModelBinder : IModelBinder
{
    private readonly bool _isNullable;

    public UtcDateTimeModelBinder(bool isNullable)
    {
        _isNullable = isNullable;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var raw = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (_isNullable)
                bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (DateTime.TryParseExact(
                raw,
                DateTimeJsonFormats.Supported,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                out var exact)
            || DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                out exact))
        {
            bindingContext.Result = ModelBindingResult.Success(DateTimeJsonFormats.NormalizeToUtc(exact));
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            $"无法解析日期时间: {raw}");
        return Task.CompletedTask;
    }
}
