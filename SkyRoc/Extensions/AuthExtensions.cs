using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Application.interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Extensions;

/// <summary>
///     鉴权扩展
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    ///     注册 JWT Bearer 认证、稳定权限码策略和资源操作授权处理器。
    /// </summary>
    /// <param name="services">应用服务注册集合。</param>
    /// <param name="configuration">提供 JWT 发行者、受众和签名密钥的应用配置。</param>
    /// <returns>完成认证与授权注册后的原服务集合。</returns>
    /// <exception cref="InvalidOperationException">JWT 配置缺失或签名密钥不足 32 个 UTF-8 字节时抛出。</exception>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT 鉴权配置
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        if (jwtSettings is null)
            throw new InvalidOperationException("JwtSettings is not configured.");
        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
            throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");
        if (Encoding.UTF8.GetByteCount(jwtSettings.SecretKey) < 32)
            throw new InvalidOperationException("JwtSettings:SecretKey must contain at least 32 UTF-8 bytes.");
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // ⭐ 验证签名密钥
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    // ⭐ 验证发行者和受众
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    // ⭐ 验证过期时间
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero, // ⭐ 移除 5 分钟时间偏移

                    // ⭐ 其他验证
                    RequireExpirationTime = true,
                    ValidateTokenReplay = false
                };
                options.Events = new JwtBearerEvents
                {
                    // 认证失败
                    OnAuthenticationFailed = OnAuthenticationFailed,
                    // 需要认证但未提供令牌
                    OnChallenge = OnChallenge,
                    // 禁止访问（有令牌但权限不足）
                    OnForbidden = OnForbidden,
                    // 令牌验证成功处理
                    OnTokenValidated = OnTokenValidated,
                    // 从消息中读取令牌
                    OnMessageReceived = OnMessageReceived
                };
            });

        services.AddAuthorization(options =>
        {
            foreach (var permissionCode in PermissionCodes.Defined)
                options.AddPolicy(permissionCode, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new PermissionRequirement(permissionCode));
                });
        });
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, ResourcePermissionAuthorizationHandler>();

        return services;
    }

    /// <summary>
    ///     认证失败时（Token 无效、过期等）- 不直接返回错误
    /// </summary>
    /// <param name="context"></param>
    private static async Task OnAuthenticationFailed(AuthenticationFailedContext context)
    {
        //✅ 不要 context.NoResult()，让流程继续
        // ✅ 不要直接 WriteAsJsonAsync，让后续中间件处理


        await Task.CompletedTask;
    }

    /// <summary>
    ///     需要认证但未提供令牌
    /// </summary>
    /// <param name="context"></param>
    private static async Task OnChallenge(JwtBearerChallengeContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogWarning(
            "Authorization challenge for path: {Path}",
            context.Request.Path);
        context.HandleResponse();
        if (context.Response.HasStarted)
            return;

        context.Response.ContentType = "application/json; charset=utf-8";

        if (context.AuthenticateFailure != null)
        {
            await HandleAuthenticationFailure(context, context.AuthenticateFailure);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            await context.Response.WriteAsJsonAsync(
                ApiResponse<string>.Unauthorized("认证失败,未提供认证令牌"));
            return;
        }

        await context.Response.WriteAsJsonAsync(
            ApiResponse<string>.Unauthorized("认证失败，令牌格式错误或无效"));
    }

    /// <summary>
    ///     处理认证失败异常，并写入与业务码一致的 HTTP 状态码。
    /// </summary>
    private static Task HandleAuthenticationFailure(JwtBearerChallengeContext context, Exception exception)
    {
        return exception switch
        {
            // ⭐ 1. 令牌过期 - 最常见的情况
            SecurityTokenExpiredException => WriteUnauthorizedChallengeAsync(
                context,
                "令牌已过期，请重新登录或刷新令牌"),
            // 5. 发行者无效
            SecurityTokenInvalidIssuerException => WriteUnauthorizedChallengeAsync(
                context,
                "令牌发行者无效"),
            // 6. 受众无效
            SecurityTokenInvalidAudienceException => WriteUnauthorizedChallengeAsync(
                context,
                "令牌受众无效"),
            // 7. 其他安全令牌异常（含注销后缓存失效）
            SecurityTokenException => WriteUnauthorizedChallengeAsync(context, exception.Message),
            // 8. 其他未知异常
            _ => WriteChallengeAsync(
                context,
                StatusCodes.Status500InternalServerError,
                new ApiResponse<string>
                {
                    Code = ResponseCode.InternalError,
                    Msg = "认证过程中出现未知错误"
                })
        };
    }

    /// <summary>
    ///     权限不足处理
    /// </summary>
    /// <param name="context"></param>
    private static async Task OnForbidden(ForbiddenContext context)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            var result = ApiResponse<string>.Forbidden();
            await context.Response.WriteAsJsonAsync(result);
        }
    }

    private static Task WriteUnauthorizedChallengeAsync(JwtBearerChallengeContext context, string message)
    {
        return WriteChallengeAsync(
            context,
            StatusCodes.Status401Unauthorized,
            ApiResponse<string>.Unauthorized(message));
    }

    private static Task WriteChallengeAsync(
        JwtBearerChallengeContext context,
        int statusCode,
        ApiResponse<string> payload)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(payload);
    }

    /// <summary>
    ///     令牌签名与声明验证通过后，再核对访问令牌缓存：注销或吊销后的 jti 不得继续访问受保护接口。
    /// </summary>
    private static async Task OnTokenValidated(TokenValidatedContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                  ?? context.Principal?.FindFirst("jti")?.Value;
        if (string.IsNullOrWhiteSpace(jti))
        {
            logger.LogWarning("Rejected access token without jti for path {Path}", context.Request.Path);
            context.Fail(new SecurityTokenException("访问令牌缺少 jti"));
            return;
        }

        var tokenCache = context.HttpContext.RequestServices.GetRequiredService<ITokenCacheService>();
        if (!await tokenCache.IsAccessTokenValidAsync(jti))
        {
            logger.LogWarning(
                "Rejected revoked or missing access token jti {Jti} for path {Path}",
                jti,
                context.Request.Path);
            context.Fail(new SecurityTokenException("访问令牌已失效，请重新登录"));
            return;
        }

        var userName = context.Principal?.Identity?.Name ?? "Unknown";
        logger.LogDebug("JWT validated for user {UserName}", userName);
    }

    /// <summary>
    ///     从消息中读取令牌（支持查询字符串）
    /// </summary>
    private static Task OnMessageReceived(MessageReceivedContext context)
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        // SignalR 场景：从查询字符串读取 Token
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
        {
            context.Token = accessToken;
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("JWT read from query string for hub path {Path}", path);
        }

        return Task.CompletedTask;
    }
}
