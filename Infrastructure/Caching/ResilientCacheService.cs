using Microsoft.Extensions.Logging;
using Shared.Common;
using StackExchange.Redis;

namespace Infrastructure.Caching;

/// <summary>
///     组合缓存实现：优先使用 Redis，Redis 在运行时不可用时自动回退到本机内存。
/// </summary>
public class ResilientCacheService(
    IEnumerable<IRedisCacheService> redisCaches,
    MemoryCacheService memoryCache,
    ILogger<ResilientCacheService> logger)
    : ICacheService
{
    private readonly IRedisCacheService? _redis = redisCaches.FirstOrDefault();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_redis is not null)
            try
            {
                var value = await _redis.GetAsync<T>(key, ct);
                if (value is not null) return value;
            }
            catch (Exception ex) when (IsTransientRedisException(ex))
            {
                LogFallback("GET", key, ex);
            }

        return await memoryCache.GetAsync<T>(key, ct);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default)
    {
        if (_redis is not null)
            try
            {
                await _redis.SetAsync(key, value, expire, ct);
                await memoryCache.RemoveAsync(key, ct);
                return;
            }
            catch (Exception ex) when (IsTransientRedisException(ex))
            {
                LogFallback("SET", key, ex);
            }

        await memoryCache.SetAsync(key, value, expire, ct);
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        var removed = false;

        if (_redis is not null)
            try
            {
                removed = await _redis.RemoveAsync(key, ct);
            }
            catch (Exception ex) when (IsTransientRedisException(ex))
            {
                LogFallback("REMOVE", key, ex);
            }

        return await memoryCache.RemoveAsync(key, ct) || removed;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        if (_redis is not null)
            try
            {
                if (await _redis.ExistsAsync(key, ct)) return true;
            }
            catch (Exception ex) when (IsTransientRedisException(ex))
            {
                LogFallback("EXISTS", key, ex);
            }

        return await memoryCache.ExistsAsync(key, ct);
    }

    public async Task SetAddAsync(string setKey, string value, TimeSpan? expire = null,
        CancellationToken ct = default)
    {
        if (_redis is not null)
            try
            {
                await _redis.SetAddAsync(setKey, value, expire, ct);
                await memoryCache.SetRemoveAsync(setKey, value, ct);
                return;
            }
            catch (Exception ex) when (IsTransientRedisException(ex))
            {
                LogFallback("SETADD", setKey, ex);
            }

        await memoryCache.SetAddAsync(setKey, value, expire, ct);
    }

    public async Task SetRemoveAsync(string setKey, string value, CancellationToken ct = default)
    {
        if (_redis is not null)
            try
            {
                await _redis.SetRemoveAsync(setKey, value, ct);
            }
            catch (Exception ex) when (IsTransientRedisException(ex))
            {
                LogFallback("SETREMOVE", setKey, ex);
            }

        await memoryCache.SetRemoveAsync(setKey, value, ct);
    }

    public async Task<IReadOnlyList<string>> SetMembersAsync(string setKey, CancellationToken ct = default)
    {
        IReadOnlyList<string> redisMembers = [];

        if (_redis is not null)
            try
            {
                redisMembers = await _redis.SetMembersAsync(setKey, ct);
            }
            catch (Exception ex) when (IsTransientRedisException(ex))
            {
                LogFallback("SETMEMBERS", setKey, ex);
                return await memoryCache.SetMembersAsync(setKey, ct);
            }

        var memoryMembers = await memoryCache.SetMembersAsync(setKey, ct);
        if (memoryMembers.Count == 0) return redisMembers;
        if (redisMembers.Count == 0) return memoryMembers;

        return redisMembers
            .Concat(memoryMembers)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private void LogFallback(string operation, string key, Exception ex)
    {
        logger.LogWarning(ex,
            "Redis cache operation {Operation} failed for key {Key}, falling back to in-memory cache.",
            operation, key);
    }

    private static bool IsTransientRedisException(Exception ex)
    {
        return ex is RedisConnectionException or RedisTimeoutException or ObjectDisposedException;
    }
}
