using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;
using StackExchange.Redis;

namespace Infrastructure.Caching;

/// <summary>
///     基于 StackExchange.Redis 的 <see cref="ICacheService" /> 实现
/// </summary>
public class RedisCacheService(
    IConnectionMultiplexer redis,
    IOptions<RedisOptions> options,
    ILogger<RedisCacheService> logger)
    : ICacheService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RedisOptions _options = options.Value;
    private readonly IDatabase _db = redis.GetDatabase(options.Value.Database);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(BuildKey(key));
        if (value.IsNullOrEmpty) return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value!, JsonOpts);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize cache value for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, JsonOpts);
        if (expire.HasValue)
            await _db.StringSetAsync(BuildKey(key), json, expire.Value);
        else
            await _db.StringSetAsync(BuildKey(key), json);
    }

    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        return _db.KeyDeleteAsync(BuildKey(key));
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return _db.KeyExistsAsync(BuildKey(key));
    }

    public async Task SetAddAsync(string setKey, string value, TimeSpan? expire = null,
        CancellationToken ct = default)
    {
        var key = BuildKey(setKey);
        await _db.SetAddAsync(key, value);
        if (expire.HasValue) await _db.KeyExpireAsync(key, expire.Value);
    }

    public Task SetRemoveAsync(string setKey, string value, CancellationToken ct = default)
    {
        return _db.SetRemoveAsync(BuildKey(setKey), value);
    }

    public async Task<IReadOnlyList<string>> SetMembersAsync(string setKey, CancellationToken ct = default)
    {
        var members = await _db.SetMembersAsync(BuildKey(setKey));
        return members.Select(m => m.ToString()).ToList();
    }

    private string BuildKey(string key)
    {
        return string.IsNullOrEmpty(_options.InstanceName) ? key : _options.InstanceName + key;
    }
}
