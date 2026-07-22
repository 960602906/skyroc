namespace Application.AI.Capabilities;

/// <summary>AI 调用业务操作时采用的风险等级。</summary>
public enum AiOperationRiskLevel
{
    /// <summary>未显式覆盖，使用 HTTP 方法对应的默认等级。</summary>
    Default = 0,
    /// <summary>只读取数据且不改变业务状态。</summary>
    Read = 1,
    /// <summary>会新增、修改或删除业务状态。</summary>
    Write = 2,
    /// <summary>涉及权限、审核、作废、结算或其他不可轻易撤销的操作。</summary>
    HighRisk = 3
}
