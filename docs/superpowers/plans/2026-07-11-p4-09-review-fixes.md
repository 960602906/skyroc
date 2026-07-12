# P4-09 Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 P4-09 操作/登录审计、日志检索和分层映射评审问题，并用回归测试锁定安全边界。

**Architecture:** 操作审计继续位于认证授权之后，但以已认证身份作为启用条件；异常响应和审计共享一个 HTTP 状态映射。登录审计通过 Application 自有请求来源抽象获取 Web 信息，以单一出口覆盖登录全流程；系统响应映射交给 AutoMapper。

**Tech Stack:** .NET 9、ASP.NET Core、EF Core 9、AutoMapper 12、xUnit 2.9、EF Core InMemory。

## Global Constraints

- 只修复 P4-09，不推进 P4-10。
- 不读取或保存请求正文、响应正文、密码、令牌或密钥。
- Application 新代码只能依赖 Domain、Shared 和 Application 自有抽象。
- 机械实体到响应 DTO 映射必须使用 AutoMapper。
- 新增或修改公共契约必须使用中文 XML 文档。
- 所有行为变更必须先有失败测试，再写最小实现。

---

### Task 1: 收紧操作审计安全边界

**Files:**
- Modify: `SkyRoc.Tests/SystemSupport/SystemSupportContractTests.cs`
- Modify: `SkyRoc/Middleware/OperationAuditMiddleware.cs`
- Create: `SkyRoc/Middleware/ExceptionHttpStatusMapper.cs`
- Modify: `SkyRoc/Middleware/ExceptionHandlingMiddleware.cs`

**Interfaces:**
- Produces: `ExceptionHttpStatusMapper.GetStatusCode(Exception exception) : int`
- Produces: `OperationAuditMiddleware.ShouldAudit(HttpContext context) : bool`（私有）

- [ ] **Step 1: 写操作审计失败回归测试**

在 `SystemSupportContractTests` 增加测试：未认证 `POST /api/auth/login` 不调用审计服务；已认证请求仍记录；`apiKey`、`access_key`、`private-key` 被替换为 `***`，`keyword` 保留原值。

```csharp
[Fact]
public async Task OperationAuditMiddleware_SkipsAnonymousWrites()
{
    var (middleware, context, sink) = CreateAuditMiddleware(HttpMethods.Post, "/api/auth/login", authenticated: false);
    await middleware.InvokeAsync(context);
    Assert.Empty(sink.Entries);
}

[Theory]
[InlineData("apiKey")]
[InlineData("access_key")]
[InlineData("private-key")]
public async Task OperationAuditMiddleware_RedactsKeyParameters(string parameterName)
{
    var (middleware, context, sink) = CreateAuditMiddleware(HttpMethods.Post, "/api/notices", authenticated: true);
    context.Request.QueryString = new QueryString($"?{parameterName}=sensitive&keyword=delivery");
    await middleware.InvokeAsync(context);
    var summary = Assert.Single(sink.Entries).RequestSummary;
    Assert.Contains($"{parameterName}=***", summary);
    Assert.Contains("keyword=delivery", summary);
    Assert.DoesNotContain("sensitive", summary);
}
```

- [ ] **Step 2: 运行测试并确认 RED**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --no-build --filter "FullyQualifiedName~OperationAuditMiddleware"`

Expected: 匿名写请求测试和密钥参数测试失败，因为当前仅判断方法/路径且不识别 key。

- [ ] **Step 3: 写异常状态一致性失败测试**

组合 `ExceptionHandlingMiddleware` 与 `OperationAuditMiddleware`，让下游抛出 `NotFoundException`，断言响应状态与 `ResponseSummary` 都是 404；对 `ValidationException` 断言 422，对未知异常断言 500。

```csharp
Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
Assert.Equal("HTTP 404", Assert.Single(sink.Entries).ResponseSummary);
```

- [ ] **Step 4: 运行状态测试并确认 RED**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --no-build --filter "FullyQualifiedName~OperationAuditMiddleware_RecordsFinalHttpStatus"`

Expected: FAIL，当前异常审计统一写 500，异常处理中间件未设置 HTTP 状态。

- [ ] **Step 5: 实现最小安全修复**

`ShouldAudit` 接收 `HttpContext` 并要求 `User.Identity.IsAuthenticated == true`。敏感名按 camelCase、下划线和连字符分词，匹配 `password`、`token`、`secret`，或末尾独立词 `key`，避免命中 `keyword`。新增共享异常映射并同时用于两个中间件。

