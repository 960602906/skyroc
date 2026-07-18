using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Filters;
using SkyRoc.Middleware;
using SkyRoc.ModelBinding;

namespace SkyRoc.Extensions;

/// <summary>
///     MVC 控制器与 JSON 序列化扩展。
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    ///     注册控制器：业务接口受理后 HTTP 固定 200，结果码写在 body.code；并配置统一 JSON 选项。
    /// </summary>
    /// <param name="services">应用服务注册集合。</param>
    /// <param name="environment">宿主环境，用于仅在开发环境美化 JSON。</param>
    /// <returns>完成控制器注册后的原服务集合。</returns>
    public static IServiceCollection AddSkyRocControllers(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services.AddControllers(options =>
            {
                // query/form 的 DateTime 统一绑为 UTC，避免 Npgsql timestamptz 拒绝 Unspecified
                options.ModelBinderProviders.Insert(0, new UtcDateTimeModelBinderProvider());
                options.Filters.Add<ApiBusinessCodeResultFilter>();
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(entry => entry.Value is { Errors.Count: > 0 })
                        .ToDictionary(
                            entry => entry.Key,
                            entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
                    var payload = new ApiResponse<object>
                    {
                        Code = ResponseCode.BadRequest,
                        Msg = "请求参数错误",
                        Data = errors
                    };
                    ApiResponseHttp.MarkBusinessCode(context.HttpContext, payload.Code);
                    return new OkObjectResult(payload);
                };
            })
            .AddJsonOptions(options =>
            {
                var jsonOptions = options.JsonSerializerOptions;
                jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                jsonOptions.AllowTrailingCommas = true;
                jsonOptions.IncludeFields = false;
                jsonOptions.PropertyNameCaseInsensitive = true;
                jsonOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
                jsonOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                jsonOptions.MaxDepth = 64;
                // 仅开发环境美化 JSON，生产关闭以降低序列化 CPU 与响应体积
                jsonOptions.WriteIndented = environment.IsDevelopment();
            });

        return services;
    }
}
