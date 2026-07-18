using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.IdentityModel.Tokens;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Middleware;

namespace SkyRoc.Authorization;

/// <summary>
///     授权失败统一出口：HTTP 固定 200，业务码写在 body.code（401 / 4011 / 403）。
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
            await WriteBusinessFailureAsync(context, ResolveChallengeResponse(context));
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await WriteBusinessFailureAsync(context, ApiResponse<string>.Forbidden());
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    /// <summary>
    ///     Access Token 过期返回 <see cref="ResponseCode.TokenExpired"/>，其余挑战返回 Unauthorized。
    /// </summary>
    private static ApiResponse<string> ResolveChallengeResponse(HttpContext context)
    {
        var failure = UnwrapAuthenticateFailure(ResolveAuthenticateFailure(context));

        return failure switch
        {
            SecurityTokenExpiredException => ApiResponse<string>.TokenExpired("令牌已过期，请刷新令牌"),
            SecurityTokenInvalidIssuerException => ApiResponse<string>.Unauthorized("令牌发行者无效"),
            SecurityTokenInvalidAudienceException => ApiResponse<string>.Unauthorized("令牌受众无效"),
            SecurityTokenException tokenException => ApiResponse<string>.Unauthorized(
                string.IsNullOrWhiteSpace(tokenException.Message)
                    ? "认证失败，令牌格式错误或无效"
                    : tokenException.Message),
            _ => ApiResponse<string>.Unauthorized(
                string.IsNullOrEmpty(context.Request.Headers.Authorization)
                    ? "认证失败,未提供认证令牌"
                    : "认证失败，令牌格式错误或无效")
        };
    }

    /// <summary>
    ///     优先读取 JwtBearer OnAuthenticationFailed 挂载的异常；否则回退到 AuthenticateResult.Failure。
    /// </summary>
    private static Exception? ResolveAuthenticateFailure(HttpContext context)
    {
        if (context.Items.TryGetValue(AuthConstants.AuthenticateFailureItemKey, out var item)
            && item is Exception fromItems)
            return fromItems;

        return context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult?.Failure;
    }

    /// <summary>
    ///     JwtBearer 有时会把过期异常包在外层 Exception 中，需解包后再匹配业务码。
    /// </summary>
    private static Exception? UnwrapAuthenticateFailure(Exception? failure)
    {
        while (failure is not null)
        {
            if (failure is SecurityTokenExpiredException
                or SecurityTokenInvalidIssuerException
                or SecurityTokenInvalidAudienceException)
                return failure;

            if (failure.InnerException is null)
                return failure;

            failure = failure.InnerException;
        }

        return null;
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
