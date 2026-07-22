namespace Application.AI.Abstractions;

/// <summary>按应用配置选择唯一活动模型适配器，隔离业务编排与厂商注册细节。</summary>
public interface IAiModelProviderRegistry
{
    /// <summary>获取配置中的活动 Provider 名称，对应 <c>Ai:Providers</c> 的键。</summary>
    string ActiveProviderName { get; }

    /// <summary>返回当前活动 Provider 对应的唯一已注册适配器。</summary>
    /// <returns>供进程内 AI 编排使用的模型适配器。</returns>
    /// <exception cref="InvalidOperationException">AI 未启用、Provider 不存在或适配器未注册时抛出。</exception>
    IAiModelProvider GetActiveProvider();
}
