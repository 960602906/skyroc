using Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Shared.Common;
using Xunit;

namespace SkyRoc.Tests.Caching;

public class RedisServiceCollectionExtensionsTests
{
    [Fact]
    public void Disabled_redis_should_register_memory_cache_service()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Redis:Enabled"] = "false"
        });

        services.AddRedisServices(configuration, new FakeHostEnvironment());

        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
        var memoryCache = provider.GetRequiredService<MemoryCacheService>();

        Assert.IsType<MemoryCacheService>(cache);
        Assert.Same(memoryCache, cache);
        Assert.Null(provider.GetService<RedisCacheService>());
    }

    [Fact]
    public void Unreachable_redis_should_fall_back_to_memory_cache_at_startup()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Redis:Enabled"] = "true",
            ["Redis:ConnectionString"] = "localhost:6399,abortConnect=false,connectTimeout=100,syncTimeout=100,connectRetry=0"
        });

        services.AddRedisServices(configuration, new FakeHostEnvironment());

        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
        var memoryCache = provider.GetRequiredService<MemoryCacheService>();

        Assert.IsType<MemoryCacheService>(cache);
        Assert.Same(memoryCache, cache);
        Assert.Null(provider.GetService<RedisCacheService>());
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "SkyRoc.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(AppContext.BaseDirectory);
    }
}
