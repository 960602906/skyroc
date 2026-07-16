using System.Collections.Concurrent;
using Shared.Common;

namespace Infrastructure.Storage;

/// <summary>
/// 进程内对象存储，用于单元测试与 PostgreSQL 联调，不依赖真实 RustFS。
/// </summary>
public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly ConcurrentDictionary<string, StoredObject> _objects = new(StringComparer.Ordinal);

    /// <summary>
    /// 当前已存储对象数量，供测试断言补偿删除等场景。
    /// </summary>
    public int Count => _objects.Count;

    /// <inheritdoc />
    public Task PutAsync(string key, Stream content, string contentType, long length, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);
        using var copy = new MemoryStream();
        content.CopyTo(copy);
        var bytes = copy.ToArray();
        if (bytes.LongLength != length)
            throw new InvalidOperationException("对象内容长度与声明长度不一致");

        _objects[key] = new StoredObject(bytes, contentType);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_objects.TryGetValue(key, out var stored))
            throw new FileNotFoundException("对象不存在", key);

        return Task.FromResult<Stream>(new MemoryStream(stored.Content, writable: false));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _objects.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Task.FromResult(_objects.ContainsKey(key));
    }

    private sealed record StoredObject(byte[] Content, string ContentType);
}
