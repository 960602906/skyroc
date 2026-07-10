# P4-04 Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Correct the P4-04 report aggregation, purchaser-return attribution, contract coverage, and seed-construction review findings.

**Architecture:** Report repositories project inbound and outbound details into compatible database queries, combine them before aggregation, and let EF Core page the final query. Purchase-return attribution resolves the earliest audited purchase inbound for a stock batch before applying purchaser and pattern filters.

**Tech Stack:** .NET 9, EF Core 9, xUnit, PostgreSQL-compatible LINQ.

## Global Constraints

- Preserve the existing P4-04 working-tree changes.
- Add Chinese XML documentation to any new public type or member.
- Use `NumericPrecision` for response rounding; do not change persisted models or migrations.
- Add regression tests before implementation and observe each test fail.

---

### Task 1: Purchaser return attribution regression

**Files:**
- Modify: `SkyRoc.Tests/Reports/ReportServiceTests.cs`
- Modify: `Infrastructure/Repositories/Reports/ReportRepository.cs`

- [x] Add a seed with two audited purchase inbounds for one batch but different purchasers, then assert the return belongs only to the earliest inbound purchaser and is excluded from the later purchaser query.
- [x] Run the focused test and confirm it fails against the current filtering-after-source-selection behavior.
- [x] Resolve each return's earliest audited purchase source before applying purchaser and purchase-pattern filtering; rerun the focused test.

### Task 2: Database aggregation and contract regressions

**Files:**
- Modify: `Infrastructure/Repositories/Reports/ReportRepository.cs`
- Modify: `SkyRoc.Tests/Reports/ReportServiceTests.cs`
- Modify: `SkyRoc.Tests/Documentation/SwaggerResponseSchemaTests.cs`

- [x] Add regression assertions for paged report results and for each added Swagger path's JSON schema reference.
- [x] Run focused report and Swagger tests, confirming the strengthened schema assertion passes against the existing typed response contract.
- [x] Replace the in-memory inbound/outbound merge and `ToPagedResult` calls with compatible database query concatenation, grouping, ordering, and `ToPagedResultAsync`; strengthen Swagger assertions; rerun focused tests.

### Task 3: Review cleanup and verification

**Files:**
- Modify: `SkyRoc.Tests/Reports/ReportServiceTests.cs`

- [x] Replace the positional `Guid` seed-record construction with named arguments.
- [x] Run `dotnet build SkyRoc.sln --no-restore`, `dotnet test SkyRoc.Tests\\SkyRoc.Tests.csproj`, and `dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project SkyRoc`.
