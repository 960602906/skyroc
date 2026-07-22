namespace Application.AI.Capabilities;

/// <summary>能力结果中敏感字段的投影策略。</summary>
public enum AiSensitiveFieldPolicy
{
    /// <summary>使用网关的默认敏感字段规则。</summary>
    Default = 0,
    /// <summary>保留字段但对值进行掩码处理。</summary>
    Mask = 1,
    /// <summary>从提供给模型的结果中完全移除敏感字段。</summary>
    Remove = 2
}
