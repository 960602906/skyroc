# Repository Guidelines

## Project Structure & Module Organization

`SkyRoc.sln` contains six .NET 9 projects:

- `SkyRoc/`: ASP.NET Core entry point, controllers, middleware, configuration, and HTTP samples.
- `Application/`: DTOs, validators, mapping profiles, application services, and business exceptions.
- `Domain/`: entities and repository interfaces; keep this layer independent of infrastructure concerns.
- `Infrastructure/`: EF Core context, entity configurations, repositories, migrations, caching, and seed data.
- `Shared/`: cross-cutting constants, response models, options, and utilities.
- `SkyRoc.Tests/`: xUnit tests grouped by feature (`Mapping/`, `Caching/`, `Customers/`, and `Serialization/`).

Add code to the narrowest layer and preserve dependency direction.

## Development Continuity

Before feature work, read `docs/开发进度.md` and `docs/自动开发任务清单.md`; complete only the first unchecked task. For automated business testing, also read `docs/测试进度.md` and `docs/自动测试任务清单.md`. Before handoff, update implementation breakpoints in `docs/开发进度.md` and validation evidence in `docs/测试进度.md` separately.

## Build, Test, and Development Commands

Run commands from the repository root:

```powershell
dotnet restore SkyRoc.sln
dotnet build SkyRoc.sln --no-restore
dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --no-build
dotnet run --project SkyRoc\SkyRoc.csproj --launch-profile http
dotnet format SkyRoc.sln --verify-no-changes
```

The API runs at `http://localhost:5293`; Swagger is at the root in Development. Configure PostgreSQL first. Redis can fall back to memory locally.

## Coding Style & Naming Conventions

Use four-space indentation: PascalCase for types, methods, and public members; camelCase for parameters and locals; and `I` prefixes for interfaces. Nullable reference types and implicit usings are enabled. Keep exactly one top-level type per file and match its filename; private nested test helpers may remain with their owning test. Group related types with domain folders instead of aggregate files. Follow suffixes such as `*Controller`, `*Service`, `*Repository`, `*Dto`, `*Validator`, and `*Tests`. Use async APIs for I/O and include the `Async` suffix where established.

For pure derived response values that depend only on fields of the same object, prefer read-only computed properties instead of storing or mapping duplicate state. All quantity, price, cost, and amount calculations must use `Shared.Constants.NumericPrecision`; do not hardcode decimal scales or midpoint rounding modes in services, mappings, or DTOs.

Use AutoMapper for direct structural mappings that contain no business decisions. Do not hide validation, repository lookups, source precedence, snapshot selection, state transitions, or `NumericPrecision` calculations in mapping profiles, custom resolvers, or `AfterMap`; construct such entities explicitly in the application service or a dedicated factory, and reserve AutoMapper for the mechanical mapping portions and response models. When an explicit construction helper develops a long positional parameter list—especially repeated `Guid`, `string`, or `decimal` values—replace those arguments with a named parameter object such as an internal `*Snapshot` or `*BuildContext`, or extract a focused factory. Prefer named property initialization so source fields remain reviewable and cannot be silently swapped.

## Documentation Comment Requirements

Comments are part of the definition of done, not optional cleanup. Add concise XML documentation to domain entities and their public properties, repository interfaces and implementations, application service interfaces and implementations, controllers, actions, and public request/response members. Comments must explain business meaning, units, status semantics, side effects, and important constraints instead of merely repeating the identifier.

For every new or changed persisted entity, configure PostgreSQL table and column comments through EF Core (`HasComment`) and include those comments in the generated migration. Review migrations to ensure comments are added, changed, and rolled back correctly. Add focused inline comments only where non-obvious private logic or state transitions need explanation. New or modified code missing required comments is incomplete and must not be handed off.

## Testing Guidelines

Tests use xUnit, EF Core InMemory, and coverlet. Name test classes after the subject and test methods as behavior statements, for example `GetByIdAsync_ReturnsCustomer_WhenCustomerExists`. Place regression tests beside the matching feature folder. Run the full test project before submitting; no numeric coverage threshold is currently enforced.

