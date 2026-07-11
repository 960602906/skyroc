using Application.Exceptions;

namespace SkyRoc.Middleware;

/// <summary>将应用异常映射为客户端响应与操作审计共同使用的 HTTP 状态码。</summary>
internal static class ExceptionHttpStatusMapper
{
    /// <summary>返回指定异常对应的稳定 HTTP 状态码。</summary>
    /// <param name="exception">请求管道捕获的异常。</param>
    /// <returns>写入 HTTP 响应和审计摘要的状态码。</returns>
    public static int GetStatusCode(Exception exception) => exception switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        ValidationException => StatusCodes.Status422UnprocessableEntity,
        BusinessException => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError
    };
}
