using Application;
using Application.AI.Abstractions;
using Application.AI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.Options;
using Xunit;

namespace SkyRoc.Tests.AI;

/// <summary>验证 AI/MCP 配置开关、启动校验、DI 和活动 Provider 选择边界。</summary>
public class AiConfigurationAndRegistryTests
{
    [Fact]
    public async Task StartupValidation_SucceedsWithoutSecretsOrAdapters_WhenAiAndExternalMcpAreDisabled()
    {
        var providerResolutionCount = 0;
        using var serviceProvider = BuildServiceProvider(
            new Dictionary<string, string?>
            {
                ["Ai:Enabled"] = "false",
                ["Ai:Providers:Disabled:ApiKeyEnvironmentVariable"] = "SKYROC_TEST_MISSING_AI_KEY",
                ["Mcp:ExternalEnabled"] = "false",
                ["Mcp:TokenHashKeyEnvironmentVariable"] = "SKYROC_TEST_MISSING_MCP_KEY"
            },
            services => services.AddSingleton<IAiModelProvider>(_ =>
            {
                providerResolutionCount++;
                return new FakeProvider("MustNotResolve");
            }));

        await StartApplicationHostedServicesAsync(serviceProvider);

        Assert.False(serviceProvider.GetRequiredService<IOptions<AiOptions>>().Value.Enabled);
        Assert.False(serviceProvider.GetRequiredService<IOptions<McpOptions>>().Value.ExternalEnabled);
        Assert.Equal(0, providerResolutionCount);
    }

    [Theory]
    [InlineData("Ai:Providers:Test:BaseUrl", "", "BaseUrl")]
    [InlineData("Ai:Providers:Test:Model", "", "Model")]
    [InlineData("Ai:Providers:Test:ApiKeyEnvironmentVariable", "", "ApiKeyEnvironmentVariable")]
    public async Task StartupValidation_FailsWithRecognizablePath_WhenActiveProviderSettingIsMissing(
        string settingKey,
        string settingValue,
        string expectedPath)
    {
        var environmentVariableName = UniqueEnvironmentVariableName();
        Environment.SetEnvironmentVariable(environmentVariableName, "test-secret");
        try
        {
            var settings = CreateEnabledSettings(environmentVariableName);
            settings[settingKey] = settingValue;
            using var serviceProvider = BuildServiceProvider(settings, new FakeProvider("TestAdapter"));

            var exception = await Assert.ThrowsAsync<OptionsValidationException>(
                () => StartApplicationHostedServicesAsync(serviceProvider));

            Assert.Contains(expectedPath, exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, null);
        }
    }

    [Fact]
    public async Task StartupValidation_Fails_WhenApiKeyEnvironmentVariableHasNoValue()
    {
        var settings = CreateEnabledSettings(UniqueEnvironmentVariableName());
        using var serviceProvider = BuildServiceProvider(settings, new FakeProvider("TestAdapter"));

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => StartApplicationHostedServicesAsync(serviceProvider));

