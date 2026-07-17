using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Exceptions;
using Shared.Common;
using Shared.Constants;

namespace SkyRoc.Middleware;

/// <summary>
///     全局异常处理中间件：HTTP 固定 200，业务结果写入响应体 code。
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
            return;
        }

        var response = CreateErrorResponse(exception);
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";
        ApiResponseHttp.MarkBusinessCode(context, response.Code);

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
