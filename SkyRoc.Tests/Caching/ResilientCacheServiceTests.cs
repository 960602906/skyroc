using Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SkyRoc.Tests.Caching;

public class ResilientCacheServiceTests
{
    [Fact]
    public async Task Set_and_get_should_fall_back_to_memory_when_redis_is_temporarily_unavailable()
    {
        var redis = new FakeRedisCacheService
        {
            ThrowOnSet = true
        };
        var cache = CreateCache(redis);
        var dto = new TestPayload("token-1");

        await cache.SetAsync("access:1", dto, TimeSpan.FromMinutes(5));

        redis.ThrowOnSet = false;

        var actual = await cache.GetAsync<TestPayload>("access:1");

        Assert.NotNull(actual);
        Assert.Equal(dto.Value, actual!.Value);
        Assert.True(await cache.ExistsAsync("access:1"));
    }

    [Fact]
    public async Task Remove_should_clear_memory_fallback_copy_even_if_redis_is_available_again()
    {
        var redis = new FakeRedisCacheService
        {
            ThrowOnSet = true
        };
        var cache = CreateCache(redis);

        await cache.SetAsync("refresh:1", new TestPayload("refresh-token"), TimeSpan.FromMinutes(5));

        redis.ThrowOnSet = false;
        await cache.RemoveAsync("refresh:1");

        Assert.Null(await cache.GetAsync<TestPayload>("refresh:1"));
        Assert.False(await cache.ExistsAsync("refresh:1"));
    }

    [Fact]
    public async Task Set_members_should_merge_redis_and_memory_entries_during_recovery_window()
    {
        var redis = new FakeRedisCacheService();
        var cache = CreateCache(redis);
        const string setKey = "user:access:1";

        await cache.SetAddAsync(setKey, "redis-token", TimeSpan.FromMinutes(5));

        redis.ThrowOnSetAdd = true;
        await cache.SetAddAsync(setKey, "memory-token", TimeSpan.FromMinutes(5));

        redis.ThrowOnSetAdd = false;

        var members = await cache.SetMembersAsync(setKey);

        Assert.Equal(2, members.Count);
        Assert.Contains("redis-token", members);
        Assert.Contains("memory-token", members);
    }

    [Fact]
    public async Task Memory_remove_should_also_delete_set_storage()
    {
        var memory = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));

        await memory.SetAddAsync("user:refresh:1", "token-1", TimeSpan.FromMinutes(5));
        await memory.RemoveAsync("user:refresh:1");

        var members = await memory.SetMembersAsync("user:refresh:1");

        Assert.Empty(members);
        Assert.False(await memory.ExistsAsync("user:refresh:1"));
    }

    private static ResilientCacheService CreateCache(IRedisCacheService redis)
    {
        var memory = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        return new ResilientCacheService([redis], memory, NullLogger<ResilientCacheService>.Instance);
    }

    private sealed record TestPayload(string Value);

    private sealed class FakeRedisCacheService : IRedisCacheService
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> _sets = new(StringComparer.Ordinal);

        public bool ThrowOnGet { get; set; }
        public bool ThrowOnSet { get; set; }
        public bool ThrowOnRemove { get; set; }
        public bool ThrowOnExists { get; set; }
        public bool ThrowOnSetAdd { get; set; }
        public bool ThrowOnSetRemove { get; set; }
        public bool ThrowOnSetMembers { get; set; }

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            ThrowIfNeeded(ThrowOnGet);
            return Task.FromResult(_values.TryGetValue(key, out var value) ? (T?)value : default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default)
        {
            ThrowIfNeeded(ThrowOnSet);
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
        {
            ThrowIfNeeded(ThrowOnRemove);
            var removed = _values.Remove(key);
            _sets.Remove(key);
            return Task.FromResult(removed);
        }

        public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            ThrowIfNeeded(ThrowOnExists);
            return Task.FromResult(_values.ContainsKey(key) || _sets.ContainsKey(key));
        }

        public Task SetAddAsync(string setKey, string value, TimeSpan? expire = null,
            CancellationToken ct = default)
        {
            ThrowIfNeeded(ThrowOnSetAdd);
            if (!_sets.TryGetValue(setKey, out var set))
            {
                set = [];
                _sets[setKey] = set;
            }

            set.Add(value);
            return Task.CompletedTask;
        }

        public Task SetRemoveAsync(string setKey, string value, CancellationToken ct = default)
        {
            ThrowIfNeeded(ThrowOnSetRemove);
            if (_sets.TryGetValue(setKey, out var set)) set.Remove(value);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> SetMembersAsync(string setKey, CancellationToken ct = default)
        {
            ThrowIfNeeded(ThrowOnSetMembers);
            if (!_sets.TryGetValue(setKey, out var set)) return Task.FromResult<IReadOnlyList<string>>([]);
            return Task.FromResult<IReadOnlyList<string>>(set.ToList());
        }

        private static void ThrowIfNeeded(bool shouldThrow)
        {
            if (shouldThrow) throw new ObjectDisposedException("redis");
        }
    }
}
