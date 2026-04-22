using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using Shared.Common;

namespace Application.Caching;

/// <summary>
///     基于 Castle DynamicProxy 的查询缓存拦截器。
///     <list type="bullet">
///         <item>方法标注 <see cref="CacheableAttribute" /> → 读缓存；未命中则执行并写回。</item>
///         <item>方法标注 <see cref="CacheEvictAttribute" /> → 执行成功后清除指定 Bucket。</item>
///     </list>
///     仅支持返回 <see cref="Task" /> 或 <see cref="Task{TResult}" /> 的异步方法。
/// </summary>
public sealed class CachingAsyncInterceptor(
    ICacheService cache,
    ILogger<CachingAsyncInterceptor> logger) : IAsyncInterceptor
{
    /// <summary>
    ///     Bucket 在缓存中的 Set Key 前缀。
    /// </summary>
    private const string BucketPrefix = "cache:bucket:";

    public void InterceptSynchronous(IInvocation invocation)
    {
        // 同步方法不做缓存，保持行为一致
        invocation.Proceed();
    }

    public void InterceptAsynchronous(IInvocation invocation)
    {
        invocation.ReturnValue = ProceedVoidAsync(invocation);
    }

    public void InterceptAsynchronous<TResult>(IInvocation invocation)
    {
        invocation.ReturnValue = ProceedResultAsync<TResult>(invocation);
    }

    private async Task ProceedVoidAsync(IInvocation invocation)
    {
        invocation.Proceed();
        var task = (Task)invocation.ReturnValue!;
        await task;
        await TryEvictAsync(invocation);
    }

    private async Task<TResult> ProceedResultAsync<TResult>(IInvocation invocation)
    {
        var method = invocation.Method;
        var targetMethod = invocation.MethodInvocationTarget;

        var cacheable = GetAttribute<CacheableAttribute>(method, targetMethod);
        if (cacheable is not null)
        {
            var prefix = string.IsNullOrWhiteSpace(cacheable.KeyPrefix)
                ? $"{method.DeclaringType?.Name}.{method.Name}"
                : cacheable.KeyPrefix!;
            var key = CacheKeyBuilder.Build(prefix, invocation.Arguments);

            var cached = await cache.GetAsync<TResult>(key);
            if (cached is not null)
            {
                logger.LogDebug("[Cache] HIT {Key}", key);
                return cached;
            }

            invocation.Proceed();
            var innerTask = (Task<TResult>)invocation.ReturnValue!;
            var value = await innerTask;

            if (value is not null)
            {
                var ttl = TimeSpan.FromSeconds(Math.Max(1, cacheable.Seconds));
                await cache.SetAsync(key, value, ttl);
                if (cacheable.Buckets.Length > 0)
                {
                    // bucket 比缓存本身多活一会儿，避免边界清理不干净
                    var bucketTtl = ttl.Add(TimeSpan.FromMinutes(5));
                    foreach (var bucket in cacheable.Buckets)
                        await cache.SetAddAsync(BucketPrefix + bucket, key, bucketTtl);
                }

                logger.LogDebug("[Cache] MISS→SET {Key} TTL={Ttl}s", key, (int)ttl.TotalSeconds);
            }

            return value;
        }

        invocation.Proceed();
        var resultTask = (Task<TResult>)invocation.ReturnValue!;
        var result = await resultTask;
        await TryEvictAsync(invocation);
        return result;
    }

    private async Task TryEvictAsync(IInvocation invocation)
    {
        var evicts = GetAttributes<CacheEvictAttribute>(invocation.Method, invocation.MethodInvocationTarget);
        if (evicts.Length == 0) return;

        foreach (var evict in evicts)
        foreach (var bucket in evict.Buckets)
        {
            if (string.IsNullOrWhiteSpace(bucket)) continue;
            var setKey = BucketPrefix + bucket;
            var keys = await cache.SetMembersAsync(setKey);
            foreach (var k in keys) await cache.RemoveAsync(k);
            await cache.RemoveAsync(setKey);
            logger.LogDebug("[Cache] EVICT bucket={Bucket} count={Count}", bucket, keys.Count);
        }
    }

    private static T? GetAttribute<T>(MethodInfo interfaceMethod, MethodInfo? targetMethod) where T : Attribute
    {
        return interfaceMethod.GetCustomAttribute<T>(true)
               ?? targetMethod?.GetCustomAttribute<T>(true);
    }

    private static T[] GetAttributes<T>(MethodInfo interfaceMethod, MethodInfo? targetMethod) where T : Attribute
    {
        var onInterface = interfaceMethod.GetCustomAttributes<T>(true).ToArray();
        if (onInterface.Length > 0) return onInterface;
        return targetMethod?.GetCustomAttributes<T>(true).ToArray() ?? [];
    }
}
