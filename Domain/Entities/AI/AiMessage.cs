namespace Domain.Entities.AI;

/// <summary>
/// AI 会话消息，只保存最终可展示文字、模型标识及脱敏后的工具和来源摘要。
/// </summary>
public class AiMessage : BaseEntity
{
    /// <summary>
    /// 所属 AI 会话主键。
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// 消息在统一对话协议中的角色。
    /// </summary>
    public AiMessageRole Role { get; set; }

    /// <summary>
    /// 用户或助手最终可展示的纯文字内容；不得写入模型推理过程或完整敏感工具结果。
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 消息在会话内单调递增的游标序号。
    /// </summary>
    public long Sequence { get; set; }

    /// <summary>
    /// 消息生成状态，用于防止中断内容被误认为完整回复。
    /// </summary>
    public AiMessageStatus MessageStatus { get; set; } = AiMessageStatus.Pending;

    /// <summary>
    /// 生成助手消息时使用的统一 Provider 名称；用户消息为空。
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// 生成助手消息时使用的模型名称；用户消息为空。
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// 工具调用在当前模型回合中的标识；非工具消息为空。
    /// </summary>
    public string? ToolCallId { get; set; }

    /// <summary>
    /// 被调用的白名单工具名称；非工具消息为空。
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// 工具执行状态的稳定文本值，不保存厂商原始响应。
    /// </summary>
    public string? ToolStatus { get; set; }

    /// <summary>
    /// 工具结果的必要脱敏摘要，不得包含完整敏感业务响应。
    /// </summary>
    public string? ToolSummary { get; set; }

    /// <summary>
    /// 知识来源的 JSON 数组，仅允许保存来源 slug 和标题。
    /// </summary>
    public string? SourceReferences { get; set; }

    /// <summary>
    /// 消息进入完成、失败或取消终态的时间（UTC）。
    /// </summary>
    public DateTime? CompletedTime { get; set; }

    /// <summary>
    /// 所属 AI 会话。
    /// </summary>
    public virtual AiConversation Conversation { get; set; } = null!;
}
