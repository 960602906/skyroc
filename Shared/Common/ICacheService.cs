namespace Shared.Common;

/// <summary>
///     通用缓存服务抽象（启动时根据 Redis 可用性选择实现）
/// </summary>
public interface ICacheService
{
    /// <summary>
    ///     读取对象（内部 JSON 反序列化）
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    ///     写入对象（可选过期时间）
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expire = null, CancellationToken ct = default);

    /// <summary>
    ///     删除一个 key
    /// </summary>
    Task<bool> RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    ///     key 是否存在
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    ///     向集合添加成员
    /// </summary>
    Task SetAddAsync(string setKey, string value, TimeSpan? expire = null, CancellationToken ct = default);

    /// <summary>
    ///     从集合移除成员
    /// </summary>
    Task SetRemoveAsync(string setKey, string value, CancellationToken ct = default);

    /// <summary>
    ///     获取集合全部成员
    /// </summary>
    Task<IReadOnlyList<string>> SetMembersAsync(string setKey, CancellationToken ct = default);
}
