using Application.Exceptions;
using Shared.Constants;

namespace SkyRoc.Middleware;

/// <summary>
///     将应用异常映射为响应体业务码（HTTP 层固定 200，不再用状态码区分业务结果）。
/// </summary>
internal static class ExceptionHttpStatusMapper
{
    /// <summary>返回指定异常对应的稳定业务响应码。</summary>
    /// <param name="exception">请求管道捕获的异常。</param>
    /// <returns>写入响应体 <c>code</c> 与审计摘要的业务码。</returns>
    public static ResponseCode GetResponseCode(Exception exception) => exception switch
    {
        NotFoundException => ResponseCode.NotFound,
        ValidationException => ResponseCode.ValidationError,
        BusinessException => ResponseCode.DatabaseError,
        _ => ResponseCode.InternalError
    };

    /// <summary>
    ///     兼容旧调用：返回与业务码数值相同的整数，仅用于审计摘要展示，不作为 HTTP 状态码。
    /// </summary>
    public static int GetStatusCode(Exception exception) => (int)GetResponseCode(exception);
}
