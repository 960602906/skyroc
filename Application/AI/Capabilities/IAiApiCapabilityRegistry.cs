namespace Application.AI.Capabilities;

/// <summary>按关键词搜索业务 API 能力并按稳定标识读取完整契约。</summary>
public interface IAiApiCapabilityRegistry
{
    /// <summary>在全局上限内搜索当前调用者可见的能力摘要。</summary>
    Task<IReadOnlyList<AiApiCapability>> SearchAsync(
        string? keyword,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>按全局唯一操作标识读取能力及其请求、响应 Schema。</summary>
    Task<AiApiCapability?> GetByOperationIdAsync(
        string operationId,
        CancellationToken cancellationToken = default);
}
