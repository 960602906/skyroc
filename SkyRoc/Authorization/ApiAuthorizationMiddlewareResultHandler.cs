using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.IdentityModel.Tokens;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Middleware;

namespace SkyRoc.Authorization;

/// <summary>
///     授权失败统一出口：HTTP 固定 200，业务码写在 body.code（401/403）。
/// </summary>
public sealed class ApiAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    /// <inheritdoc />
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            await WriteBusinessFailureAsync(
                context,
                ApiResponse<string>.Unauthorized(ResolveUnauthorizedMessage(context)));
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await WriteBusinessFailureAsync(context, ApiResponse<string>.Forbidden());
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static string ResolveUnauthorizedMessage(HttpContext context)
    {
        var failure = context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult?.Failure;
        return failure switch
        {
            SecurityTokenExpiredException => "令牌已过期，请重新登录或刷新令牌",
            SecurityTokenInvalidIssuerException => "令牌发行者无效",
            SecurityTokenInvalidAudienceException => "令牌受众无效",
            SecurityTokenException tokenException => string.IsNullOrWhiteSpace(tokenException.Message)
                ? "认证失败，令牌格式错误或无效"
                : tokenException.Message,
            _ => string.IsNullOrEmpty(context.Request.Headers.Authorization)
                ? "认证失败,未提供认证令牌"
                : "认证失败，令牌格式错误或无效"
        };
    }

    private static async Task WriteBusinessFailureAsync(HttpContext context, ApiResponse<string> payload)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";
        ApiResponseHttp.MarkBusinessCode(context, payload.Code);
        await context.Response.WriteAsJsonAsync(payload);
    }
}
