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

Before feature work, read `docs/开发进度.md` and `docs/自动开发任务清单.md`; complete only the first unchecked task. Before handoff, update the breakpoint and test baseline.

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

- `SwaggerAuthorizationOperationFilter` — Bearer security, 401/403, permission code in description
- `SwaggerDateTimeSchemaFilter` — documents `yyyy-MM-dd HH:mm:ss` string format for custom JSON converters
- `SwaggerEnumSchemaFilter` — lists integer enum values in schema description
- Business **Tags** — controller name mapped via `ControllerTags` dictionary (认证、系统权限、基础资料、定价、销售订单、采购、库存、配送)

**Regression tests:** `SkyRoc.Tests/Documentation/` (`SwaggerResponseSchemaTests`, `SwaggerAuthorizationOperationFilterTests`).

### Controller return types (required)

**Do not** use `Task<IActionResult>` for API actions. Swashbuckle cannot infer `ApiResponse<T>` from `Ok(...)` inside `IActionResult`, which produces empty `200` response schemas and omits response DTOs from `components/schemas`.

**Use typed returns** matching the `Ok` payload:

```csharp
// Correct — Swagger emits ApiResponseOfSaleOrderDto + SaleOrderDto schemas
public async Task<ActionResult<ApiResponse<SaleOrderDto>>> GetById(Guid id)
{
    var result = await service.GetByIdAsync(id);
    return Ok(ApiResponse<SaleOrderDto>.Ok(result));
}

// Wrong — 200 response has no content schema
public async Task<IActionResult> GetById(Guid id) { ... }
```

`BaseDataController` generic actions must also use `ActionResult<ApiResponse<TDto>>`, etc.

### Response body contract

Success payloads use `Shared.Common.ApiResponse<T>`:

```json
{ "code": 200, "msg": "操作成功", "data": { ... } }
```

- Do not return `ApiResponse<object>` with anonymous types — define a DTO (e.g. `GetRoutesResDto`).
- JSON property names are **camelCase** (`Program.cs` `JsonSerializerOptions`).

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

- Properties with `FixedDateTimeJsonConverter` / `FixedNullableDateTimeJsonConverter` serialize as **`yyyy-MM-dd HH:mm:ss`** (UTC), not ISO `date-time`.
- Enums serialize as **integers**; use Domain enums in DTOs only when they are stable API contract values.

### New controller checklist

1. `[ApiController]`, `[Route(...)]`, permission attributes
2. `ActionResult<ApiResponse<T>>` on every action
3. XML docs on class and actions
4. Add controller name → Tag in `SwaggerExtensions.ControllerTags` if not covered
5. Extend `SkyRoc.Tests/Documentation/` when adding routes or response shapes
6. Update `SkyRoc.http` / stage API docs when the task requires contract sync

### Common Swagger mistakes

| Mistake | Effect |
| --- | --- |
| `Task<IActionResult>` | Empty 200 schema; `ApiResponse` wrapper invisible |
| Only `SkyRoc.xml` loaded | DTO fields lack descriptions |
| Anonymous `new { ... }` responses | Unusable / empty schema |
| Empty `<param>` / `<returns>` | Noise in Swagger UI |
| Missing Tag mapping | Endpoints under wrong or generic tag |
| ISO date examples | Mismatch with actual JSON converters |

## Supplement: Testing (Swagger & Integration)

In addition to the Testing Guidelines above:

- Integration tests use `WebApplicationFactory<Program>` (`SkyRoc.Tests/` feature folders include `Orders/`, `Storage/`, `Authorization/`, `Documentation/`, etc.).
- **Swagger or API contract changes** — add assertions under `SkyRoc.Tests/Documentation/` (paths, tags, response `content`, schema presence, DTO descriptions).
