namespace Application.AI.Models;

/// <summary>表示厂商错误归一化后的安全结果，供编排层决定重试或终止。</summary>
public sealed class AiProviderError
{
    /// <summary>跨厂商稳定错误码，例如 timeout、rate_limit 或 invalid_request。</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>可记录和展示的脱敏错误说明。</summary>
    public string Message { get; init; } = string.Empty;
    /// <summary>指示相同请求是否可以在退避后重试。</summary>
    public bool IsTransient { get; init; }
    /// <summary>厂商建议的重试等待时间；未提供时为空。</summary>
    public TimeSpan? RetryAfter { get; init; }
}
