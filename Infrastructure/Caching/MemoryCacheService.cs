using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Shared.Common;

namespace Infrastructure.Caching;

/// <summary>
///     内存降级实现：当 Redis 不可用或 <c>Redis:Enabled=false</c> 时使用。
///     注意：仅限单实例进程，不跨进程共享。
/// </summary>
public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ConcurrentDictionary<string, HashSet<string>> _sets = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (cache.TryGetValue<string>(key, out var json) && !string.IsNullOrEmpty(json))
            return Task.FromResult(JsonSerializer.Deserialize<T>(json, JsonOpts));
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, JsonOpts);
        var opts = new MemoryCacheEntryOptions();
        if (expire.HasValue) opts.AbsoluteExpirationRelativeToNow = expire;
        cache.Set(key, json, opts);
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        _sets.TryRemove(key, out _);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(cache.TryGetValue(key, out _) || _sets.ContainsKey(key));
    }

    public Task SetAddAsync(string setKey, string value, TimeSpan? expire = null,
        CancellationToken ct = default)
    {
        var set = _sets.GetOrAdd(setKey, _ => []);
        lock (set)
        {
            set.Add(value);
        }

        return Task.CompletedTask;
    }

    public Task SetRemoveAsync(string setKey, string value, CancellationToken ct = default)
    {
        if (_sets.TryGetValue(setKey, out var set))
            lock (set)
            {
                set.Remove(value);
                if (set.Count == 0) _sets.TryRemove(setKey, out _);
            }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> SetMembersAsync(string setKey, CancellationToken ct = default)
    {
        if (!_sets.TryGetValue(setKey, out var set))
            return Task.FromResult<IReadOnlyList<string>>([]);
        lock (set)
        {
            return Task.FromResult<IReadOnlyList<string>>(set.ToList());
        }
    }
}
