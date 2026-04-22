namespace Shared.Common;

/// <summary>
///     标记一个方法执行成功后，需要清除指定 Bucket 下全部缓存。
///     常用于 Create / Update / Delete 等写入操作。
/// </summary>
/// <example>
///     <code>
/// [CacheEvict("user")]
/// Task&lt;UserDto&gt; CreateUserAsync(CreateUserDto request);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class CacheEvictAttribute(params string[] buckets) : Attribute
{
    /// <summary>
    ///     要清理的失效分组。
    /// </summary>
    public string[] Buckets { get; } = buckets ?? [];
}
