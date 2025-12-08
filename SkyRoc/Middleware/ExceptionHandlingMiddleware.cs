using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Exceptions;
using Shared.Common;
using Shared.Constants;

namespace SkyRoc.Middleware;

/// <summary>
///     全局异常处理中间件
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>
    ///     异常处理
    /// </summary>
    /// <param name="context"></param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            logger.LogWarning(
                "The response has already started, the exception middleware will not be executed.");
            return; // ✅ 改成 return，而不是 throw
        }

        context.Response.Clear();
        context.Response.ContentType = "application/json; charset=utf-8";
        var response = CreateErrorResponse(exception);
        // 返回JSON响应
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        await context.Response.WriteAsync(jsonResponse);
    }

    private static ApiResponse<object> CreateErrorResponse(Exception exception)
    {
        return exception switch
        {
            NotFoundException notFoundException => new ApiResponse<object>
            {
                Msg = notFoundException.Message,
                Code = ResponseCode.NotFound
            },
            ValidationException validationException => new ApiResponse<object>
            {
                Msg = validationException.Message,
                Code = ResponseCode.ValidationError,
                Data = validationException.Errors
            },
            BusinessException businessException => new ApiResponse<object>
            {
                Msg = businessException.Message,
                Code = ResponseCode.DatabaseError
            },
            _ => new ApiResponse<object>
            {
                Msg = "服务器内部错误",
                Code = ResponseCode.InternalError
            }
        };
    }
}