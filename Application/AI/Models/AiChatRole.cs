namespace Application.AI.Models;

/// <summary>AI 对话消息角色；工具结果须关联原工具调用编号。</summary>
public enum AiChatRole
{
    /// <summary>限定模型行为和项目事实来源的系统消息。</summary>
    System = 0,
    /// <summary>当前登录用户提交的消息。</summary>
    User = 1,
    /// <summary>模型生成的最终文字或工具调用消息。</summary>
    Assistant = 2,
    /// <summary>应用执行白名单工具后返回给模型的结果。</summary>
    Tool = 3
}
