using System.Text.Json;

namespace Application.AI.Capabilities;

/// <summary>模型提交给受控 API 网关的最小调用参数。</summary>
public sealed class AiApiOperationRequest
{
    /// <summary>由能力注册表解析的稳定操作标识。</summary>
    public string OperationId { get; init; } = string.Empty;

    /// <summary>严格按能力 JSON Schema 提交的业务参数，不包含任何传输或身份字段。</summary>
    public JsonElement Arguments { get; init; }
}
