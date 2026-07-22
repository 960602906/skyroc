namespace Application.AI.Models;

/// <summary>统一模型结束原因，避免业务层依赖厂商专属字符串。</summary>
public enum AiFinishReason
{
    /// <summary>尚未收到结束原因。</summary>
    None = 0,
    /// <summary>模型自然完成回答。</summary>
    Stop = 1,
    /// <summary>达到输出 token 或厂商长度限制。</summary>
    Length = 2,
    /// <summary>模型请求执行一个或多个工具。</summary>
    ToolCalls = 3,
    /// <summary>内容被模型服务的安全策略中止。</summary>
    ContentFilter = 4,
    /// <summary>调用因规范化错误结束。</summary>
    Error = 5
}
