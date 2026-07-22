namespace Domain.Entities.AI;

/// <summary>
/// AI 会话的生命周期状态。
/// </summary>
public enum AiConversationStatus
{
    /// <summary>
    /// 会话可继续追加消息。
    /// </summary>
    Active = 1,

    /// <summary>
    /// 用户已删除会话，等待保留期清理。
    /// </summary>
    Deleted = 2
}
