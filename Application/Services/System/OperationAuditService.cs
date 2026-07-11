using Application.DTOs.System;
using Application.interfaces;
using Application.interfaces.System;
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
            Id = Guid.NewGuid(), Module = Limit(entry.Module, 64), OperationType = Limit(entry.OperationType, 64),
            Desc = Limit(entry.Description, 512), Method = Limit(entry.Method, 10), Url = Limit(entry.Url, 512),
            RequestParams = LimitNullable(entry.RequestSummary, 4_000), ResponseResult = LimitNullable(entry.ResponseSummary, 1_000),
            IpAddress = Limit(entry.IpAddress, 50), Browser = LimitNullable(entry.UserAgent, 255),
            ExecutionDuration = Math.Max(0, entry.ExecutionDuration), IsSuccess = entry.IsSuccess,
            ErrorMessage = LimitNullable(entry.ErrorMessage, 1_000), Status = Status.Enable,
            CreateBy = currentUserService.GetUserId(), CreateName = currentUserService.GetUserName()
        };
        await operationLogRepository.AddAsync(entity);
        await unitOfWork.SaveChangesAsync();
    }

    private static string Limit(string value, int maxLength) => string.IsNullOrWhiteSpace(value) ? "未知" : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];
    private static string? LimitNullable(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];
}
