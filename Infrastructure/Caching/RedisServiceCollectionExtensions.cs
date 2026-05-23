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
    ///     1. 启动时探测 Redis 是否可用
    ///     2. Redis 可用时直接使用 Redis 缓存实现
    ///     3. Redis 不可用或禁用时直接使用内存缓存实现
    /// </summary>
    public static IServiceCollection AddRedisServices(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(RedisOptions.SectionName);
        services.Configure<RedisOptions>(section);
        var options = section.Get<RedisOptions>() ?? new RedisOptions();
        var healthChecks = services.AddHealthChecks();

        services.AddMemoryCache();

        var redisConfigured = options.Enabled && !string.IsNullOrWhiteSpace(options.ConnectionString);
        if (redisConfigured)
        {
            try
            {
                var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
                configOptions.AbortOnConnectFail = false;
                configOptions.DefaultDatabase = options.Database;

                var multiplexer = ConnectionMultiplexer.Connect(configOptions);
                EnsureRedisAvailable(multiplexer);
                services.AddSingleton<IConnectionMultiplexer>(multiplexer);
                services.AddSingleton<IRedisConnectionProbe, RedisConnectionProbe>();
                services.AddSingleton<RedisCacheService>();
                services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<RedisCacheService>());
                services.AddStackExchangeRedisCache(o =>
                {
                    o.Configuration = options.ConnectionString;
                    o.InstanceName = options.InstanceName;
                });
                healthChecks.AddCheck<RedisHealthCheck>("redis",
                    environment.IsDevelopment() ? HealthStatus.Degraded : HealthStatus.Unhealthy);

                Console.Error.WriteLine(
                    "[Redis] Available at startup. Using Redis cache.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[Redis] Initialization failed, using in-memory cache only. {ex.Message}");
                RegisterMemoryCache(services);
            }
        }
        else
        {
            Console.Error.WriteLine(
                "[Redis] Disabled or connection string missing; using in-memory cache only.");
            RegisterMemoryCache(services);
        }

        return services;
    }

    private static void EnsureRedisAvailable(IConnectionMultiplexer multiplexer)
    {
        try
        {
            if (!multiplexer.IsConnected)
                throw new InvalidOperationException("Redis is not connected at startup.");

            multiplexer.GetDatabase().Ping();
        }
        catch
        {
            multiplexer.Dispose();
            throw;
        }
    }

    private static void RegisterMemoryCache(IServiceCollection services)
    {
        services.AddDistributedMemoryCache();
        services.AddSingleton<MemoryCacheService>();
        services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<MemoryCacheService>());
    }
}
