using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Caching;

/// <summary>
///     简单的 Redis 健康检查：基于 <see cref="IConnectionMultiplexer.IsConnected" /> + PING
/// </summary>
public class RedisHealthCheck(IRedisConnectionProbe redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var failureStatus = context.Registration.FailureStatus;

        try
        {
            if (!redis.IsConnected)
                return new HealthCheckResult(failureStatus, "Redis is not connected");
            await redis.PingAsync(cancellationToken);
            return HealthCheckResult.Healthy("Redis is reachable");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(failureStatus, "Redis ping failed", ex);
        }
    }
}
