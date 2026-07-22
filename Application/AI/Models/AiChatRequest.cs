namespace Application.AI.Models;

/// <summary>表示发送给活动模型 Provider 的统一对话请求。</summary>
public sealed class AiChatRequest
{
    /// <summary>按对话顺序排列的系统、用户、助手和工具消息。</summary>
    public IReadOnlyList<AiChatMessage> Messages { get; init; } = [];
    /// <summary>已按当前用户权限过滤的可调用工具定义。</summary>
    public IReadOnlyList<AiToolDefinition> Tools { get; init; } = [];
    /// <summary>本次调用允许生成的最大 token 数，不得超过全局配置。</summary>
    public int MaxOutputTokens { get; init; }
    /// <summary>采样温度；为空时使用活动 Provider 的安全默认值。</summary>
    public decimal? Temperature { get; init; }
}
