using Application.AI.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.Options;

namespace Application.AI.Configuration;

/// <summary>在宿主接收请求前强制解析配置与活动模型适配器。</summary>
internal sealed class AiConfigurationStartupService(
    IOptions<AiOptions> aiOptions,
    IOptions<McpOptions> mcpOptions,
    IServiceProvider serviceProvider) : IHostedService
{
    /// <summary>触发 Options 校验；AI 启用时同时验证活动适配器专属配置。</summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var currentAiOptions = aiOptions.Value;
        _ = mcpOptions.Value;
        if (currentAiOptions.Enabled)
        {
            var registry = serviceProvider.GetRequiredService<IAiModelProviderRegistry>();
            registry.GetActiveProvider().ValidateConfiguration();
        }

        return Task.CompletedTask;
    }

    /// <summary>配置校验服务不持有运行时资源，停止时无需清理。</summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
