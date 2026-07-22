namespace Domain.Entities.AI;

/// <summary>
/// AI 消息的生成状态，用于区分完整内容与中断内容。
/// </summary>
public enum AiMessageStatus
{
    /// <summary>
    /// 消息仍在生成或等待工具结果。
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 消息已完整生成，可作为后续上下文使用。
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 消息因模型或工具故障失败。
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 消息由用户取消或请求断开而终止。
    /// </summary>
    Cancelled = 4
}
