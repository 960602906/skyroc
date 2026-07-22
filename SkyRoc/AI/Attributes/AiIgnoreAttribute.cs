namespace SkyRoc.AI.Attributes;

/// <summary>将技术端点或不支持的非通用契约从 AI 能力目录中显式排除。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AiIgnoreAttribute : Attribute
{
    /// <summary>创建带有可审计排除原因的标注。</summary>
    /// <param name="reason">不能留空的排除原因。</param>
    public AiIgnoreAttribute(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("AI 能力排除原因不能为空。", nameof(reason));

        Reason = reason.Trim();
    }

    /// <summary>该端点不能通过通用 AI 网关调用的原因。</summary>
    public string Reason { get; }
}