## Commit & Pull Request Guidelines

Recent history contains brief Chinese and English summaries. Prefer a specific imperative subject over generic messages such as `提交`, for example `修复客户查询分页` or `Add Redis fallback tests`. Keep commits focused. Pull requests should describe the behavior change, affected layers, configuration or migration impact, and test evidence. Link related issues and include request/response examples or Swagger screenshots when API behavior changes.

## Security & Configuration

Do not commit production connection strings, JWT signing keys, or seed passwords. Override `SkyRoc/appsettings.json` with environment variables or an untracked development configuration. Review generated EF Core migrations before committing them.

---

## Supplement: Tech Stack

SkyRoc is a fresh-produce supply-chain backend using **Clean Architecture**. Business docs and XML comments are primarily **Chinese**; keep new comments in Chinese to match the codebase.

| Category | Technology |
| --- | --- |
| Runtime | .NET 9, C# 13 (`net9.0`, nullable + implicit usings) |
| Web | ASP.NET Core Web API, Swashbuckle.AspNetCore 9, Microsoft.AspNetCore.OpenApi |
| Data | EF Core 9, PostgreSQL (Npgsql), code-first migrations |
| Cache | Redis (StackExchange.Redis) with in-memory fallback |
| Auth | JWT Bearer, policy + resource/action permission handlers |
| Validation | FluentValidation 12 |
| Mapping | AutoMapper 12 |
| DI | Scrutor assembly scanning |
| Testing | xUnit 2.9, EF Core InMemory, coverlet, `WebApplicationFactory<Program>` |

Additional reference: `CLAUDE.md`, `docs/business-flows/`.

## Supplement: Project Initialization

After clone, from repository root:

```powershell
dotnet restore SkyRoc.sln
dotnet build SkyRoc.sln --no-restore
```

**Environment**

- PostgreSQL — `ConnectionStrings:DefaultConnection` is required at startup.
- Redis — optional; set `Redis:Enabled=false` or omit Redis to use in-memory fallback.

**Configuration overrides** (prefer over editing committed secrets):

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=skyroc;Username=...;Password=..."
$env:Redis__Enabled = "false"
```

**OpenAPI JSON** (Development): `http://localhost:5293/swagger/v1/swagger.json`

**EF Core migrations** (design-time factory: `Infrastructure/Data/DbContextFactory.cs`):

```powershell
dotnet ef migrations add <Name> --project Infrastructure --startup-project SkyRoc
dotnet ef database update --project Infrastructure --startup-project SkyRoc
```

**Single test filter:**

```powershell
dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --filter "FullyQualifiedName~OrderServiceTests"
```

## Supplement: Architecture Overview

### Request flow

`Controller` → application service → repository (`IUnitOfWork` / `SaveChangesAsync`) → `ApplicationDbContext`.

Controllers stay thin: one service call, wrap in `ApiResponse<T>.Ok(...)`.

### DI (Scrutor)

New services and repositories are auto-registered when naming conventions match — no manual `AddScoped` needed:

- `Application.Services.*Service` → scoped (`Application/DependencyInjection.cs`)
- `Infrastructure.Repositories.*Repository` → scoped (`Infrastructure/DependencyInjection.cs`)
- FluentValidation validators and AutoMapper profiles — scanned from Application

Composition roots: `AddApplicationServices`, `AddInfrastructureServices`, `AddAuthenticationServices` (`SkyRoc/Extensions/AuthExtensions.cs`).

### Authorization

Permission codes: `module:resource:action` (`Shared/Constants/PermissionCodes.cs`).

```csharp
[Authorize]
[PermissionResource(PermissionCodes.Business.Orders.Resource)]
public class OrdersController(ISaleOrderService service) : ControllerBase
{
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<SaleOrderDto>>> GetById(Guid id) { ... }
}
```

