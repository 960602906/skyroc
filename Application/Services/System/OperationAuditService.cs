using Application.DTOs.System;
using Application.Interfaces;
using Application.Interfaces.System;
using Domain.Entities;
using Domain.Interfaces;
using Shared.Constants;

namespace Application.Services.System;

/// <summary>将 Web 层提供的脱敏关键操作摘要写入操作日志。</summary>
public class OperationAuditService(
    IOperationLogRepository operationLogRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IOperationAuditService
{
    /// <inheritdoc />
    public async Task RecordAsync(OperationAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var entity = new OperationLog
        {
            Id = Guid.NewGuid(),
            Module = AuditTextSanitizer.Required(entry.Module, 64, "未知"),
            OperationType = AuditTextSanitizer.Required(entry.OperationType, 64, "未知"),
            Desc = AuditTextSanitizer.Required(entry.Description, 512, "未知"),
            Method = AuditTextSanitizer.Required(entry.Method, 10, "未知"),
            Url = AuditTextSanitizer.Required(entry.Url, 512, "未知"),
            RequestParams = AuditTextSanitizer.Optional(entry.RequestSummary, 4_000),
            ResponseResult = AuditTextSanitizer.Optional(entry.ResponseSummary, 1_000),
            IpAddress = AuditTextSanitizer.Required(entry.IpAddress, 50, "未知"),
            Browser = AuditTextSanitizer.Optional(entry.UserAgent, 255),
            ExecutionDuration = Math.Max(0, entry.ExecutionDuration),
            IsSuccess = entry.IsSuccess,
            ErrorMessage = AuditTextSanitizer.Optional(entry.ErrorMessage, 1_000),
            Status = Status.Enable,
            CreateBy = currentUserService.GetUserId(),
            CreateName = currentUserService.GetUserName()
        };
        await operationLogRepository.AddAsync(entity);
        await unitOfWork.SaveChangesAsync();
    }
}
