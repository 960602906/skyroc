namespace Shared.Common;

/// <summary>
///     标记一个方法的返回值需要被缓存（cache-aside）。
///     只能用在接口方法上（搭配 <c>CachingAsyncInterceptor</c> 装饰器生效），
///     且方法返回类型必须是 <see cref="Task{TResult}" />。
/// </summary>
/// <example>
///     <code>
/// [Cacheable(KeyPrefix = "user:page", Seconds = 300, Buckets = new[] { "user" })]
/// Task&lt;PagedResult&lt;UserDto&gt;&gt; GetPagedAsync(UserQueryParameters p);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class CacheableAttribute : Attribute
{
    /// <summary>
    ///     缓存 Key 前缀。为空时自动使用 <c>类名.方法名</c>。
    ///     实际 Key = <c>{KeyPrefix}:{参数哈希}</c>。
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    ///     过期时间（秒）。默认 300 秒（5 分钟）。
    /// </summary>
    public int Seconds { get; set; } = 300;

    /// <summary>
    ///     所属失效分组（Bucket）。
    ///     缓存写入时会把 Key 登记到对应 bucket；
    ///     <see cref="CacheEvictAttribute" /> 可按 bucket 批量清理。
    /// </summary>
    public string[] Buckets { get; set; } = [];
}
