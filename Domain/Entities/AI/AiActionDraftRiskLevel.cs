namespace Domain.Entities.AI;

/// <summary>
/// AI 通用操作草稿的持久化风险等级。
/// </summary>
public enum AiActionDraftRiskLevel
{
    /// <summary>普通新增、修改或删除操作，需标准人工确认。</summary>
    Write = 1,

    /// <summary>权限、审核、作废、结算等操作，需高风险二次确认。</summary>
    HighRisk = 2
}
