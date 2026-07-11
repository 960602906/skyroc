using Application.DTOs.System;
using Application.interfaces.System;
using Domain.Entities.System;
using Domain.Interfaces;
using Domain.Interfaces.System;
using Shared.Constants;

namespace Application.Services.System;

/// <summary>记录认证结果并从当前 HTTP 请求提取非敏感来源信息。</summary>
public class LoginAuditService(
    ILoginLogRepository loginLogRepository,
    IUnitOfWork unitOfWork,
    IAuditRequestSourceAccessor requestSourceAccessor) : ILoginAuditService
{
    /// <inheritdoc />
    public async Task RecordAsync(string username, Guid? userId, bool isSuccess, string? failureReason)
    {
        var source = requestSourceAccessor.GetCurrent();
        var entity = new LoginLog
        {
            Id = Guid.NewGuid(),
            Username = AuditTextSanitizer.Required(username, 100, string.Empty),
            UserId = userId,
            IsSuccess = isSuccess,
            FailureReason = isSuccess ? null : AuditTextSanitizer.Optional(failureReason, 500),
            IpAddress = AuditTextSanitizer.Required(source.IpAddress, 50, string.Empty),
            UserAgent = AuditTextSanitizer.Optional(source.UserAgent, 500),
            LoginTime = DateTime.UtcNow,
            Status = Status.Enable
        };
        await loginLogRepository.AddAsync(entity);
        await unitOfWork.SaveChangesAsync();
    }
}
