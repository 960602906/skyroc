namespace Application.AI.Models;

/// <summary>表示模型请求中的规范化消息，不保存或暴露厂商推理内容。</summary>
public sealed class AiChatMessage
{
    /// <summary>消息角色，决定内容在模型上下文中的语义。</summary>
    public AiChatRole Role { get; init; }
    /// <summary>仅包含最终可展示文字或经过脱敏的工具结果。</summary>
    public string? Content { get; init; }
    /// <summary>工具结果所响应的调用编号；非工具消息为空。</summary>
    public string? ToolCallId { get; init; }
    /// <summary>助手消息中已完成聚合的工具调用。</summary>
    public IReadOnlyList<AiToolCall> ToolCalls { get; init; } = [];
}
