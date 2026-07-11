namespace Application.QueryParameters.System;

/// <summary>操作审计日志分页筛选条件。</summary>
public class OperationLogQueryParameters : PagedQueryParameters
{
    /// <summary>内容关键字，不区分大小写匹配描述、地址、请求/响应摘要、错误摘要和操作人。</summary>
    public string? Keyword { get; set; }
    /// <summary>按模块精确筛选，空值表示不限制模块。</summary>
    public string? Module { get; set; }
    /// <summary>按 HTTP 方法或业务操作类型筛选。</summary>
    public string? OperationType { get; set; }
    /// <summary>按成功或失败结果筛选。</summary>
    public bool? IsSuccess { get; set; }
    /// <summary>操作发生时间下限（UTC，含）。</summary>
    public DateTime? StartTime { get; set; }
    /// <summary>操作发生时间上限（UTC，含）。</summary>
    public DateTime? EndTime { get; set; }
}
