using System.Text.Json;
using System.Text.Json.Serialization;
using Application;
using Application.interfaces;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.Extensions.Options;
using Shared.Common;
using SkyRoc.Extensions;
using SkyRoc.Services;

var builder = WebApplication.CreateBuilder(args);


// 📋 获取配置
var configuration = builder.Configuration;

builder.Services.Configure<DevSeedOptions>(configuration.GetSection(DevSeedOptions.SectionName));
builder.Services.AddSingleton<IFileStoragePathProvider, WebHostFileStoragePathProvider>();

// ========================================
// 1️⃣ 添加服务到容器
// ========================================

// 注册 IHttpContextAccessor
// builder.Services.AddHttpContextAccessor();

// 添加基础设施层服务
builder.Services.AddInfrastructureServices(configuration, builder.Environment);

// 添加应用层服务
builder.Services.AddApplicationServices(configuration);

//添加鉴权服务
builder.Services.AddAuthenticationServices(configuration);

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        var jsonOptions = options.JsonSerializerOptions;
        // 1. 枚举转数字（核心配置）
        /*jsonOptions.Converters.Add(new EnumToNumberConverterFactory());*/
        // 2. 属性命名策略
        jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        // 3. 忽略null值属性
        jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // 4. 允许尾随逗号
        jsonOptions.AllowTrailingCommas = true;
        // 5. 只读属性序列化
        jsonOptions.IncludeFields = false;
        // 6. 大小写不敏感
        jsonOptions.PropertyNameCaseInsensitive = true;
        // 7. 数字处理
        jsonOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        // 8. 循环引用处理（.NET 9.0新特性）
        jsonOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // 9. 最大深度限制
        jsonOptions.MaxDepth = 64;
        // 10.格式化输出（开发环境）
        jsonOptions.WriteIndented = true; // 格式化输出（开发环境）
    });

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

// ========================================
// 2️⃣ 构建应用
// ========================================

var app = builder.Build();

// 初始化种子数据
// using (var scope = app.Services.CreateScope())
// {
//     var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//     var devSeedOptions = scope.ServiceProvider.GetRequiredService<IOptions<DevSeedOptions>>();
//     await DbSeeder.SeedAsync(context, app.Environment, devSeedOptions);
// }


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyRoc API v1");
        options.RoutePrefix = string.Empty;
    });
}

// ========================================
// 3️⃣ 配置HTTP请求管道
// ========================================
// 1. 使用全局异常处理（必须在最前面）
app.UseExceptionHandling();
// 2. HTTPS 重定向
app.UseHttpsRedirection();
// 3. ✅ 路由 - 必须有！
app.UseRouting();
// 使用认证中间件
app.UseAuthentication();
//  使用授权中间件
app.UseAuthorization();
// 使用控制器路由
app.MapControllers();
// 健康检查端点
app.MapHealthChecks("/health");

app.Run();

/// <summary>
///     Web 应用入口，供 API 集成测试创建测试宿主。
/// </summary>
public partial class Program;
