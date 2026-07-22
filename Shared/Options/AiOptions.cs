namespace Shared.Options;

/// <summary>AI 助手的功能开关、活动 Provider 和全局资源边界。</summary>
public sealed class AiOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "Ai";
    /// <summary>是否启用内部 AI 助手；关闭时不得解析厂商密钥。</summary>
    public bool Enabled { get; set; }
    /// <summary>活动 Provider 名称，对应 <see cref="Providers"/> 中的键。</summary>
    public string ActiveProvider { get; set; } = string.Empty;
    /// <summary>单次用户输入和上下文允许的最大字符数。</summary>
    public int MaxInputCharacters { get; set; } = 8000;
    /// <summary>单次模型回答允许的最大输出 token 数。</summary>
    public int MaxOutputTokens { get; set; } = 4096;
    /// <summary>一次用户请求允许的最大模型工具往返轮数。</summary>
    public int MaxToolIterations { get; set; } = 8;
    /// <summary>单次模型网络请求超时秒数。</summary>
    public int RequestTimeoutSeconds { get; set; } = 120;
    /// <summary>已完成 AI 会话的默认保留天数。</summary>
    public int ConversationRetentionDays { get; set; } = 30;
    /// <summary>订单草稿自生成起允许人工确认的分钟数。</summary>
    public int DraftExpiryMinutes { get; set; } = 30;
    /// <summary>AI 调用现有业务 API 时使用的受控网关边界。</summary>
    public AiCapabilityGatewayOptions CapabilityGateway { get; set; } = new();
    /// <summary>按稳定名称索引的 Provider 配置集合。</summary>
    public Dictionary<string, AiProviderOptions> Providers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
