namespace Domain.Entities.System;

/// <summary>
/// 登录日志，记录登录成功和失败的身份、来源及时间，不保存密码或令牌。
/// </summary>
public class LoginLog : BaseEntity
{
    /// <summary>请求中提交的登录名，用于定位不存在用户的失败尝试。</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>已匹配用户的主键；未知用户名的失败登录为空。</summary>
    public Guid? UserId { get; set; }

    /// <summary>登录是否验证成功。</summary>
    public bool IsSuccess { get; set; }

    /// <summary>失败原因的安全摘要，成功时为空且不得包含密码或令牌。</summary>
    public string? FailureReason { get; set; }

    /// <summary>客户端 IP 地址，不可获取时保存空字符串。</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>客户端 User-Agent 摘要，用于排查浏览器或客户端问题。</summary>
    public string? UserAgent { get; set; }

    /// <summary>本次登录校验完成的 UTC 时间。</summary>
    public DateTime LoginTime { get; set; }
}
