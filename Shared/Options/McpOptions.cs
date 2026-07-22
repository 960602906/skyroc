namespace Shared.Options;

/// <summary>SkyRoc MCP 外部访问开关、来源白名单和 Token 哈希配置。</summary>
public sealed class McpOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "Mcp";
    /// <summary>是否允许个人 MCP Token 从外部客户端访问；默认关闭。</summary>
    public bool ExternalEnabled { get; set; }
    /// <summary>允许携带 Origin 请求头访问 MCP 的 HTTP/HTTPS 来源。</summary>
    public List<string> AllowedOrigins { get; set; } = [];
    /// <summary>保存 Token HMAC-SHA256 密钥的环境变量名称。</summary>
    public string TokenHashKeyEnvironmentVariable { get; set; } = string.Empty;
    /// <summary>保存内部委托令牌签名密钥的环境变量名称。</summary>
    public string DelegationSigningKeyEnvironmentVariable { get; set; } = string.Empty;
}
