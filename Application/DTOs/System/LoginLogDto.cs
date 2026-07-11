using Application.DTOs;

namespace Application.DTOs.System;

/// <summary>登录审计日志响应，不包含密码、访问令牌或刷新令牌。</summary>
public class LoginLogDto : BaseDto
{
    /// <summary>请求中的登录名。</summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>已匹配用户的主键；未知用户名为空。</summary>
    public Guid? UserId { get; set; }
    /// <summary>登录是否验证成功。</summary>
    public bool IsSuccess { get; set; }
    /// <summary>失败原因安全摘要。</summary>
    public string? FailureReason { get; set; }
    /// <summary>客户端 IP 地址。</summary>
    public string IpAddress { get; set; } = string.Empty;
    /// <summary>客户端 User-Agent 摘要。</summary>
    public string? UserAgent { get; set; }
    /// <summary>登录校验完成时间（UTC）。</summary>
    public DateTime LoginTime { get; set; }
}
