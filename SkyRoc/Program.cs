using Application;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.Extensions.Options;
using Shared.Common;
using SkyRoc.Extensions;
using SkyRoc.Middleware;
using SkyRoc.Services;

var builder = WebApplication.CreateBuilder(args);

// 本机可使用未纳入版本控制的配置文件保存临时开发密钥；后续配置源仍可覆盖这些值。
if (builder.Environment.IsDevelopment())
{
    builder.Configuration
        .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args);
}

// 📋 获取配置
var configuration = builder.Configuration;

builder.AddSkyRocObservability();

builder.Services.Configure<DevSeedOptions>(configuration.GetSection(DevSeedOptions.SectionName));
builder.Services.AddScoped<Application.Interfaces.System.IAuditRequestSourceAccessor, HttpAuditRequestSourceAccessor>();

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

// 添加控制器：业务接口受理后 HTTP 固定 200，结果码写在 body.code
builder.Services.AddSkyRocControllers(builder.Environment);

// 全局限流：按客户端 IP 的固定窗口保护，突发流量兜底
builder.Services.AddSkyRocRateLimiter();

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
app.UseSkyRocObservability();
// 2. HTTPS 重定向（开发环境常用 http profile，跳过以免无法解析 https 端口的警告）
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
// 3. ✅ 路由 - 必须有！
app.UseRouting();
// 限流：放在路由之后、认证之前，拦截突发流量
app.UseRateLimiter();
// 使用认证中间件
app.UseAuthentication();
//  使用授权中间件
app.UseAuthorization();
// 记录已通过认证和授权的关键写操作；失败不影响原业务响应。
app.UseMiddleware<OperationAuditMiddleware>();
// 使用控制器路由
app.MapControllers();
// 健康检查端点
app.MapHealthChecks("/health");

app.Run();

/// <summary>
///     Web 应用入口，供 API 集成测试创建测试宿主。
/// </summary>
public partial class Program;
