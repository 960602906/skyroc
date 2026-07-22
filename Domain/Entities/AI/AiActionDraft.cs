namespace Domain.Entities.AI;

/// <summary>
/// AI 为写业务 API 生成的通用操作草稿，必须由所属用户在有效期内确认后执行。
/// </summary>
public class AiActionDraft : BaseEntity
{
    /// <summary>来源 AI 会话主键；外部 MCP 客户端独立生成草稿时可为空。</summary>
    public Guid? ConversationId { get; set; }

    /// <summary>草稿所属系统用户主键，也是唯一允许确认和执行的用户。</summary>
    public Guid UserId { get; set; }

    /// <summary>能力目录中的稳定操作标识，确认和执行时不得改变。</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>属性排序且无额外空白的规范化业务参数 JSON。</summary>
    public string CanonicalArgumentsJson { get; set; } = "{}";

    /// <summary>绑定所属用户、operationId 和规范化参数的 SHA-256 哈希。</summary>
    public string ArgumentsHash { get; set; } = string.Empty;

    /// <summary>草稿写操作的风险等级，决定确认页面的提示强度。</summary>
    public AiActionDraftRiskLevel RiskLevel { get; set; } = AiActionDraftRiskLevel.Write;

    /// <summary>展示给用户的最小业务变更摘要，不包含密钥或完整敏感响应。</summary>
    public string ConfirmationSummary { get; set; } = string.Empty;

    /// <summary>草稿当前确认和执行状态。</summary>
    public AiActionDraftStatus DraftStatus { get; set; } = AiActionDraftStatus.PendingConfirmation;

    /// <summary>草稿失效时间（UTC），默认自生成起 30 分钟。</summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(AiPersistenceDefaults.ActionDraftLifetimeMinutes);

    /// <summary>实际确认草稿的系统用户主键；未确认时为空且必须等于所属用户。</summary>
    public Guid? ConfirmedByUserId { get; set; }

    /// <summary>草稿通过人工确认的时间（UTC）。</summary>
    public DateTime? ConfirmedTime { get; set; }

    /// <summary>原业务接口执行结束的时间（UTC）。</summary>
    public DateTime? ExecutedTime { get; set; }

    /// <summary>可追溯执行结果的非敏感引用，不保存完整工具响应。</summary>
    public string? ExecutionResultReference { get; set; }

    /// <summary>草稿生成请求的用户级幂等键。</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>草稿状态流转使用的乐观并发版本，每次更新必须递增。</summary>
    public long ConcurrencyVersion { get; set; } = 1;

    /// <summary>判断确认请求是否保持原草稿的用户、operationId 和业务参数不变。</summary>
    public bool MatchesConfirmation(Guid currentUserId, string operationId, string argumentsJson) =>
        currentUserId == UserId
        && !string.IsNullOrWhiteSpace(operationId)
        && string.Equals(OperationId, operationId.Trim(), StringComparison.Ordinal)
        && AiActionDraftIntegrity.Matches(ArgumentsHash, currentUserId, operationId, argumentsJson);

    /// <summary>来源 AI 会话；外部 MCP 独立草稿为空。</summary>
    public virtual AiConversation? Conversation { get; set; }

    /// <summary>草稿所属系统用户。</summary>
    public virtual User User { get; set; } = null!;

    /// <summary>实际确认草稿的系统用户。</summary>
    public virtual User? ConfirmedByUser { get; set; }
}
