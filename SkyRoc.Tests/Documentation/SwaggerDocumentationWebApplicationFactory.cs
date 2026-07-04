using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
        builder.UseSetting("Redis:Enabled", "false");
    }
}