JWT `permission` claims; admin wildcard `*:*:*`. Token state in Redis/memory via `ICacheService`.

## Supplement: Swagger / OpenAPI Documentation

OpenAPI is generated by **Swashbuckle** in Development. Configuration: `SkyRoc/Extensions/SwaggerExtensions.cs`.

**Filters and conventions**

- `SwaggerAuthorizationOperationFilter` — Bearer security；在 description 中说明未认证/无权限对应 `body.code=401/403`（HTTP 仍为 200）
- `SwaggerEnumSchemaFilter` — lists integer enum values in schema description
- Business **Tags** — controller name mapped via `ControllerTags` dictionary (认证、系统权限、基础资料、定价、销售订单、采购、库存、配送)

**Regression tests:** `SkyRoc.Tests/Documentation/` (`SwaggerResponseSchemaTests`, `SwaggerAuthorizationOperationFilterTests`).

### HTTP status vs business code (required)

**业务接口一旦进入受理，HTTP 状态码一律为 `200`。业务成败只看响应体 `code`（`Shared.Constants.ResponseCode`），不要用 HTTP 4xx/5xx 表达业务结果。**

| 场景 | HTTP | body.`code` |
| --- | --- | --- |
| 成功 | 200 | `200` Success |
| 参数/模型错误 | 200 | `400` BadRequest |
| 未认证 | 200 | `401` Unauthorized |
| 无权限 | 200 | `403` Forbidden |
| 资源不存在 | 200 | `404` NotFound |
| 业务校验失败 | 200 | `422` ValidationError |
| 业务异常（`BusinessException`） | 200 | `502` DatabaseError |
| 未处理异常 | 200 | `500` InternalError |

实现约定（新增/修改接口必须遵守）：

- Controller **只** `return Ok(ApiResponse<T>.Ok(...))` / `Ok(ApiResponse<T>.BadRequest(...))` 等；**禁止** `BadRequest(...)`、`NotFound(...)`、`StatusCode(...)`、`Unauthorized(...)`、`Forbid()` 等会改写 HTTP 状态的写法。
- 业务失败优先抛 `NotFoundException` / `ValidationException` / `BusinessException`，由 `ExceptionHandlingMiddleware` 写成 HTTP 200 + body.`code`。
- 认证/授权失败由 `ApiAuthorizationMiddlewareResultHandler`（及 JWT 事件兜底）统一写成 HTTP 200 + `401`/`403`。
- 模型绑定失败由 `InvalidModelStateResponseFactory` 写成 HTTP 200 + `400`。
- 文件下载等非 `ApiResponse` 成功响应仍可为 HTTP 200；失败同样走异常中间件（HTTP 200 + body.`code`）。
- 健康检查等非业务契约端点可保留框架默认 HTTP 状态，不在此约定内。
- 集成测试断言业务结果时用 `ApiHttpAssert.AssertBusinessCodeAsync`（或读 body.`code`），**不要**断言 `HttpStatusCode.Unauthorized` / `Forbidden` / `BadGateway` 等。

### Controller return types (required)

**Do not** use `Task<IActionResult>` for API actions. Swashbuckle cannot infer `ApiResponse<T>` from `Ok(...)` inside `IActionResult`, which produces empty `200` response schemas and omits response DTOs from `components/schemas`.

**Use typed returns** matching the `Ok` payload:

```csharp
// Correct — HTTP 200 + ApiResponse；Swagger 能推断 schema
public async Task<ActionResult<ApiResponse<SaleOrderDto>>> GetById(Guid id)
{
    var result = await service.GetByIdAsync(id);
    return Ok(ApiResponse<SaleOrderDto>.Ok(result));
}

// Wrong — 用 HTTP 状态表达业务错误（禁止）
return BadRequest(ApiResponse<SaleOrderDto>.Fail("..."));
return NotFound(...);
return StatusCode(502, ...);

// Wrong — 200 response has no content schema
public async Task<IActionResult> GetById(Guid id) { ... }
```