```csharp
private static bool ShouldAudit(HttpContext context) =>
    context.User.Identity?.IsAuthenticated == true &&
    context.Request.Path.StartsWithSegments("/api") &&
    context.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE";
```

- [ ] **Step 6: 运行 Task 1 测试并确认 GREEN**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --filter "FullyQualifiedName~OperationAuditMiddleware"`

Expected: PASS。

### Task 2: 解耦 Web 请求上下文并完整记录登录结果

**Files:**
- Create: `Application/DTOs/System/AuditRequestSource.cs`
- Create: `Application/interfaces/System/IAuditRequestSourceAccessor.cs`
- Create: `SkyRoc/Services/HttpAuditRequestSourceAccessor.cs`
- Modify: `Application/Services/System/LoginAuditService.cs`
- Modify: `Application/Services/AuthService.cs`
- Modify: `SkyRoc.Tests/SystemSupport/SystemSupportContractTests.cs`
- Create: `SkyRoc.Tests/Authorization/AuthLoginAuditTests.cs`

**Interfaces:**
- Produces: `IAuditRequestSourceAccessor.GetCurrent() : AuditRequestSource`
- `AuditRequestSource` contains `string IpAddress` and `string? UserAgent`.

- [ ] **Step 1: 写分层和登录全流程失败测试**

增加架构测试，确保 `LoginAuditService` 构造函数不再依赖 `IHttpContextAccessor`。增加认证服务测试：用户不存在、密码错误、角色仓储异常、缓存异常、成功各调用登录审计恰好一次；审计自身抛错不改变原始返回或异常。

```csharp
Assert.DoesNotContain(typeof(IHttpContextAccessor),
    typeof(LoginAuditService).GetConstructors().Single().GetParameters().Select(x => x.ParameterType));
```

- [ ] **Step 2: 运行测试并确认 RED**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --no-build --filter "FullyQualifiedName~AuthLoginAudit|FullyQualifiedName~LoginAuditService"`

Expected: FAIL，当前服务依赖 `IHttpContextAccessor`，中途异常不会审计。

- [ ] **Step 3: 实现请求来源抽象**

Application 定义只读来源 DTO 与访问接口；SkyRoc 实现接口并读取当前 `HttpContext`。实现类以 `*Service` 命名或在 Web 组合根显式注册为 scoped，确保 Scrutor/DI 可解析。

```csharp
public sealed record AuditRequestSource(string IpAddress, string? UserAgent);

public interface IAuditRequestSourceAccessor
{
    AuditRequestSource GetCurrent();
}
```

- [ ] **Step 4: 将登录流程改为单一审计出口**

`LoginAsync` 保存 `userId`、`isSuccess` 和安全失败摘要，在 `finally` 中调用吞错审计。凭据失败摘要为“用户不存在或密码错误”，基础设施或流程异常摘要为“登录处理失败”，成功摘要为空；每次流程只记录一次。

- [ ] **Step 5: 运行 Task 2 测试并确认 GREEN**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --filter "FullyQualifiedName~AuthLoginAudit|FullyQualifiedName~SystemSupportContractTests"`

Expected: PASS。

### Task 3: 增加日志内容检索

**Files:**
- Modify: `Application/QueryParameters/System/OperationLogQueryParameters.cs`
- Modify: `Application/QueryParameters/System/LoginLogQueryParameters.cs`
- Modify: `Application/Services/System/SystemSupportService.cs`
- Modify: `SkyRoc.Tests/SystemSupport/SystemSupportContractTests.cs`
- Modify: `docs/business-flows/12-system.md`
- Modify: `SkyRoc/SkyRoc.http`

**Interfaces:**
- Produces: 两个查询参数类型新增 `string? Keyword { get; set; }`。

- [ ] **Step 1: 写内容检索失败测试**

插入两条不同描述、URL、错误摘要、用户名和 User-Agent 的日志，分别以不同大小写关键字查询，断言只返回匹配记录。

- [ ] **Step 2: 运行测试并确认 RED**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --no-build --filter "FullyQualifiedName~AuditLogs_FilterByKeyword"`

Expected: 编译失败或断言失败，因为查询参数没有 `Keyword`。

- [ ] **Step 3: 实现不区分大小写的已有字段检索**

