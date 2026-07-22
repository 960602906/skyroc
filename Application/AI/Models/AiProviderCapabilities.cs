namespace Application.AI.Models;

/// <summary>描述适配器已经过契约测试的能力，不代表当前用户拥有工具权限。</summary>
public sealed class AiProviderCapabilities
{
    /// <summary>是否支持逐增量返回最终回答。</summary>
    public bool SupportsStreaming { get; init; }
    /// <summary>是否支持结构化工具调用。</summary>
    public bool SupportsTools { get; init; }
    /// <summary>是否支持在同一轮请求多个工具。</summary>
    public bool SupportsParallelToolCalls { get; init; }
    /// <summary>是否能返回输入和输出 token 用量。</summary>
    public bool ReportsUsage { get; init; }
    /// <summary>已知的最大上下文 token 数；未知时为空。</summary>
    public int? MaxContextTokens { get; init; }
}
