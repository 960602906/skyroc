namespace Application.DTOs.System;

/// <summary>审计记录可使用的非敏感 HTTP 请求来源摘要。</summary>
public sealed record AuditRequestSource
{
    /// <summary>请求来源 IP 地址；无法获取时为空字符串。</summary>
    public string IpAddress { get; init; } = string.Empty;

    /// <summary>客户端 User-Agent 摘要；无法获取时为空。</summary>
    public string? UserAgent { get; init; }
}