规范化关键字为小写，操作日志匹配 `Desc/Url/RequestParams/ResponseResult/ErrorMessage/CreateName`，登录日志匹配 `Username/FailureReason/IpAddress/UserAgent`。对可空字段先判空，使用 `ToLower().Contains(keyword)` 以保持 PostgreSQL 可翻译性。

- [ ] **Step 4: 同步 XML、流程文档与 HTTP 示例**

两个 `Keyword` 属性添加中文 XML 注释；文档与请求示例增加 `keyword` 参数，不宣称检索请求或响应正文。

- [ ] **Step 5: 运行 Task 3 测试并确认 GREEN**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --filter "FullyQualifiedName~AuditLogs"`

Expected: PASS。

### Task 4: 使用 AutoMapper 并统一审计文本裁剪

**Files:**
- Create: `Application/Mappers/SystemSupportMappingProfile.cs`
- Create: `Application/Services/System/AuditTextSanitizer.cs`
- Modify: `Application/Services/System/SystemSupportService.cs`
- Modify: `Application/Services/System/OperationAuditService.cs`
- Modify: `Application/Services/System/LoginAuditService.cs`
- Create: `SkyRoc.Tests/Mapping/SystemSupportMappingProfileTests.cs`
- Modify: `SkyRoc.Tests/SystemSupport/SystemSupportContractTests.cs`

**Interfaces:**
- Produces: AutoMapper maps `ServicePeriod/Notice/OperationLog/LoginLog` 到对应 DTO。
- Produces: `AuditTextSanitizer.Required(string?, int, string)` 与 `Optional(string?, int)`（internal）。

- [ ] **Step 1: 写映射与裁剪失败测试**

配置只加载 `SystemSupportMappingProfile` 并执行 `AssertConfigurationIsValid()`；验证四种实体映射关键字段。通过审计服务验证超长文本被裁剪、空必填值使用指定回退值。

- [ ] **Step 2: 运行测试并确认 RED**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --no-build --filter "FullyQualifiedName~SystemSupportMappingProfile|FullyQualifiedName~AuditTextSanitizer"`

Expected: 编译失败，因为 Profile 和统一裁剪器尚不存在。

- [ ] **Step 3: 实现 Profile 并替换手工 Map**

向 `SystemSupportService` 注入 `IMapper`，集合使用 `mapper.Map<List<TDto>>(data)`，单项使用 `mapper.Map<TDto>(entity)`；删除四个私有 `Map` 方法。

```csharp
CreateMap<ServicePeriod, ServicePeriodDto>();
CreateMap<Notice, NoticeDto>();
CreateMap<OperationLog, OperationLogDto>();
CreateMap<LoginLog, LoginLogDto>();
```

- [ ] **Step 4: 实现统一文本裁剪器**

两个审计服务调用同一 internal 工具，保留操作日志必填空值回退“未知”和登录名空值回退空字符串的既有行为。

- [ ] **Step 5: 运行 Task 4 测试并确认 GREEN**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --filter "FullyQualifiedName~SystemSupportMappingProfile|FullyQualifiedName~SystemSupportContractTests"`

Expected: PASS。

### Task 5: 全量验证与断点同步

**Files:**
- Modify: `docs/开发进度.md`（仅更新 P4-09 复审说明与测试基线）

**Interfaces:**
- Consumes: Task 1–4 的全部修复。

- [ ] **Step 1: 运行格式检查**

Run: `dotnet format SkyRoc.sln --verify-no-changes`

Expected: exit 0；如仅本次文件不符合格式，执行 `dotnet format SkyRoc.sln` 后重新验证。

- [ ] **Step 2: 运行构建**

Run: `dotnet build SkyRoc.sln --no-restore`

Expected: 0 warnings、0 errors。

- [ ] **Step 3: 运行 P4-09 定向测试**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --no-build --filter "FullyQualifiedName~SystemSupport|FullyQualifiedName~AuthLoginAudit|FullyQualifiedName~SystemSupportMappingProfile"`

Expected: 0 failures。

- [ ] **Step 4: 运行全量测试**

Run: `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --no-build`

Expected: 0 failures。

- [ ] **Step 5: 核对 EF 模型**

Run: `dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project SkyRoc --no-build`

Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 6: 更新开发进度**

记录 P4-09 复审修复项、最终测试数量、构建警告数和迁移一致性结果；当前断点仍保持 P4-10。
