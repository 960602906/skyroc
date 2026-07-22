namespace Application.AI.Capabilities;

/// <summary>按能力元数据实施元素限量、字节限量和敏感字段处理。</summary>
public interface IAiApiResponseProjector
{
    /// <summary>将业务 API 结果投影为允许提供给模型的最小结果。</summary>
    Task<AiApiOperationResult> ProjectAsync(
        AiApiCapability capability,
        AiApiOperationResult result,
        CancellationToken cancellationToken = default);
}
