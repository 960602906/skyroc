using Application.interfaces.System;
using Domain.Entities.System;
using Domain.Interfaces;
using Domain.Interfaces.System;
using Microsoft.AspNetCore.Http;
using Shared.Constants;

namespace Application.Services.System;

/// <summary>记录认证结果并从当前 HTTP 请求提取非敏感来源信息。</summary>
public class LoginAuditService(
    ILoginLogRepository loginLogRepository,
    IUnitOfWork unitOfWork,
    IHttpContextAccessor httpContextAccessor) : ILoginAuditService
{
    /// <inheritdoc />
    public async Task RecordAsync(string username, Guid? userId, bool isSuccess, string? failureReason)
    {
        var context = httpContextAccessor.HttpContext;
        var entity = new LoginLog
        {
            Id = Guid.NewGuid(), Username = Limit(username, 100), UserId = userId, IsSuccess = isSuccess,
            FailureReason = isSuccess ? null : LimitNullable(failureReason, 500),
            IpAddress = Limit(context?.Connection.RemoteIpAddress?.ToString() ?? string.Empty, 50),
            UserAgent = LimitNullable(context?.Request.Headers["User-Agent"].ToString(), 500), LoginTime = DateTime.UtcNow,
            Status = Status.Enable
        };
        await loginLogRepository.AddAsync(entity);
        await unitOfWork.SaveChangesAsync();
    }

    private static string Limit(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];
    private static string? LimitNullable(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];
}
