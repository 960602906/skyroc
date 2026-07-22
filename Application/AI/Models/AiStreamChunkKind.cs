namespace Application.AI.Models;

/// <summary>流式响应分片类型，统一不同厂商的文字、工具和结束事件。</summary>
public enum AiStreamChunkKind
{
    /// <summary>可展示的回答文字增量。</summary>
    ContentDelta = 0,
    /// <summary>尚待编排层聚合的工具调用增量。</summary>
    ToolCallDelta = 1,
    /// <summary>本次模型调用的 token 用量。</summary>
    Usage = 2,
    /// <summary>模型调用正常结束及其结束原因。</summary>
    Completed = 3,
    /// <summary>已规范化且不含敏感原始响应的错误。</summary>
    Error = 4
}
