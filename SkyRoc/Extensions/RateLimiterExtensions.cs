using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SkyRoc.Extensions;

/// <summary>
///     全局限流扩展。
/// </summary>
public static class RateLimiterExtensions
{
    /// <summary>
    ///     注册按客户端 IP 的固定窗口限流，作为突发流量兜底保护。
    /// </summary>
    /// <param name="services">应用服务注册集合。</param>
    /// <returns>完成限流注册后的原服务集合。</returns>
    public static IServiceCollection AddSkyRocRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromSeconds(10),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });
        });

        return services;
    }
}
