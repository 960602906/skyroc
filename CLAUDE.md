# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

SkyRoc is a .NET 9 ASP.NET Core Web API for a fresh-produce supply-chain backend, built with Clean Architecture. Most documentation and code comments are in Chinese; keep comments in Chinese to match.

The React 19 admin frontend lives in `client/` (SkyrocAdmin, pnpm/Vite/Ant Design). It has its own `client/CLAUDE.md` — read that before working on frontend code. This file covers the backend.

## Commands

Run from the repository root (`C:/Users/mrqin/RiderProjects/skyroc`, one level above the `SkyRoc/` API project):

```powershell
dotnet restore SkyRoc.sln
dotnet build SkyRoc.sln --no-restore
dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --no-build
dotnet run --project SkyRoc\SkyRoc.csproj --launch-profile http   # http://localhost:5293, Swagger at root
dotnet format SkyRoc.sln --verify-no-changes
```

Run a single test class or method with a filter:

```powershell
dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --filter "FullyQualifiedName~CustomerServiceTests"
dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --filter "DisplayName~GetByIdAsync_ReturnsCustomer_WhenCustomerExists"
```

EF Core migrations (design-time factory is `Infrastructure/Data/DbContextFactory.cs`; run with `--project Infrastructure --startup-project SkyRoc`):

```powershell
dotnet ef migrations add <Name> --project Infrastructure --startup-project SkyRoc
dotnet ef database update --project Infrastructure --startup-project SkyRoc
```

The app auto-runs migrations and dev seeding on startup. A valid `ConnectionStrings:DefaultConnection` (PostgreSQL) is required or startup throws; Redis is optional and falls back to in-memory.

## Architecture

Five layered projects plus tests, with strict dependency direction — **add code to the narrowest layer and never invert these arrows**:

- **`Domain/`** — entities, repository interfaces, `IUnitOfWork`, read models, queries. No infrastructure dependencies.
- **`Application/`** — application services (business logic), DTOs, FluentValidation validators, AutoMapper profiles (`Mappers/`), query parameters, business exceptions. Depends only on Domain + Shared.
- **`Infrastructure/`** — EF Core `ApplicationDbContext`, entity configurations (`Data/EntityConfigurations/`), repository implementations, migrations, caching, `DbSeeder`.
- **`Shared/`** — cross-cutting `ApiResponse<T>`, response/permission constants, options models, `NumericPrecision`, utilities. Referenced by all layers.
- **`SkyRoc/`** — Web API host: controllers, middleware, authorization handlers, Swagger, `Extensions/` (DI wiring), `Program.cs`.

### Dependency injection & conventions

DI is convention-driven via Scrutor assembly scanning — **new services and repositories are auto-registered, no manual `AddScoped` needed** as long as they follow the naming/namespace rules:

- Classes in `Application.Services` namespace ending in `Service` → registered as their implemented interface (scoped). See `Application/DependencyInjection.cs`.
- Classes in `Infrastructure.Repositories` namespace ending in `Repository` → registered as their implemented interface (scoped). See `Infrastructure/DependencyInjection.cs`.
- FluentValidation validators and AutoMapper profiles are scanned from the Application assembly automatically.

Composition roots: `AddApplicationServices`, `AddInfrastructureServices`, `AddAuthenticationServices` (in `SkyRoc/Extensions/AuthExtensions.cs`), all called from `Program.cs`.

### Request flow

Controller → application service → repository (via `IUnitOfWork` for transactions/`SaveChangesAsync`) → `ApplicationDbContext`. Controllers are thin: they call one service method and wrap the result in `ApiResponse<T>.Ok(...)` / `.Fail(...)` etc. Persistence changes in a service go through `IUnitOfWork.SaveChangesAsync` / `BeginTransactionAsync`, not the DbContext directly.

### Authorization model

Permission codes use `module:resource:action` format (see `Shared/Constants/PermissionCodes.cs`). Controllers declare the resource once at class level and the action per method:

```csharp
[Authorize]
[PermissionResource(PermissionCodes.Business.Orders.Resource)]   // class-level resource
public class OrdersController(ISaleOrderService service) : ControllerBase
{
    [ResourcePermission(PermissionActions.Read)]                 // method-level action
    public async Task<ActionResult<ApiResponse<PagedResult<SaleOrderDto>>>> GetPaged(...) { ... }
}
```

`ResourcePermissionAuthorizationHandler` combines the two into the required permission and checks the user's `permission` JWT claims (admin holds the wildcard `*:*:*`). JWT claims and the dynamic route tree (`getRoutes`) are built in `Application/Services/AuthService.cs` and `JwtService.cs`. Token access/refresh state lives in Redis (or memory fallback) via `ICacheService`.

## Project-specific rules (from AGENTS.md)

- **Numeric precision:** all quantity, price, cost, and amount math **must** use `Shared.Constants.NumericPrecision` (`QuantityScale=6`, `MoneyScale=4`, `RoundQuantity`/`RoundMoney`). Never hardcode decimal scales or midpoint rounding modes.
- **Computed properties:** for pure derived response values depending only on same-object fields, prefer read-only computed properties over storing/mapping duplicate state.
- **XML doc comments are part of "done"**, not optional. Add concise Chinese XML docs to entities and public properties, repository/service interfaces and implementations, controllers, actions, and request/response members — explaining business meaning, units, status semantics, side effects, and constraints (not just restating the name).
- **Entity DB comments:** every new/changed persisted entity must configure PostgreSQL table and column comments via EF Core `HasComment`, and those comments must appear in the generated migration (added/changed/rolled back correctly). Review generated migrations before committing.
- **File layout:** one top-level type per file, filename matches the type. Group by domain folders, not aggregate files. Suffix conventions: `*Controller`, `*Service`, `*Repository`, `*Dto`, `*Validator`, `*Tests`. Use `Async` suffix for async I/O.
- **Do not commit** production connection strings, JWT signing keys, or seed passwords. `appsettings.json` currently contains dev placeholders — override via environment variables (e.g. `ConnectionStrings__DefaultConnection`).

## Development continuity

This project follows a strict task-ordered workflow. Before feature work, read `docs/开发进度.md` (module progress) and `docs/自动开发任务清单.md` (task checklist), and complete only the **first unchecked task** — do not skip ahead. Business flow specs live in `docs/business-flows/`. Update the progress breakpoint and test baseline before handoff.

## Testing

xUnit + EF Core InMemory + coverlet + `Microsoft.AspNetCore.Mvc.Testing` (integration tests use `WebApplicationFactory` against the `Program` partial class). Tests are grouped by feature under `SkyRoc.Tests/` (e.g. `Orders/`, `Customers/`, `Mapping/`, `Caching/`, `Authorization/`, `Architecture/`, `Documentation/`). Name test methods as behavior statements: `GetByIdAsync_ReturnsCustomer_WhenCustomerExists`. Place regression tests beside the matching feature folder. Run the full test project before submitting; no coverage threshold is enforced.

## Swagger / OpenAPI

See **Swagger / OpenAPI** section in `AGENTS.md`. Controllers must return `ActionResult<ApiResponse<T>>` (not `IActionResult`). DTO XML lives in Application/Domain/Shared assemblies and is loaded via `SwaggerExtensions`. New controllers need Tag mapping and Documentation tests when response contracts change.
