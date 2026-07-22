namespace Domain.Entities.AI;

/// <summary>
/// AI 订单草稿从待确认到终态的业务状态。
/// </summary>
public enum AiOrderDraftStatus
{
    /// <summary>
    /// 草稿已生成，等待所属用户在页面人工确认。
    /// </summary>
    PendingConfirmation = 1,

    /// <summary>
    /// 草稿已确认并创建正式销售订单。
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// 草稿超过有效期，不能再确认。
    /// </summary>
    Expired = 3,

    /// <summary>
    /// 草稿已由用户取消。
    /// </summary>
    Cancelled = 4
}
