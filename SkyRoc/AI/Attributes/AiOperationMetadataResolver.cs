using Application.AI.Capabilities;

namespace SkyRoc.AI.Attributes;

/// <summary>合并 HTTP 默认值和显式标注，并拒绝降低写操作安全等级。</summary>
public static class AiOperationMetadataResolver
{
    /// <summary>生成最终 AI 操作元数据。</summary>
    /// <param name="httpMethod">Action 声明的明确 HTTP 方法。</param>
    /// <param name="attribute">可选的显式覆盖标注。</param>
    /// <returns>不可弱化的风险、确认和结果投影元数据。</returns>
    public static AiOperationMetadata Resolve(string httpMethod, AiOperationAttribute? attribute = null)
    {
        var defaults = AiOperationDefaults.ForHttpMethod(httpMethod);
        if (attribute is null)
            return defaults;

        if (attribute.MaxResultItems < 0)
            throw new InvalidOperationException("AiOperation.MaxResultItems 不能小于零。");

        var riskLevel = attribute.RiskLevel == AiOperationRiskLevel.Default
            ? defaults.RiskLevel
            : attribute.RiskLevel;
        if (riskLevel < defaults.RiskLevel)
            throw new InvalidOperationException("AiOperation 不能降低 HTTP 方法的默认风险等级。");

        var minimumConfirmation = riskLevel switch
        {
            AiOperationRiskLevel.Read => defaults.ConfirmationMode,
            AiOperationRiskLevel.Write => AiConfirmationMode.Required,
            AiOperationRiskLevel.HighRisk => AiConfirmationMode.HighRisk,
            _ => throw new InvalidOperationException("AiOperation 风险等级无效。")
        };
        var confirmationMode = attribute.ConfirmationMode == AiConfirmationMode.Default
            ? minimumConfirmation
            : attribute.ConfirmationMode;
        if (confirmationMode < minimumConfirmation)
            throw new InvalidOperationException("AiOperation 不能降低风险等级要求的确认强度。");

        return new AiOperationMetadata
        {
            Title = attribute.Title?.Trim() ?? string.Empty,
            Category = attribute.Category?.Trim() ?? string.Empty,
            RiskLevel = riskLevel,
            ConfirmationMode = confirmationMode,
            MaxResultItems = attribute.MaxResultItems == 0 ? null : attribute.MaxResultItems,
            SensitiveFieldPolicy = attribute.SensitiveFieldPolicy
        };
    }
}
