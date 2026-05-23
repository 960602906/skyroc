using Infrastructure.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace SkyRoc.Tests.Caching;

public class RedisHealthCheckTests
{
    [Fact]
    public async Task Disconnected_redis_should_be_reported_as_degraded_when_failure_status_is_degraded()
    {
        var check = new RedisHealthCheck(new FakeRedisConnectionProbe
        {
            IsConnected = false
        });

        var result = await check.CheckHealthAsync(CreateContext(HealthStatus.Degraded));

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Redis is not connected", result.Description);
    }

    [Fact]
    public async Task Ping_failure_should_be_reported_as_unhealthy_when_failure_status_is_unhealthy()
    {
        var check = new RedisHealthCheck(new FakeRedisConnectionProbe
        {
            ThrowOnPing = true
        });

        var result = await check.CheckHealthAsync(CreateContext(HealthStatus.Unhealthy));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Redis ping failed", result.Description);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task Reachable_redis_should_be_reported_as_healthy()
    {
        var check = new RedisHealthCheck(new FakeRedisConnectionProbe());

        var result = await check.CheckHealthAsync(CreateContext(HealthStatus.Degraded));

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Redis is reachable", result.Description);
    }

    private static HealthCheckContext CreateContext(HealthStatus failureStatus)
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "redis",
                _ => new NoopHealthCheck(),
                failureStatus,
                tags: null)
        };
    }

    private sealed class FakeRedisConnectionProbe : IRedisConnectionProbe
    {
        public bool IsConnected { get; set; } = true;

        public bool ThrowOnPing { get; set; }

        public Task PingAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnPing) throw new InvalidOperationException("redis ping failed");
            return Task.CompletedTask;
        }
    }

    private sealed class NoopHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
