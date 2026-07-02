using System.Reflection;
using Microsoft.OpenApi.Models;

namespace SkyRoc.Extensions;

/// <summary>
///     Swagger 配置扩展
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    ///     添加Swagger服务
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // 基本信息
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SkyRoc API",
                Version = "v1",
                Description = "SkyRoc 生鲜供应链后台 API。第一阶段包含认证、系统权限和基础资料接口。"
            });

            // 添加XML注释
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath, true);

            // 支持 Authorization header
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "输入登录接口返回的 AccessToken。"
            });
            options.OperationFilter<SwaggerAuthorizationOperationFilter>();
        });

        return services;
    }
}
