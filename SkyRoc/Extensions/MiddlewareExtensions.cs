using SkyRoc.Middleware;

namespace SkyRoc.Extensions;

/// <summary>
///     中间件扩展方法
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    ///     使用全局异常处理中间件
    /// </summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }

   
}