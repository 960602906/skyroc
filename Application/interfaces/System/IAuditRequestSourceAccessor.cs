using Application.DTOs.System;

namespace Application.interfaces.System;

/// <summary>提供当前请求可用于安全审计的非敏感来源摘要。</summary>
public interface IAuditRequestSourceAccessor
{
    /// <summary>读取当前请求的 IP 与 User-Agent，不包含正文、密码或令牌。</summary>
    /// <returns>当前请求来源摘要；无 HTTP 请求时返回空摘要。</returns>
    AuditRequestSource GetCurrent();
}
