using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Middleware;

namespace SkyRoc.Filters;

/// <summary>
///     从控制器返回的 <see cref="ApiResponse{T}"/> 中提取业务码并写入请求上下文，供审计中间件判断成败。
/// </summary>
public sealed class ApiBusinessCodeResultFilter : IAsyncResultFilter
{
    /// <inheritdoc />
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: not null } objectResult
            && TryGetResponseCode(objectResult.Value, out var code))
        {
            ApiResponseHttp.MarkBusinessCode(context.HttpContext, code);
        }

        await next();
    }

    private static bool TryGetResponseCode(object value, out ResponseCode code)
    {
        var property = value.GetType().GetProperty(nameof(ApiResponse<int>.Code));
        if (property?.PropertyType == typeof(ResponseCode)
            && property.GetValue(value) is ResponseCode responseCode)
        {
            code = responseCode;
            return true;
        }

        code = default;
        return false;
    }
}
