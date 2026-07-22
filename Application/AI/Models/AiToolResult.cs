namespace Application.AI.Models;

/// <summary>表示白名单工具执行后回传给模型的受限结果。</summary>
public sealed class AiToolResult
{
    /// <summary>对应 <see cref="AiToolCall.Id"/> 的调用编号。</summary>
    public string ToolCallId { get; init; } = string.Empty;
    /// <summary>实际执行的工具稳定名称。</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>经过字段裁剪和脱敏的内容，不含完整敏感业务响应。</summary>
    public string Content { get; init; } = string.Empty;
    /// <summary>指示结果是否为可反馈给模型修正参数的受控错误。</summary>
    public bool IsError { get; init; }
}
