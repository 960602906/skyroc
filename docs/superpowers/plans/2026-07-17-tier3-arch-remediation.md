# Tier-3 Arch Remediation Implementation Plan

> **For agentic workers:** Use executing-plans / implement task-by-task. Checkboxes track progress.

**Goal:** Ship #14 docs, #12 Serilog+OTel, #10 InventoryCosting split, #11 same-tx app events for supplier bills — no #13 secrets work.

**Architecture:** Docs align with README; observability via host extensions; costing extracted as scoped app service; finance synced via in-process event handlers inside existing UoW transactions.

**Tech Stack:** .NET 9, Serilog, OpenTelemetry, Scrutor DI, existing StockIn/Out + SupplierBill services.

**Spec:** `docs/superpowers/specs/2026-07-17-tier3-arch-remediation-design.md`

## Global Constraints

- Do not restore Program.cs auto Migrate/Seed
- Do not move secrets / rotate credentials (#13)
- Preserve ExecuteInTransactionAsync boundaries and NumericPrecision
- Chinese XML docs on new public types
- Separate commits per task below

---

### Task 1: #14 Documentation

**Files:**
- Modify: `CLAUDE.md` (startup migrate sentence)
- Modify: `docs/后端架构评审与整改.md` (#14 status)
- Optionally touch `AGENTS.md` only if it claims auto-migrate (it does not currently)

- [ ] Replace CLAUDE.md line about auto-runs migrations with README-aligned text
- [ ] Mark #14 resolved as docs-only in remediation doc
- [ ] Commit: `docs: 对齐启动不自动迁移/种子的说明`

---

### Task 2: #12 Observability

**Files:**
- Create: `SkyRoc/Extensions/ObservabilityExtensions.cs`
- Modify: `SkyRoc/Program.cs`, `SkyRoc/SkyRoc.csproj`
- Modify: `SkyRoc/appsettings.json`, `SkyRoc/appsettings.Development.json` (Serilog/OpenTelemetry sections only — no secrets)

Packages (SkyRoc.csproj):
- Serilog.AspNetCore
- Serilog.Sinks.Console
- Serilog.Sinks.File
- OpenTelemetry.Extensions.Hosting
- OpenTelemetry.Instrumentation.AspNetCore
- OpenTelemetry.Instrumentation.Http
- OpenTelemetry.Instrumentation.EntityFrameworkCore
- OpenTelemetry.Exporter.OpenTelemetryProtocol

- [ ] Add packages + `AddSkyRocObservability` / `UseSkyRocObservability`
- [ ] Wire in Program.cs; request logging after exception middleware
- [ ] Build; smoke that tests still construct WebApplicationFactory
- [ ] Commit: `feat: 接入 Serilog 与 OpenTelemetry`

---

### Task 3: #10 InventoryCostingService

**Files:**
- Create: `Application/Interfaces/Storage/IInventoryCostingService.cs`
- Create: `Application/Services/Storage/InventoryCostingService.cs`
- Modify: `Application/Services/Storage/StockInService.cs`

Move inbound batch resolve/apply/reversal/ledger helpers from StockInService; keep orchestration there.

- [ ] Extract interface + service (Chinese XML)
- [ ] StockInService calls costing service; remove private costing methods + unused deps if any
- [ ] `dotnet test --filter FullyQualifiedName~StockIn`
- [ ] Commit: `refactor: 抽取入库库存成本批次服务`

---

### Task 4: #11 Application events for finance sync

**Files:**
- Create event + publisher + handler types under `Application/Events/` (or `Application/Abstractions/Events/`)
- Modify: `Application/DependencyInjection.cs` to register handlers
- Modify: `StockInService.cs`, `StockOutService.cs`
- Create handlers calling `ISupplierBillService`

- [ ] Publisher + handler registration
- [ ] Replace direct bill calls with PublishAsync
- [ ] `dotnet test --filter "FullyQualifiedName~StockIn|FullyQualifiedName~StockOut|FullyQualifiedName~SupplierBill"`
- [ ] Commit: `refactor: 同事务应用事件解耦库存与供应商账单`

---

### Task 5: Spec commit + verify remediation doc

- [ ] Commit design spec if not already
- [ ] Update `docs/后端架构评审与整改.md` tier-3 items as done (except #13)
