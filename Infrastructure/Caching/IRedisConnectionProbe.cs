namespace Infrastructure.Caching;

/// <summary>
///     Redis 连接探针：用于缓存健康检查，避免健康检查直接依赖复杂的连接对象。
/// </summary>
public interface IRedisConnectionProbe
{
    bool IsConnected { get; }

    Task PingAsync(CancellationToken cancellationToken = default);
}