        Assert.Contains("ApiKeyEnvironmentVariable", exception.Message, StringComparison.Ordinal);
        Assert.Contains("未配置", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Ai:MaxInputCharacters")]
    [InlineData("Ai:MaxOutputTokens")]
    [InlineData("Ai:MaxToolIterations")]
    [InlineData("Ai:RequestTimeoutSeconds")]
    [InlineData("Ai:ConversationRetentionDays")]
    [InlineData("Ai:DraftExpiryMinutes")]
    public async Task StartupValidation_Fails_WhenNumericBoundaryIsZero(string settingKey)
    {
        using var serviceProvider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Ai:Enabled"] = "false",
            [settingKey] = "0"
        });

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => StartApplicationHostedServicesAsync(serviceProvider));

        Assert.Contains(settingKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartupValidation_Fails_WhenActiveProviderNameDoesNotExist()
    {
        var environmentVariableName = UniqueEnvironmentVariableName();
        Environment.SetEnvironmentVariable(environmentVariableName, "test-secret");
        try
        {
            var settings = CreateEnabledSettings(environmentVariableName);
            settings["Ai:ActiveProvider"] = "Unknown";
            using var serviceProvider = BuildServiceProvider(settings, new FakeProvider("TestAdapter"));

            var exception = await Assert.ThrowsAsync<OptionsValidationException>(
                () => StartApplicationHostedServicesAsync(serviceProvider));

            Assert.Contains("Ai:ActiveProvider", exception.Message, StringComparison.Ordinal);
            Assert.Contains("Unknown", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, null);
        }
    }

    [Fact]
    public async Task StartupValidation_Fails_WhenConfiguredAdapterIsNotRegistered()
    {
        var environmentVariableName = UniqueEnvironmentVariableName();
        Environment.SetEnvironmentVariable(environmentVariableName, "test-secret");
        try
        {
            using var serviceProvider = BuildServiceProvider(CreateEnabledSettings(environmentVariableName));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => StartApplicationHostedServicesAsync(serviceProvider));

            Assert.Contains("TestAdapter", exception.Message, StringComparison.Ordinal);
            Assert.Contains("未注册", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, null);
        }
    }

    [Fact]
    public async Task StartupValidation_Fails_WhenAdapterIsRegisteredMoreThanOnce()
    {
        var environmentVariableName = UniqueEnvironmentVariableName();
        Environment.SetEnvironmentVariable(environmentVariableName, "test-secret");
        try
        {
            using var serviceProvider = BuildServiceProvider(
                CreateEnabledSettings(environmentVariableName),
                new FakeProvider("Duplicate"),
                new FakeProvider("duplicate"));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => StartApplicationHostedServicesAsync(serviceProvider));

            Assert.Contains("重复注册", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, null);
        }
    }

    [Fact]
    public async Task Registry_SelectsOnlyActiveProvider_AndValidatesItsConfiguration()
    {
        var environmentVariableName = UniqueEnvironmentVariableName();
        Environment.SetEnvironmentVariable(environmentVariableName, "test-secret");
        try
        {
            var activeProvider = new FakeProvider("TestAdapter");
            var inactiveProvider = new FakeProvider("OtherAdapter");
            using var serviceProvider = BuildServiceProvider(
                CreateEnabledSettings(environmentVariableName),
                inactiveProvider,
                activeProvider);

            await StartApplicationHostedServicesAsync(serviceProvider);
            var registry = serviceProvider.GetRequiredService<IAiModelProviderRegistry>();

            Assert.Equal("Test", registry.ActiveProviderName);
            Assert.Same(activeProvider, registry.GetActiveProvider());
            Assert.Equal(1, activeProvider.ValidationCount);
            Assert.Equal(0, inactiveProvider.ValidationCount);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, null);
        }
    }

    [Fact]
    public async Task StartupValidation_Fails_WhenExternalMcpHashKeyIsMissing()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Ai:Enabled"] = "false",
            ["Mcp:ExternalEnabled"] = "true",
            ["Mcp:TokenHashKeyEnvironmentVariable"] = UniqueEnvironmentVariableName()
        };
        using var serviceProvider = BuildServiceProvider(settings);

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => StartApplicationHostedServicesAsync(serviceProvider));

        Assert.Contains("Mcp:TokenHashKeyEnvironmentVariable", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartupValidation_Fails_WhenExternalMcpDelegationSigningKeyIsMissing()
    {
        var hashKeyEnvironmentVariable = UniqueEnvironmentVariableName();
        Environment.SetEnvironmentVariable(hashKeyEnvironmentVariable, new string('h', 32));
        try
        {
            var settings = new Dictionary<string, string?>
            {
                ["Ai:Enabled"] = "false",
                ["Mcp:ExternalEnabled"] = "true",
                ["Mcp:TokenHashKeyEnvironmentVariable"] = hashKeyEnvironmentVariable,
                ["Mcp:DelegationSigningKeyEnvironmentVariable"] = UniqueEnvironmentVariableName()
            };
            using var serviceProvider = BuildServiceProvider(settings);

            var exception = await Assert.ThrowsAsync<OptionsValidationException>(
                () => StartApplicationHostedServicesAsync(serviceProvider));

            Assert.Contains(
                "Mcp:DelegationSigningKeyEnvironmentVariable",
                exception.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(hashKeyEnvironmentVariable, null);
        }
    }

    private static Dictionary<string, string?> CreateEnabledSettings(string environmentVariableName) => new()
    {
        ["Ai:Enabled"] = "true",
        ["Ai:ActiveProvider"] = "Test",
        ["Ai:Providers:Test:Adapter"] = "TestAdapter",
        ["Ai:Providers:Test:BaseUrl"] = "https://ai.example.test/v1",
        ["Ai:Providers:Test:Model"] = "test-model",
        ["Ai:Providers:Test:ApiKeyEnvironmentVariable"] = environmentVariableName,
        ["Ai:CapabilityGateway:InternalBaseUrl"] = "http://localhost:5293",
        ["Mcp:ExternalEnabled"] = "false"
    };

    private static ServiceProvider BuildServiceProvider(
        IDictionary<string, string?> settings,
        params IAiModelProvider[] providers)
    {
        return BuildServiceProvider(settings, services =>
        {
            foreach (var provider in providers)
                services.AddSingleton(provider);
        });
    }

    private static ServiceProvider BuildServiceProvider(
        IDictionary<string, string?> settings,
        Action<IServiceCollection> configureServices)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection();
        configureServices(services);
        services.AddApplicationServices(configuration);
        return services.BuildServiceProvider();
    }

    private static async Task StartApplicationHostedServicesAsync(IServiceProvider serviceProvider)
    {
        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);
    }

    private static string UniqueEnvironmentVariableName() =>
        $"SKYROC_TEST_AI_{Guid.NewGuid():N}".ToUpperInvariant();

    private sealed class FakeProvider(string adapterName) : IAiModelProvider
    {
        public string AdapterName { get; } = adapterName;
        public int ValidationCount { get; private set; }

        public IAsyncEnumerable<AiStreamChunk> StreamChatAsync(
            AiChatRequest request,
            CancellationToken cancellationToken) => EmptyChunks();

        public ValueTask<AiProviderCapabilities> GetCapabilitiesAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiProviderCapabilities());

        public AiProviderError NormalizeError(Exception exception) => new()
        {
            Code = "test",
            Message = exception.Message
        };

        public void ValidateConfiguration() => ValidationCount++;

        private static async IAsyncEnumerable<AiStreamChunk> EmptyChunks()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
