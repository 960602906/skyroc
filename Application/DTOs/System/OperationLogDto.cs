using Application.DTOs;

namespace Application.DTOs.System;

/// <summary>关键接口操作审计日志响应，敏感请求内容已在写入前脱敏。</summary>
public class OperationLogDto : BaseDto
{
    /// <summary>业务模块或路由分段。</summary>
    public string Module { get; set; } = string.Empty;
    /// <summary>HTTP 方法映射的操作类型。</summary>
    public string OperationType { get; set; } = string.Empty;
    /// <summary>操作说明。</summary>
    public string Desc { get; set; } = string.Empty;
    /// <summary>请求 HTTP 方法。</summary>
    public string Method { get; set; } = string.Empty;
    /// <summary>不含认证令牌的请求 URL。</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>已经脱敏和长度截断的请求摘要。</summary>
    public string? RequestParams { get; set; }
    /// <summary>响应状态摘要，不包含响应业务数据。</summary>
    public string? ResponseResult { get; set; }
    /// <summary>客户端 IP 地址。</summary>
    public string IpAddress { get; set; } = string.Empty;
    /// <summary>请求执行耗时，单位为毫秒。</summary>
    public long ExecutionDuration { get; set; }
    /// <summary>控制器操作是否成功返回。</summary>
    public bool IsSuccess { get; set; }
    /// <summary>失败的安全错误摘要。</summary>
    public string? ErrorMessage { get; set; }
}
