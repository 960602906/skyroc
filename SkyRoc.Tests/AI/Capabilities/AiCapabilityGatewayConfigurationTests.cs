using Application;
using Application.AI.Capabilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.Options;
using Xunit;

namespace SkyRoc.Tests.AI.Capabilities;

/// <summary>验证能力网关配置边界、配置文件凭据和禁用时的延迟构造。</summary>
public class AiCapabilityGatewayConfigurationTests
{
    [Theory]
    [InlineData("Ai:CapabilityGateway:SearchLimit", "0", "SearchLimit")]
    [InlineData("Ai:CapabilityGateway:SearchLimit", "21", "SearchLimit")]
    [InlineData("Ai:CapabilityGateway:MaxToolResultBytes", "1023", "MaxToolResultBytes")]
    [InlineData("Ai:CapabilityGateway:MaxToolResultBytes", "1048577", "MaxToolResultBytes")]
    [InlineData("Ai:CapabilityGateway:DelegationTokenLifetimeSeconds", "0", "DelegationTokenLifetimeSeconds")]
    [InlineData("Ai:CapabilityGateway:DelegationTokenLifetimeSeconds", "61", "DelegationTokenLifetimeSeconds")]
    [InlineData("Ai:CapabilityGateway:HighRiskConfirmationLifetimeSeconds", "0", "HighRiskConfirmationLifetimeSeconds")]
    [InlineData("Ai:CapabilityGateway:HighRiskConfirmationLifetimeSeconds", "301", "HighRiskConfirmationLifetimeSeconds")]
    public void Options_RejectOutOfRangeGatewayValues(string key, string value, string expectedPath)
    {
        var settings = DisabledSettings();
        settings[key] = value;
        using var serviceProvider = BuildServiceProvider(settings);

        var exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<AiOptions>>().Value);

        Assert.Contains(expectedPath, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://localhost")]
    [InlineData("http://user:password@localhost")]
    [InlineData("http://localhost/api")]
    [InlineData("http://localhost?target=other")]
    public void Options_RejectInvalidInternalBaseUrl(string internalBaseUrl)
    {
        var settings = EnabledSettings();
        settings["Ai:CapabilityGateway:InternalBaseUrl"] = internalBaseUrl;
        using var serviceProvider = BuildServiceProvider(settings);

        var exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<AiOptions>>().Value);

        Assert.Contains("Ai:CapabilityGateway:InternalBaseUrl", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Options_AcceptProviderApiKeyFromConfiguration()
    {
        using var serviceProvider = BuildServiceProvider(EnabledSettings());

        var options = serviceProvider.GetRequiredService<IOptions<AiOptions>>().Value;

        Assert.Equal("configuration-test-key", options.Providers["Test"].ApiKey);
    }

    [Fact]
    public async Task HostedValidation_DoesNotConstructGatewayExecutor_WhenAiIsDisabled()
    {
        var constructionCount = 0;
        using var serviceProvider = BuildServiceProvider(DisabledSettings(), services =>
            services.AddSingleton<IAiApiOperationExecutor>(_ =>
            {
                constructionCount++;
                return new FakeExecutor();
            }));

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);

        Assert.Equal(0, constructionCount);
    }

    private static Dictionary<string, string?> DisabledSettings() => new()
    {
        ["Ai:Enabled"] = "false",
        ["Mcp:ExternalEnabled"] = "false"
    };

    private static Dictionary<string, string?> EnabledSettings() => new()
    {
        ["Ai:Enabled"] = "true",
        ["Ai:ActiveProvider"] = "Test",
        ["Ai:Providers:Test:Adapter"] = "TestAdapter",
        ["Ai:Providers:Test:BaseUrl"] = "https://ai.example.test/v1",
        ["Ai:Providers:Test:Model"] = "test-model",
        ["Ai:Providers:Test:ApiKey"] = "configuration-test-key",
        ["Ai:CapabilityGateway:InternalBaseUrl"] = "http://localhost:5293",
        ["Mcp:ExternalEnabled"] = "false"
    };

    private static ServiceProvider BuildServiceProvider(
        IDictionary<string, string?> settings,
        Action<IServiceCollection>? configureServices = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        services.AddApplicationServices(configuration);
        return services.BuildServiceProvider();
    }

    private sealed class FakeExecutor : IAiApiOperationExecutor
    {
        public Task<AiApiOperationResult> ExecuteAsync(
            AiApiOperationRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiApiOperationResult { Code = 200 });
    }
}
