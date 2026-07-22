namespace Application.AI.Models;

/// <summary>表示一次模型调用的 token 用量；厂商未返回的值保持为零。</summary>
public sealed class AiUsage
{
    /// <summary>输入消息和工具定义消耗的 token 数。</summary>
    public int InputTokens { get; init; }
    /// <summary>最终文字及工具调用消耗的输出 token 数。</summary>
    public int OutputTokens { get; init; }
    /// <summary>输入与输出 token 总数。</summary>
    public int TotalTokens { get; init; }
}
