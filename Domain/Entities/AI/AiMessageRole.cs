namespace Domain.Entities.AI;

/// <summary>
/// 持久化 AI 消息在统一对话协议中的角色。
/// </summary>
public enum AiMessageRole
{
    /// <summary>
    /// 系统约束消息。
    /// </summary>
    System = 1,

    /// <summary>
    /// 当前用户提交的消息。
    /// </summary>
    User = 2,

    /// <summary>
    /// 模型返回给用户的最终消息。
    /// </summary>
    Assistant = 3,

    /// <summary>
    /// 工具调用过程的安全摘要消息。
    /// </summary>
    Tool = 4
}