`BaseDataController` generic actions must also use `ActionResult<ApiResponse<TDto>>`, etc.

### Response body contract

统一响应模型为 `Shared.Common.ApiResponse<T>`（HTTP 始终 200）：

```json
{ "code": 200, "msg": "操作成功", "data": { ... } }
```

失败示例：

```json
{ "code": 403, "msg": "无权限访问", "data": null }
```

- Do not return `ApiResponse<object>` with anonymous types — define a DTO (e.g. `GetRoutesResDto`).
- JSON property names are **camelCase** (`Program.cs` `JsonSerializerOptions`).
- 前端以 `VITE_SERVICE_SUCCESS_CODE`（通常为 `200`）判断成功，登出/刷新等看 body.`code` 配置，不依赖 HTTP 4xx。

### XML comments loaded into Swagger

| Assembly | Swagger purpose |
| --- | --- |
| `SkyRoc` | Controller/action summaries |
| `Application` | DTO and query parameter descriptions |
| `Domain` | Enum and entity summaries |
| `Shared` | `ApiResponse<T>`, `PagedResult<T>`, constants |

`SkyRoc.csproj` copies `Application.xml`, `Domain.xml`, `Shared.xml` to output on build. When exposing types from a new assembly in OpenAPI, extend `IncludeXmlCommentsFromAssemblies` and the XML copy target.

**DTO / action checklist for new or changed endpoints**

- Class-level `/// <summary>` on request/response DTOs
- Property-level summaries for non-obvious fields (units, enum meaning, requiredness)
- Controller: meaningful `/// <summary>`, `/// <param>`, `/// <returns>` — no empty placeholders

### DateTime and enums

- DateTime JSON uses **System.Text.Json defaults** (ISO 8601). Values with `DateTimeKind.Utc` serialize as `yyyy-MM-dd'T'HH:mm:ssZ`. Clients **must send UTC with `Z`** (or offset). Query/form binding still normalizes via `UtcDateTimeModelBinder` + `DateTimeJsonFormats`.
- Enums serialize as **integers**; use Domain enums in DTOs only when they are stable API contract values.

### New controller checklist

1. `[ApiController]`, `[Route(...)]`, permission attributes
2. `ActionResult<ApiResponse<T>>` on every action；成功/失败均 `return Ok(ApiResponse...)`（HTTP 200）
3. 不手写 HTTP 4xx/5xx；业务错误用异常或 `ApiResponse` 的 `code`
4. XML docs on class and actions
5. Add controller name → Tag in `SwaggerExtensions.ControllerTags` if not covered
6. Extend `SkyRoc.Tests/Documentation/` when adding routes or response shapes
7. Update `SkyRoc.http` / stage API docs when the task requires contract sync

### Common Swagger mistakes

| Mistake | Effect |
| --- | --- |
| `return BadRequest` / `NotFound` / `StatusCode(4xx\|5xx)` | 违反「HTTP 一律 200」约定；前端与审计按 body.`code` 判断会失真 |
| `Task<IActionResult>` | Empty 200 schema; `ApiResponse` wrapper invisible |
| Only `SkyRoc.xml` loaded | DTO fields lack descriptions |
| Anonymous `new { ... }` responses | Unusable / empty schema |
| Empty `<param>` / `<returns>` | Noise in Swagger UI |
| Missing Tag mapping | Endpoints under wrong or generic tag |
| ISO date examples | Mismatch with actual JSON converters |
| Swagger 文档写死 HTTP 401/403 responses | 与真实契约不符；应在 description 中说明 body.`code` |

## Supplement: Testing (Swagger & Integration)

In addition to the Testing Guidelines above:

- Integration tests use `WebApplicationFactory<Program>` (`SkyRoc.Tests/` feature folders include `Orders/`, `Storage/`, `Authorization/`, `Documentation/`, etc.).
- **Swagger or API contract changes** — add assertions under `SkyRoc.Tests/Documentation/` (paths, tags, response `content`, schema presence, DTO descriptions).
