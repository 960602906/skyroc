namespace Domain.Entities.AI;

/// <summary>
/// AI 通用操作草稿从待确认到执行终态的业务状态。
/// </summary>
public enum AiActionDraftStatus
{
    /// <summary>草稿已生成，等待所属用户确认。</summary>
    PendingConfirmation = 1,

    /// <summary>草稿已由所属用户确认，等待执行。</summary>
    Confirmed = 2,

    /// <summary>草稿正在调用原业务接口。</summary>
    Executing = 3,

    /// <summary>原业务接口已成功受理并返回结果。</summary>
    Executed = 4,

    /// <summary>原业务接口返回业务失败或调用异常。</summary>
    Failed = 5,

    /// <summary>草稿已超过允许确认的有效期。</summary>
    Expired = 6,

    /// <summary>草稿已由用户取消。</summary>
    Cancelled = 7
}
