namespace Application.AI.Capabilities;

/// <summary>AI 业务操作在执行前要求的人工确认强度。</summary>
public enum AiConfirmationMode
{
    /// <summary>未显式覆盖，使用风险等级对应的默认方式。</summary>
    Default = 0,
    /// <summary>读取操作无需创建确认草稿。</summary>
    None = 1,
    /// <summary>写操作必须由当前用户确认通用操作草稿。</summary>
    Required = 2,
    /// <summary>高风险操作必须进行更醒目的二次确认。</summary>
    HighRisk = 3
}
