namespace Application.AI.Capabilities;

/// <summary>仅按注册表中的 operationId 使用当前身份调用既有业务 API。</summary>
public interface IAiApiOperationExecutor
{
    /// <summary>解析固定路由并执行受认证、授权和业务校验保护的调用。</summary>
    Task<AiApiOperationResult> ExecuteAsync(
        AiApiOperationRequest request,
        CancellationToken cancellationToken = default);
}
