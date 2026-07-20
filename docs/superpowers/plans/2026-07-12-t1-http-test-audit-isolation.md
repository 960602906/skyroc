# T1 HTTP 测试审计隔离 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让所有非 PostgreSQL 的 HTTP 集成测试宿主不再可能向共享 PostgreSQL 联调库写入登录或操作审计记录。

**Architecture:** 在测试项目中集中提供 InMemory DbContext 与无副作用审计服务注册，并由每个普通 `WebApplicationFactory<Program>` 调用。真实 PostgreSQL 测试宿主保持当前真实依赖，以便后续任务验证实际审计数据。

**Tech Stack:** .NET 9、xUnit、ASP.NET Core `WebApplicationFactory`、EF Core InMemory、Microsoft.Extensions.DependencyInjection。

## Global Constraints

- 只连接已确认的 `skyroc` 专用 PostgreSQL 测试库；不得删除、重建、截断或清理无批次归属数据。
- 非 PostgreSQL HTTP 测试不得解析 Npgsql DbContext，也不得持久化登录或操作审计。
- 新增/修改代码使用中文 XML 文档；不新增迁移、不提交、不推送。
- 按 TDD 执行：先增加会失败的测试，再写最小实现并复跑。

---

### Task 1: 建立普通 HTTP 测试宿主的审计隔离

**Files:**
- Create: `SkyRoc.Tests/Testing/IsolatedWebTestServiceCollectionExtensions.cs`
- Modify: `SkyRoc.Tests/Orders/OrderApiIntegrationTests.cs`
- Modify: `SkyRoc.Tests/Purchases/PurchasePlanApiIntegrationTests.cs`
- Modify: `SkyRoc.Tests/Integration/MainBusinessFlowApiIntegrationTests.cs`
- Modify: `SkyRoc.Tests/Integration/FinanceAfterSaleFlowApiIntegrationTests.cs`
- Modify: `SkyRoc.Tests/Purchases/ProcurementFlowApiIntegrationTests.cs`
- Modify: `SkyRoc.Tests/Storage/StockInApiIntegrationTests.cs`
- Modify: `SkyRoc.Tests/Documentation/SwaggerDocumentationWebApplicationFactory.cs`

**Interfaces:**
- Consumes: `IServiceCollection`、`ApplicationDbContext`、`IOperationAuditService`、`ILoginAuditService`。
- Produces: `UseIsolatedInMemoryPersistence(string databaseName)`；注册 InMemory `ApplicationDbContext` 与不落库的两种审计服务。

- [ ] **Step 1: 写失败测试。** 在两个此前未替换 DbContext 的 API 工厂中断言解析出的 `ApplicationDbContext.Database.ProviderName` 为 `Microsoft.EntityFrameworkCore.InMemory`，并在 Swagger 工厂中作同样断言。
- [ ] **Step 2: 运行失败测试。** 执行 `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --filter "FullyQualifiedName~OrderApiIntegrationTests|FullyQualifiedName~PurchasePlanApiIntegrationTests|FullyQualifiedName~SwaggerResponseSchemaTests"`；预期新增断言显示实际提供程序仍为 Npgsql。
- [ ] **Step 3: 写最小实现。** 增加集中扩展，移除原 `ApplicationDbContext`、`IOperationAuditService` 与 `ILoginAuditService` 注册，加入调用方给定名称的 InMemory 上下文和只返回完成任务的审计实现；七个普通 HTTP 测试宿主均调用该扩展。
- [ ] **Step 4: 运行通过测试。** 重复 Step 2 命令，确认新增断言和既有 API/Swagger 测试通过。

### Task 2: 真实库回归与 T1 验收

**Files:**
- Modify: `docs/自动测试任务清单.md`
- Modify: `docs/开发进度.md`
- Modify: `C:\Users\mrqin\.codex\automations\skyroc\memory.md`

- [ ] **Step 1: 记录库基线。** 生成一次真实 PostgreSQL 质量报告，记录逐表数量、临时批次残留和审计日志数量。
- [ ] **Step 2: 运行真实 T1 专项与完整套件。** 依次运行 Metadata/DataQuality/DatabaseDocumentation 专项、构建、无构建完整测试和格式检查；比较前后报告，要求无未归属审计日志增量。
- [ ] **Step 3: 生成交付报告。** 写入 JSON 与 Markdown 报告，确认全表/列规则、约束、注释、外键、重复编码、临时残留及业务一致性门禁均通过。
- [ ] **Step 4: 更新断点。** 仅在所有验证及基线保持通过时勾选 T1，写入测试数量、数据库基线与验收日期；否则保留 T1 未勾选并写入阻塞证据。
