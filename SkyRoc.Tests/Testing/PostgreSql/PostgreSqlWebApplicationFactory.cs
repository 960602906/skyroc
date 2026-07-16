using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shared.Common;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     使用白名单 PostgreSQL 测试库和 Testing 环境启动真实 Web 应用。
/// </summary>
public sealed class PostgreSqlWebApplicationFactory(
    PostgreSqlTestSettings settings,
    IObjectStorage sharedObjectStorage) : WebApplicationFactory<Program>
{
    /// <summary>
    ///     将真实宿主连接、认证密钥、缓存和进程内对象存储固定到测试用途。
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        DatabaseSafetyGuard.Validate(settings);
        builder.UseEnvironment(settings.EnvironmentName);
        builder.UseSetting("ConnectionStrings:DefaultConnection", settings.ConnectionString);
        builder.UseSetting("JwtSettings:SecretKey", "postgresql-test-only-key-with-at-least-32-bytes");
        builder.UseSetting("Redis:Enabled", "false");
        builder.UseSetting("DevSeed:Enabled", "false");
        builder.UseSetting("RustFS:UseInMemory", "true");
        builder.UseSetting("RustFS:BucketName", "skyroc-test");
        builder.ConfigureTestServices(services =>
        {
            var descriptors = services.Where(descriptor => descriptor.ServiceType == typeof(IObjectStorage)).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton(sharedObjectStorage);
        });
    }
}
