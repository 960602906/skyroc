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

## Documentation Comment Requirements

Comments are part of the definition of done, not optional cleanup. Add concise XML documentation to domain entities and their public properties, repository interfaces and implementations, application service interfaces and implementations, controllers, actions, and public request/response members. Comments must explain business meaning, units, status semantics, side effects, and important constraints instead of merely repeating the identifier.

For every new or changed persisted entity, configure PostgreSQL table and column comments through EF Core (`HasComment`) and include those comments in the generated migration. Review migrations to ensure comments are added, changed, and rolled back correctly. Add focused inline comments only where non-obvious private logic or state transitions need explanation. New or modified code missing required comments is incomplete and must not be handed off.

## Testing Guidelines

Tests use xUnit, EF Core InMemory, and coverlet. Name test classes after the subject and test methods as behavior statements, for example `GetByIdAsync_ReturnsCustomer_WhenCustomerExists`. Place regression tests beside the matching feature folder. Run the full test project before submitting; no numeric coverage threshold is currently enforced.

## Commit & Pull Request Guidelines

Recent history contains brief Chinese and English summaries. Prefer a specific imperative subject over generic messages such as `提交`, for example `修复客户查询分页` or `Add Redis fallback tests`. Keep commits focused. Pull requests should describe the behavior change, affected layers, configuration or migration impact, and test evidence. Link related issues and include request/response examples or Swagger screenshots when API behavior changes.

## Security & Configuration

Do not commit production connection strings, JWT signing keys, or seed passwords. Override `SkyRoc/appsettings.json` with environment variables or an untracked development configuration. Review generated EF Core migrations before committing them.
