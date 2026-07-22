namespace Application.AI.Capabilities;

/// <summary>由 HTTP 默认规则和显式标注合并得到的 AI 操作元数据。</summary>
public sealed class AiOperationMetadata
{
    /// <summary>面向模型和确认页面展示的业务标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>能力搜索使用的业务分类。</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>操作风险等级。</summary>
    public AiOperationRiskLevel RiskLevel { get; init; }

    /// <summary>执行前的人工确认方式。</summary>
    public AiConfirmationMode ConfirmationMode { get; init; }

    /// <summary>结果集合允许返回的元素上限；空值表示使用全局边界。</summary>
    public int? MaxResultItems { get; init; }

    /// <summary>敏感字段的结果投影策略。</summary>
    public AiSensitiveFieldPolicy SensitiveFieldPolicy { get; init; }
}
