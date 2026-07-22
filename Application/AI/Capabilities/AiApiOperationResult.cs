using System.Text.Json;

namespace Application.AI.Capabilities;

/// <summary>现有业务 API 经统一响应解析和安全投影后的厂商无关结果。</summary>
public sealed class AiApiOperationResult
{
    /// <summary>业务响应码；HTTP 传输仍遵循现有统一 200 契约。</summary>
    public int Code { get; init; }

    /// <summary>适合展示给当前用户的业务消息。</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>移除或掩码敏感字段后的业务数据。</summary>
    public JsonElement? Data { get; init; }

    /// <summary>结果是否因元素或字节边界被安全截断。</summary>
    public bool IsTruncated { get; init; }

    /// <summary>投影后 JSON 结果的 UTF-8 字节数。</summary>
    public int PayloadBytes { get; init; }
}
