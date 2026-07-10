# P4-04 Inventory And Purchase Reports Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement read-only inventory daily, goods daily, and purchase in/out summary reports for P4-04.

**Architecture:** Reuse the existing reports boundary: `ReportsController` delegates to `IReportService`, which normalizes query parameters and maps repository read models to DTOs. `ReportRepository` performs EF Core no-tracking aggregate projections over audited stock in/out documents; no persisted model or migration is expected.

**Tech Stack:** .NET 9, EF Core 9 InMemory tests, ASP.NET Core Web API, xUnit.

## Global Constraints

- Complete only the first unchecked task: P4-04 库存与采购报表.
- Preserve existing user changes; current `git status --short` is clean.
- Add XML comments for every new public query parameter, DTO, service, repository, and controller member.
- Use `Shared.Constants.NumericPrecision` for quantity and amount response rounding.
- Do not add migrations unless EF reports pending model changes.

---

### Task 1: Tests For New Report Behaviors

**Files:**
- Modify: `SkyRoc.Tests/Reports/ReportServiceTests.cs`
- Modify: `SkyRoc.Tests/Authorization/ReportControllerPermissionTests.cs`

**Interfaces:**
- Produces: failing expectations for `GetDailyStockInOutAsync`, `GetDailyGoodsStockInOutAsync`, `GetPurchaseInOutGoodsSummaryAsync`, `GetPurchaseInOutSupplierSummaryAsync`, and `GetPurchaseInOutPurchaserSummaryAsync`.

- [ ] **Step 1: Add service tests for audited-only stock and purchase reports**

Add xUnit tests that seed audited purchase in, audited purchase return out, other in/out, and draft rows, then assert:
- daily stock groups by natural day and counts only audited documents;
- daily goods groups by natural day + goods;
- purchase summaries count only purchase inbound and purchase return outbound;
- supplier and purchaser filters use source order snapshots.

- [ ] **Step 2: Add controller permission test expectations**

Add all new controller action names to the report read-permission assertion.

- [ ] **Step 3: Run tests to verify RED**

Run: `dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --filter "FullyQualifiedName~Report" --no-build`

Expected: compile failure because new application service/controller members and DTOs do not exist yet.

### Task 2: Implement P4-04 Reports

**Files:**
- Create: `Application/QueryParameters/Reports/StockReportQueryParameters.cs`
- Create: `Application/QueryParameters/Reports/PurchaseInOutReportQueryParameters.cs`
- Create: `Application/DTOs/Reports/DailyStockInOutSummaryDto.cs`
- Create: `Application/DTOs/Reports/DailyGoodsStockInOutSummaryDto.cs`
- Create: `Application/DTOs/Reports/PurchaseInOutGoodsSummaryDto.cs`
- Create: `Application/DTOs/Reports/PurchaseInOutSupplierSummaryDto.cs`
- Create: `Application/DTOs/Reports/PurchaseInOutPurchaserSummaryDto.cs`
- Create matching read models and filters under `Domain/ReadModels/Reports/`
- Modify report service, repository interface/implementation, and controller.

**Interfaces:**
- Consumes: existing `PagedResult<T>`, `NumericPrecision`, stock and purchase entities.
- Produces: typed GET endpoints under `api/reports/stock/*` and `api/reports/purchase-in-out/*`.

- [ ] **Step 1: Add DTOs, filters and read models with XML comments**

Each public property describes the reporting grain, unit, amount, or count semantics.

- [ ] **Step 2: Add service interface and implementation methods**

Normalize ids/text, call repository, round quantities and money using `NumericPrecision`.

- [ ] **Step 3: Add repository aggregate projections**

Use audited stock documents only, `AsNoTracking`, database-side `GroupBy`, and `ToPagedResultAsync`.

- [ ] **Step 4: Add controller actions**

Return `ActionResult<ApiResponse<PagedResult<TDto>>>` and declare report read permission.

- [ ] **Step 5: Run focused tests to verify GREEN**

Run: `dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --filter "FullyQualifiedName~Report"`

### Task 3: Documentation And Contract Coverage

**Files:**
- Modify: `SkyRoc.Tests/Documentation/SwaggerResponseSchemaTests.cs`
- Modify: `SkyRoc/SkyRoc.http`
- Modify: `docs/自动开发任务清单.md`
- Modify: `docs/开发进度.md`

**Interfaces:**
- Consumes: new controller routes and DTO names.
- Produces: Swagger contract assertions, HTTP examples, and updated progress state.

- [ ] **Step 1: Extend Swagger tests for new paths and schemas**

Assert every new report path has JSON content schema and includes `business:report:read`.

- [ ] **Step 2: Add HTTP examples**

Add five GET examples for stock daily, stock goods daily, and three purchase in/out dimensions.

- [ ] **Step 3: Update progress docs only after verification and final approval**

Mark P4-04 complete, make P4-05 the only current task, update breakpoint, module table, date, and test baseline.

### Task 4: Verification And Review

**Files:**
- No planned source edits unless review finds actionable issues.

**Interfaces:**
- Produces: approved review conclusion.

- [ ] **Step 1: Run full verification**

Run:
- `dotnet build SkyRoc.sln --no-restore`
- `dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj`
- `dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project SkyRoc`

- [ ] **Step 2: Perform strict code review**

Review correctness, boundary values, concurrency consistency, security, performance, layering, comments, migrations, and test coverage. Fix all actionable findings and repeat verification until approved.
