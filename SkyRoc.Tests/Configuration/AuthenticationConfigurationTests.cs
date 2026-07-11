using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkyRoc.Extensions;
using Xunit;

namespace SkyRoc.Tests.Configuration;

/// <summary>
///     验证认证启动配置会拒绝不安全的 JWT 签名参数。
/// </summary>
public class AuthenticationConfigurationTests
{
    /// <summary>
    ///     完全缺失 JWT 配置时应返回可识别的启动配置异常。
    /// </summary>
    [Fact]
    public void AddAuthenticationServices_RejectsMissingJwtSettings()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddAuthenticationServices(configuration));

        Assert.Contains("JwtSettings", exception.Message);
    }

    /// <summary>
    ///     UTF-8 字节数不足 32 的签名密钥必须在应用启动时被拒绝。
    /// </summary>
    [Fact]
    public void AddAuthenticationServices_RejectsShortJwtSigningKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "short-secret",
                ["JwtSettings:Issuer"] = "skyroc-tests",
                ["JwtSettings:Audience"] = "skyroc-tests"
            })
            .Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddAuthenticationServices(configuration));

        Assert.Contains("at least 32 UTF-8 bytes", exception.Message);
    }
}
