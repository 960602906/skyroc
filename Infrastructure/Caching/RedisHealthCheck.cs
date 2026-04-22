using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Infrastructure.Caching;

/// <summary>
///     简单的 Redis 健康检查：基于 <see cref="IConnectionMultiplexer.IsConnected" /> + PING
/// </summary>
public class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!redis.IsConnected)
                return HealthCheckResult.Unhealthy("Redis is not connected");
            await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy("Redis is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis ping failed", ex);
        }
    }
}
