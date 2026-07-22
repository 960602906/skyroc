using System.Text.Json;

namespace Application.AI.Models;

/// <summary>描述允许模型调用的白名单工具及其 JSON Schema 输入边界。</summary>
public sealed class AiToolDefinition
{
    /// <summary>工具稳定名称，供模型调用和审计摘要使用。</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>向模型说明工具用途、权限边界和结果范围。</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>工具参数 JSON Schema；执行前必须严格校验。</summary>
    public JsonElement InputSchema { get; init; }
}
