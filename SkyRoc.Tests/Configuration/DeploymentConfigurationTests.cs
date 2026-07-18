using System.Text.Json;
using Xunit;

namespace SkyRoc.Tests.Configuration;

/// <summary>
///     验证仓库内默认部署配置不会启用开发账号或携带认证凭据。
/// </summary>
public class DeploymentConfigurationTests
{
    /// <summary>
    ///     默认配置不得携带 JWT 签名密钥或开发种子密码。
    /// </summary>
    [Fact]
    public void AppSettings_DoesNotContainAuthenticationOrSeedCredentials()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetRepositoryFile("SkyRoc", "appsettings.json")));
        var root = document.RootElement;

        Assert.Empty(root.GetProperty("JwtSettings").GetProperty("SecretKey").GetString() ?? string.Empty);
        Assert.Empty(root.GetProperty("DevSeed").GetProperty("AdminPassword").GetString() ?? string.Empty);
        Assert.False(root.GetProperty("DevSeed").GetProperty("Enabled").GetBoolean());
    }

    /// <summary>
    ///     受版本控制的开发环境配置也不能默认开启开发账号初始化。
    /// </summary>
    [Fact]
    public void DevelopmentAppSettings_DoesNotEnableDevelopmentSeedByDefault()
    {
        using var document = JsonDocument.Parse(
            File.ReadAllText(GetRepositoryFile("SkyRoc", "appsettings.Development.json")));

        Assert.False(document.RootElement.GetProperty("DevSeed").GetProperty("Enabled").GetBoolean());
    }

    /// <summary>
    ///     EF Core 设计时工厂不得内嵌远端连接、账号或密码，迁移连接必须由环境变量显式提供。
    /// </summary>
    [Fact]
    public void DbContextFactory_RequiresEnvironmentConnectionWithoutEmbeddedCredentials()
    {
        var source = File.ReadAllText(
            GetRepositoryFile("Infrastructure", "Data", "DbContextFactory.cs"));

        Assert.Contains("ConnectionStrings__DefaultConnection", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("neon.tech", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRepositoryFile(params string[] pathSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SkyRoc.sln")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        return Path.Combine([directory.FullName, .. pathSegments]);
    }
}
