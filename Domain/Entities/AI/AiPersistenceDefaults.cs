namespace Domain.Entities.AI;

/// <summary>
/// AI 持久化对象的默认保留期限，供实体初始值和后续应用服务统一使用。
/// </summary>
public static class AiPersistenceDefaults
{
    /// <summary>
    /// 会话及其消息默认保留天数。
    /// </summary>
    public const int ConversationRetentionDays = 30;

    /// <summary>
    /// 待人工确认通用操作草稿的默认有效分钟数。
    /// </summary>
    public const int ActionDraftLifetimeMinutes = 30;

    /// <summary>
    /// 待人工确认订单草稿的默认有效分钟数。
    /// </summary>
    public const int OrderDraftLifetimeMinutes = 30;
}
