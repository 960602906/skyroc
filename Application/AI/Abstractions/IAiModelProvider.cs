using Application.AI.Models;

namespace Application.AI.Abstractions;

/// <summary>定义与模型厂商无关的流式对话能力，业务编排只能依赖本接口。</summary>
public interface IAiModelProvider
{
    /// <summary>获取匹配 <c>Ai:Providers:*:Adapter</c> 的稳定适配器名称。</summary>
    string AdapterName { get; }

    /// <summary>以统一分片契约执行对话，并把取消传递到厂商请求与响应读取过程。</summary>
    /// <param name="request">已完成业务权限过滤的消息和工具请求。</param>
    /// <param name="cancellationToken">用户取消、请求断开或超时令牌。</param>
    /// <returns>规范化后的文字、工具调用、用量和结束分片。</returns>
    IAsyncEnumerable<AiStreamChunk> StreamChatAsync(
        AiChatRequest request,
        CancellationToken cancellationToken);

    /// <summary>获取适配器支持的流式、工具调用和用量能力。</summary>
    /// <param name="cancellationToken">取消能力探测的令牌。</param>
    /// <returns>不包含厂商密钥或原始响应的能力描述。</returns>
    ValueTask<AiProviderCapabilities> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>把厂商异常转换为可供编排层判断重试与展示策略的安全错误。</summary>
    /// <param name="exception">适配器捕获的异常。</param>
    /// <returns>已去除密钥、请求头和原始敏感响应的统一错误。</returns>
    AiProviderError NormalizeError(Exception exception);

    /// <summary>校验当前适配器专属配置；异常不得包含密钥值。</summary>
    void ValidateConfiguration();
}
