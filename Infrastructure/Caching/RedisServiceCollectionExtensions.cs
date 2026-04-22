using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common;
using StackExchange.Redis;

namespace Infrastructure.Caching;

public static class RedisServiceCollectionExtensions
{
    /// <summary>
    ///     注册 Redis 相关服务：
    ///     1. 绑定 <see cref="RedisOptions" />
    ///     2. 注册 <see cref="IConnectionMultiplexer" /> + <see cref="ICacheService" />（Redis 版）
    ///     3. 注册 <c>IDistributedCache</c>
    ///     4. 注册健康检查
    ///     5. 启动阶段连接失败 / <c>Enabled=false</c> → 自动降级为内存实现
    /// </summary>
    public static IServiceCollection AddRedisServices(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(RedisOptions.SectionName);
        services.Configure<RedisOptions>(section);
        var options = section.Get<RedisOptions>() ?? new RedisOptions();

        services.AddMemoryCache();

        var useRedis = options.Enabled && !string.IsNullOrWhiteSpace(options.ConnectionString);
        IConnectionMultiplexer? multiplexer = null;

        if (useRedis)
            try
            {
                var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
                configOptions.AbortOnConnectFail = false;
                configOptions.DefaultDatabase = options.Database;
                multiplexer = ConnectionMultiplexer.Connect(configOptions);
            }
            catch (Exception ex)
            {
                multiplexer = null;
                useRedis = false;
                Console.Error.WriteLine(
                    $"[Redis] Initial connection failed, fallback to in-memory cache. {ex.Message}");
            }

        if (useRedis && multiplexer is not null)
        {
            services.AddSingleton(multiplexer);
            services.AddSingleton<ICacheService, RedisCacheService>();
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = options.ConnectionString;
                o.InstanceName = options.InstanceName;
            });
            services.AddHealthChecks()
                .AddCheck<RedisHealthCheck>("redis");
        }
        else
        {
            Console.Error.WriteLine(
                "[Redis] Disabled or unreachable; using in-memory cache. Set Redis:Enabled=true and a valid ConnectionString to enable Redis.");
            services.AddSingleton<ICacheService, MemoryCacheService>();
            services.AddDistributedMemoryCache();
            services.AddHealthChecks();
        }

        return services;
    }
}
