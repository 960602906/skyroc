using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Shared.Common;
using StackExchange.Redis;

namespace Infrastructure.Caching;

public static class RedisServiceCollectionExtensions
{
    /// <summary>
    ///     注册缓存相关服务：
    ///     1. 始终注册内存缓存与 <see cref="ResilientCacheService" />
    ///     2. Redis 启用时注册 <see cref="IConnectionMultiplexer" /> + <see cref="IRedisCacheService" />
    ///     3. Redis 运行时故障时由 <see cref="ResilientCacheService" /> 自动降级到内存
    ///     4. Redis 禁用或配置无效时仅使用内存缓存
    /// </summary>
    public static IServiceCollection AddRedisServices(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(RedisOptions.SectionName);
        services.Configure<RedisOptions>(section);
        var options = section.Get<RedisOptions>() ?? new RedisOptions();
        var healthChecks = services.AddHealthChecks();

        services.AddMemoryCache();
        services.AddSingleton<MemoryCacheService>();
        services.AddSingleton<ICacheService, ResilientCacheService>();

        var redisConfigured = options.Enabled && !string.IsNullOrWhiteSpace(options.ConnectionString);
        if (redisConfigured)
        {
            try
            {
                var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
                configOptions.AbortOnConnectFail = false;
                configOptions.DefaultDatabase = options.Database;

                var multiplexer = ConnectionMultiplexer.Connect(configOptions);
                services.AddSingleton<IConnectionMultiplexer>(multiplexer);
                services.AddSingleton<IRedisConnectionProbe, RedisConnectionProbe>();
                services.AddSingleton<IRedisCacheService, RedisCacheService>();
                services.AddStackExchangeRedisCache(o =>
                {
                    o.Configuration = options.ConnectionString;
                    o.InstanceName = options.InstanceName;
                });
                healthChecks.AddCheck<RedisHealthCheck>("redis",
                    environment.IsDevelopment() ? HealthStatus.Degraded : HealthStatus.Unhealthy);

                Console.Error.WriteLine(
                    "[Redis] Enabled with runtime fallback. Redis failures will fall back to in-memory cache.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[Redis] Initialization failed, using in-memory cache only. {ex.Message}");
                services.AddDistributedMemoryCache();
            }
        }
        else
        {
            Console.Error.WriteLine(
                "[Redis] Disabled or connection string missing; using in-memory cache only.");
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
