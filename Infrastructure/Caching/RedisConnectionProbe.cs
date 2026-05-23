using StackExchange.Redis;

namespace Infrastructure.Caching;

/// <summary>
///     基于 <see cref="IConnectionMultiplexer" /> 的 Redis 连接探针。
/// </summary>
public class RedisConnectionProbe(IConnectionMultiplexer redis) : IRedisConnectionProbe
{
    public bool IsConnected => redis.IsConnected;

    public Task PingAsync(CancellationToken cancellationToken = default)
    {
        return redis.GetDatabase().PingAsync();
    }
}
