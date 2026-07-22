using Application.AI.Abstractions;
using Microsoft.Extensions.Options;
using Shared.Options;

namespace Application.AI;

/// <summary>从活动 Provider 解析唯一适配器，并拒绝重复 Adapter 注册。</summary>
public sealed class AiModelProviderRegistry : IAiModelProviderRegistry
{
    private readonly AiOptions _options;
    private readonly IReadOnlyDictionary<string, IAiModelProvider> _providersByAdapter;

    /// <summary>创建模型 Provider 注册表。</summary>
    /// <param name="options">AI 开关、活动 Provider 和 Adapter 映射。</param>
    /// <param name="providers">当前进程已注册的模型适配器。</param>
    /// <exception cref="InvalidOperationException">适配器名称为空或重复注册时抛出。</exception>
    public AiModelProviderRegistry(
        IOptions<AiOptions> options,
        IEnumerable<IAiModelProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(providers);
        _options = options.Value;

        var providerList = providers.ToArray();
        if (providerList.Any(provider => string.IsNullOrWhiteSpace(provider.AdapterName)))
            throw new InvalidOperationException("IAiModelProvider.AdapterName 不能为空。");

        var duplicateAdapter = providerList
            .GroupBy(provider => provider.AdapterName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateAdapter is not null)
            throw new InvalidOperationException($"AI Adapter '{duplicateAdapter.Key}' 被重复注册。");

        _providersByAdapter = providerList.ToDictionary(
            provider => provider.AdapterName,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string ActiveProviderName => _options.ActiveProvider;

    /// <inheritdoc />
    public IAiModelProvider GetActiveProvider()
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("AI 功能未启用，不能解析活动 Provider。");

        var activeProvider = _options.Providers.FirstOrDefault(pair =>
            string.Equals(pair.Key, _options.ActiveProvider, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(activeProvider.Key) || activeProvider.Value is null)
            throw new InvalidOperationException($"AI Provider '{_options.ActiveProvider}' 未配置。");

        if (!_providersByAdapter.TryGetValue(activeProvider.Value.Adapter, out var provider))
            throw new InvalidOperationException(
                $"AI Provider '{activeProvider.Key}' 指定的 Adapter '{activeProvider.Value.Adapter}' 未注册。");

        return provider;
    }
}
