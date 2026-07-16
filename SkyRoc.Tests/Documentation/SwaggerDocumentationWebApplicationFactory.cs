using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using SkyRoc.Tests.Testing;

namespace SkyRoc.Tests.Documentation;

/// <summary>
///     供 Swagger 文档测试使用的最小 Web 宿主。
/// </summary>
internal sealed class SwaggerDocumentationWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=localhost;Database=skyroc_tests;Username=test;Password=test");
        builder.UseSetting("JwtSettings:SecretKey", "test-only-secret-key-with-at-least-32-bytes");
        builder.UseSetting("Redis:Enabled", "false");
        builder.UseSetting("RustFS:UseInMemory", "true");
        builder.ConfigureTestServices(services =>
            services.UseIsolatedInMemoryPersistence($"swagger-documentation-{Guid.NewGuid():N}"));
    }
}
