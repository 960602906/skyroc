namespace Application.AI.Models;

/// <summary>表示模型流中的规范化分片；仅承载与分片类型对应的数据。</summary>
public sealed class AiStreamChunk
{
    /// <summary>分片类型。</summary>
    public AiStreamChunkKind Kind { get; init; }
    /// <summary>可直接追加到最终回答的文字增量。</summary>
    public string? ContentDelta { get; init; }
    /// <summary>工具调用编号、名称或参数增量，由编排层跨分片聚合。</summary>
    public AiToolCall? ToolCallDelta { get; init; }
    /// <summary>本次调用的规范化 token 用量。</summary>
    public AiUsage? Usage { get; init; }
    /// <summary>完成分片携带的统一结束原因。</summary>
    public AiFinishReason FinishReason { get; init; }
    /// <summary>错误分片携带的安全错误，不含密钥、请求头或原始响应。</summary>
    public AiProviderError? Error { get; init; }
}
