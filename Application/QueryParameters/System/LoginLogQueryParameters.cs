namespace Application.QueryParameters.System;

/// <summary>登录审计日志分页筛选条件。</summary>
public class LoginLogQueryParameters : PagedQueryParameters
{
    /// <summary>登录名关键字，按不区分大小写的包含关系筛选。</summary>
    public string? Username { get; set; }
    /// <summary>按登录成功或失败筛选。</summary>
    public bool? IsSuccess { get; set; }
    /// <summary>登录时间下限（UTC，含）。</summary>
    public DateTime? StartTime { get; set; }
    /// <summary>登录时间上限（UTC，含）。</summary>
    public DateTime? EndTime { get; set; }
}
