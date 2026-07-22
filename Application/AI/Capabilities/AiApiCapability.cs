using System.Text.Json;

namespace Application.AI.Capabilities;

/// <summary>可由 AI 搜索并按稳定标识调用的单个业务 API 能力。</summary>
public sealed class AiApiCapability
{
    /// <summary>全局唯一且跨发布稳定的操作标识，格式为 ControllerName.ActionName。</summary>
    public string OperationId { get; init; } = string.Empty;

    /// <summary>业务操作标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>能力所属业务分类。</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>控制器 Action 声明的 HTTP 方法，仅用于描述，调用方不能覆盖。</summary>
    public string HttpMethod { get; init; } = string.Empty;

    /// <summary>风险等级。</summary>
    public AiOperationRiskLevel RiskLevel { get; init; }

    /// <summary>执行前的人工确认方式。</summary>
    public AiConfirmationMode ConfirmationMode { get; init; }

    /// <summary>结果集合元素上限；空值表示使用全局上限。</summary>
    public int? MaxResultItems { get; init; }

    /// <summary>敏感字段投影策略。</summary>
    public AiSensitiveFieldPolicy SensitiveFieldPolicy { get; init; }

    /// <summary>现有业务权限资源编码；AI 元数据不能覆盖该值。</summary>
    public string PermissionResource { get; init; } = string.Empty;

    /// <summary>现有业务权限动作编码；AI 元数据不能覆盖该值。</summary>
    public string PermissionAction { get; init; } = string.Empty;

    /// <summary>是否要求已认证身份。</summary>
    public bool RequiresAuthorization { get; init; }

    /// <summary>业务参数的厂商无关 JSON Schema。</summary>
    public JsonElement? InputSchema { get; init; }

    /// <summary>业务响应的厂商无关 JSON Schema。</summary>
    public JsonElement? ResponseSchema { get; init; }
}
