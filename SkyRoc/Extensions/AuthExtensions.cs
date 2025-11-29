using System.Text;
using Common.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SkyRoc.Extensions;

/// <summary>
///     鉴权扩展
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    ///     鉴权
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT 鉴权配置
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();

        if (jwtSettings is null) throw new Exception("JwtSettings is null");
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
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<JwtBearerHandler>>();
        logger.LogWarning("Token invalid");
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
        if (!context.Response.HasStarted)
        {
            // var token = context.Request.Headers["Authorization"].ToString();
            context.Response.ContentType = "application/json; charset=utf-8";
            var result = ApiResponse<string>.Unauthorized("认证失败，请提供有效的 Token");
            await context.Response.WriteAsJsonAsync(result);
        }
    }

    /// <summary>
    ///     权限不足处理
    /// </summary>
    /// <param name="context"></param>
    private static async Task OnForbidden(ForbiddenContext context)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            var result = ApiResponse<string>.Forbidden();
            await context.Response.WriteAsJsonAsync(result);
        }
    }

    /// <summary>
    ///     令牌验证成功处理
    /// </summary>
    private static Task OnTokenValidated(TokenValidatedContext context)
    {
        var userName = context.Principal?.Identity?.Name ?? "Unknown";
        Console.WriteLine($"Token validated for: {userName}");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     从消息中读取令牌（支持查询字符串）
    /// </summary>
    private static Task OnMessageReceived(MessageReceivedContext context)
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        // SignalR 场景：从查询字符串读取 Token
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs")) context.Token = accessToken;
        Console.WriteLine($"Received message: {accessToken}");
        return Task.CompletedTask;
    }
}