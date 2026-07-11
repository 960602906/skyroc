using Application.DTOs.System;
using Application.interfaces.System;

namespace SkyRoc.Services;

/// <summary>从当前 ASP.NET Core 请求提取登录审计允许保存的来源摘要。</summary>
public sealed class HttpAuditRequestSourceAccessor(IHttpContextAccessor httpContextAccessor)
    : IAuditRequestSourceAccessor
{
    /// <inheritdoc />
    public AuditRequestSource GetCurrent()
    {
        var context = httpContextAccessor.HttpContext;
        return new AuditRequestSource
        {
            IpAddress = context?.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = context?.Request.Headers.UserAgent.ToString()
        };
    }
}
