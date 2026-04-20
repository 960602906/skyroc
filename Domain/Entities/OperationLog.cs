namespace Domain.Entities;
/// <summary>
///  操作日志实体
/// </summary>
public class OperationLog:BaseEntity
{
    /// <summary>
    /// 操作模块（如：user, role, menu）
    /// </summary>
    public string Module { get; set; } = string.Empty;
    /// <summary>
    /// 操作类型（Create, Update, Delete, Query, Export, Import）
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    /// <summary>
    /// 操作描述
    /// </summary>
    public string Desc { get; set; } = string.Empty;
    /// <summary>
    /// 请求方法（GET, POST, PUT, DELETE）
    /// </summary>
    public string Method { get; set; } = string.Empty;
    /// <summary>
    /// 请求URL
    /// </summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// 请求参数（JSON）
    /// </summary>
    public string? RequestParams { get; set; }
    /// <summary>
    /// 响应结果（JSON）
    /// </summary>
    public string? ResponseResult { get; set; }
    /// <summary>
    /// 操作IP
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    /// <summary>
    /// 操作地点（IP转城市）
    /// </summary>
    public string? Location { get; set; }
    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }
    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }
    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    public long ExecutionDuration { get; set; }
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }
    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
   
}