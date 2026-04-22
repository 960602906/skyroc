using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Caching;

/// <summary>
///     基于 Castle DynamicProxy + Scrutor 装饰器的方法级缓存注册扩展。
/// </summary>
public static class CachingServiceCollectionExtensions
{
    /// <summary>
    ///     注册方法级缓存所需的基础设施：<see cref="ProxyGenerator" /> 与拦截器。
    /// </summary>
    public static IServiceCollection AddMethodCaching(this IServiceCollection services)
    {
        // ProxyGenerator 必须为单例（内部缓存生成的代理类型）
        services.AddSingleton<ProxyGenerator>();
        services.AddSingleton<CachingAsyncInterceptor>();
        return services;
    }

    /// <summary>
    ///     使用 Castle DynamicProxy 把 <typeparamref name="TInterface" /> 的实现包一层缓存装饰器。
    /// </summary>
    public static IServiceCollection DecorateWithCaching<TInterface>(this IServiceCollection services)
        where TInterface : class
    {
        services.Decorate<TInterface>((inner, sp) =>
        {
            var generator = sp.GetRequiredService<ProxyGenerator>();
            var interceptor = sp.GetRequiredService<CachingAsyncInterceptor>();
            return generator.CreateInterfaceProxyWithTargetInterface(
                inner,
                interceptor.ToInterceptor());
        });
        return services;
    }
}
