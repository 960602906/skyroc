using System.Diagnostics;
using Application.DTOs.System;
using Application.interfaces.System;

namespace SkyRoc.Middleware;

/// <summary>为受保护写操作记录脱敏的接口审计摘要，不影响原业务请求的成功或失败结果。</summary>
public class OperationAuditMiddleware(RequestDelegate next, ILogger<OperationAuditMiddleware> logger)
{
    /// <summary>执行后续管道，并在关键写操作返回或异常时写入安全审计日志。</summary>
    /// <param name="context">当前 HTTP 请求上下文。</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldAudit(context))
        {
            await next(context);
            return;
        }

        var watch = Stopwatch.StartNew();
        Exception? exception = null;
        try
        {
            await next(context);
        }
        catch (Exception caught)
        {
            exception = caught;
            throw;
        }
        finally
        {
            watch.Stop();
            try
            {
                var request = context.Request;
                var statusCode = exception is null
                    ? context.Response.StatusCode
                    : ExceptionHttpStatusMapper.GetStatusCode(exception);
                await using var auditScope = context.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
                await auditScope.ServiceProvider.GetRequiredService<IOperationAuditService>().RecordAsync(new OperationAuditEntry
                {
                    Module = GetModule(request.Path),
                    OperationType = GetOperationType(request.Method),
                    Description = $"{request.Method} {request.Path}",
                    Method = request.Method,
                    Url = request.Path,
                    RequestSummary = RedactQuery(request.Query),
                    ResponseSummary = $"HTTP {statusCode}",
                    IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    UserAgent = request.Headers.UserAgent.ToString(),
                    ExecutionDuration = watch.ElapsedMilliseconds,
                    IsSuccess = exception is null && statusCode < StatusCodes.Status400BadRequest,
                    ErrorMessage = exception is null ? null : "请求执行失败"
                });
            }
            catch (Exception auditException)
            {
                logger.LogWarning(auditException, "写入关键操作审计日志失败，原请求不受影响。Path: {Path}", context.Request.Path);
            }
        }
    }

    private static bool ShouldAudit(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true &&
        context.Request.Path.StartsWithSegments("/api") &&
        context.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE";
    private static string GetModule(PathString path) => path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? "api";
    private static string GetOperationType(string method) => method switch { "POST" => "Create", "PUT" or "PATCH" => "Update", "DELETE" => "Delete", _ => "Unknown" };
    private static string? RedactQuery(IQueryCollection query)
    {
        if (query.Count == 0) return null;
        return string.Join('&', query.Select(pair => $"{pair.Key}={(IsSensitive(pair.Key) ? "***" : pair.Value.ToString())}"));
    }
    private static bool IsSensitive(string key)
    {
        if (key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("secret", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalized = key.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        return normalized is "key" or "apikey" or "accesskey" or "privatekey" or "signingkey" or "encryptionkey" or "clientkey";
    }
}
