using System.Text.Json;
using System.Text.Json.Serialization;
using Application;
using Infrastructure;
using Infrastructure.Data;
using SkyRoc.Converter;
using WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);


// 📋 获取配置
var configuration = builder.Configuration;

// ========================================
// 1️⃣ 添加服务到容器
// ========================================

// 注册 IHttpContextAccessor
// builder.Services.AddHttpContextAccessor();

// 添加基础设施层服务
builder.Services.AddInfrastructureServices(configuration);

// 添加应用层服务
builder.Services.AddApplicationServices();

//添加鉴权服务
builder.Services.AddAuthenticationServices(configuration);

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 添加自定义日期时间转换器
        options.JsonSerializerOptions.Converters.Add(new CustomDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableCustomDateTimeConverter());
        // 忽略空值
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull;
        // 其他 JSON 配置（可选）
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; // 驼峰命名
        options.JsonSerializerOptions.WriteIndented = true; // 格式化输出（开发环境）
    });

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

// ========================================
// 2️⃣ 构建应用
// ========================================

var app = builder.Build();

// 初始化种子数据
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(context);
}


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Role Based Menu System API v1");
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

app.Run();