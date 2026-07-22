using Application.AI.Capabilities;

namespace SkyRoc.AI.Attributes;

/// <summary>覆盖业务 API 的 AI 展示、风险、确认和结果投影元数据。</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AiOperationAttribute : Attribute
{
    /// <summary>面向能力搜索和确认页面的业务标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>能力搜索使用的业务分类。</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>显式风险等级；只能保持或收紧 HTTP 默认等级。</summary>
    public AiOperationRiskLevel RiskLevel { get; set; } = AiOperationRiskLevel.Default;

    /// <summary>显式确认方式；只能保持或收紧默认确认要求。</summary>
    public AiConfirmationMode ConfirmationMode { get; set; } = AiConfirmationMode.Default;

    /// <summary>结果集合元素上限；零表示使用全局上限。</summary>
    public int MaxResultItems { get; set; }

    /// <summary>敏感字段投影策略。</summary>
    public AiSensitiveFieldPolicy SensitiveFieldPolicy { get; set; } = AiSensitiveFieldPolicy.Default;
}
