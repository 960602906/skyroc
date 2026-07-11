namespace Application.DTOs.System;

/// <summary>由 Web 层提交给审计服务的已脱敏关键操作记录。</summary>
public class OperationAuditEntry
{
    /// <summary>业务模块或第一个路由分段。</summary>
    public string Module { get; set; } = string.Empty;
    /// <summary>操作类型，例如 Create、Update 或 Delete。</summary>
    public string OperationType { get; set; } = string.Empty;
    /// <summary>操作说明。</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>HTTP 方法。</summary>
    public string Method { get; set; } = string.Empty;
    /// <summary>不含认证信息的 URL。</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>已脱敏且截断的请求摘要。</summary>
    public string? RequestSummary { get; set; }
    /// <summary>响应状态摘要。</summary>
    public string? ResponseSummary { get; set; }
    /// <summary>客户端 IP 地址。</summary>
    public string IpAddress { get; set; } = string.Empty;
    /// <summary>客户端浏览器或 User-Agent 摘要。</summary>
    public string? UserAgent { get; set; }
    /// <summary>请求持续时间，单位为毫秒。</summary>
    public long ExecutionDuration { get; set; }
    /// <summary>操作是否正常完成。</summary>
    public bool IsSuccess { get; set; }
    /// <summary>失败的安全错误摘要。</summary>
    public string? ErrorMessage { get; set; }
}
