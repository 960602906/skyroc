namespace Application.AI.Capabilities;

/// <summary>根据明确 HTTP 方法生成不可放宽的 AI 风险与确认默认值。</summary>
public static class AiOperationDefaults
{
    /// <summary>将 GET/HEAD 分类为读取，其余受支持写方法分类为需确认写入。</summary>
    /// <param name="httpMethod">控制器 Action 声明的明确 HTTP 方法。</param>
    /// <returns>该 HTTP 方法对应的风险与确认元数据。</returns>
    /// <exception cref="ArgumentException">方法为空或不是受支持的 JSON 业务方法。</exception>
    public static AiOperationMetadata ForHttpMethod(string httpMethod)
    {
        if (string.IsNullOrWhiteSpace(httpMethod))
            throw new ArgumentException("AI 能力必须声明明确的 HTTP 方法。", nameof(httpMethod));

        return httpMethod.Trim().ToUpperInvariant() switch
        {
            "GET" or "HEAD" => new AiOperationMetadata
            {
                RiskLevel = AiOperationRiskLevel.Read,
                ConfirmationMode = AiConfirmationMode.None
            },
            "POST" or "PUT" or "PATCH" or "DELETE" => new AiOperationMetadata
            {
                RiskLevel = AiOperationRiskLevel.Write,
                ConfirmationMode = AiConfirmationMode.Required
            },
            _ => throw new ArgumentException($"HTTP 方法 '{httpMethod}' 不属于受支持的 JSON 业务能力。", nameof(httpMethod))
        };
    }
}
