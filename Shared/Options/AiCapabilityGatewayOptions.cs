namespace Shared.Options;

/// <summary>AI 能力目录和内部 API 调用网关的固定安全边界。</summary>
public sealed class AiCapabilityGatewayOptions
{
    /// <summary>服务端配置的内部 API 唯一受信任来源，模型不能覆盖。</summary>
    public string InternalBaseUrl { get; set; } = string.Empty;

    /// <summary>单次能力搜索最多返回的操作数量。</summary>
    public int SearchLimit { get; set; } = 20;

    /// <summary>单次工具结果序列化后允许返回的最大 UTF-8 字节数。</summary>
    public int MaxToolResultBytes { get; set; } = 65_536;

    /// <summary>外部 MCP 身份换取内部委托令牌后的最长有效秒数。</summary>
    public int DelegationTokenLifetimeSeconds { get; set; } = 60;

    /// <summary>高风险草稿从二次确认开始允许执行的最长秒数。</summary>
    public int HighRiskConfirmationLifetimeSeconds { get; set; } = 300;
}
