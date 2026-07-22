namespace Domain.Entities.AI;

/// <summary>
/// AI 助手会话，按系统用户隔离并控制消息的默认保留期限。
/// </summary>
public class AiConversation : BaseEntity
{
    /// <summary>
    /// 拥有该会话的系统用户主键；其他用户不得读取或追加消息。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 会话列表中展示的标题，不包含模型推理内容。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 会话当前生命周期状态。
    /// </summary>
    public AiConversationStatus ConversationStatus { get; set; } = AiConversationStatus.Active;

    /// <summary>
    /// 最近一条用户、助手或工具摘要消息的写入时间（UTC）。
    /// </summary>
    public DateTime? LastMessageTime { get; set; }

    /// <summary>
    /// 会话及其消息最早可被清理的时间（UTC），默认自创建起保留 30 天。
    /// </summary>
    public DateTime RetainUntil { get; set; } = DateTime.UtcNow.AddDays(AiPersistenceDefaults.ConversationRetentionDays);

    /// <summary>
    /// 用户执行删除操作的时间（UTC）；活动会话为空。
    /// </summary>
    public DateTime? DeletedTime { get; set; }

    /// <summary>
    /// 会话所属系统用户。
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 按序号组成的会话消息集合。
    /// </summary>
    public virtual ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();

    /// <summary>
    /// 在该会话中生成的通用业务操作草稿集合。
    /// </summary>
    public virtual ICollection<AiActionDraft> ActionDrafts { get; set; } = new List<AiActionDraft>();

    /// <summary>
    /// 在该会话中生成的订单草稿集合。
    /// </summary>
    public virtual ICollection<AiOrderDraft> OrderDrafts { get; set; } = new List<AiOrderDraft>();
}
