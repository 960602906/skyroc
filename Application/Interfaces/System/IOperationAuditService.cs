using Application.DTOs.System;

namespace Application.Interfaces.System;

/// <summary>定义关键 HTTP 操作审计日志的可靠写入用例。</summary>
public interface IOperationAuditService
{
    /// <summary>持久化一条已脱敏的操作审计记录；调用方应吞掉写入失败以不影响原业务响应。</summary>
    /// <param name="entry">不含密码、令牌和完整响应体的审计摘要。</param>
    Task RecordAsync(OperationAuditEntry entry);
}
