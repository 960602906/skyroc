namespace Application.AI.Models;

/// <summary>表示模型生成并完成分片聚合后的单个工具调用。</summary>
public sealed class AiToolCall
{
    /// <summary>本轮模型生成的调用编号，用于关联工具结果。</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>必须命中当前用户可见白名单的工具名称。</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>完整 JSON 参数；形成合法 JSON 后才能校验和执行。</summary>
    public string ArgumentsJson { get; init; } = string.Empty;
}
