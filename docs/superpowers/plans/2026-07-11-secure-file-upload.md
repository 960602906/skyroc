# Secure File Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provide a reusable, authenticated upload and download capability for report documents and images without exposing the physical storage directory.

**Architecture:** Persist a `StoredFile` metadata record with creator ownership, sanitized original name, verified content type, byte count and a generated storage key. The application service verifies extension, declared MIME type, size and file signatures before atomically writing below a configured content-root directory; controllers only return protected API download URLs.

**Tech Stack:** .NET 9, ASP.NET Core multipart binding, EF Core 9/PostgreSQL, xUnit and EF Core InMemory.

## Global Constraints

- Accept only PDF, PNG and JPEG payloads; validate signatures rather than trusting file names or request MIME types.
- Limit each upload to 10 MiB and use a random server-side storage key outside public static-file middleware.
- Require stable `business:file:create` and `business:file:read` permissions; a non-owner must not retrieve a file.
- Persisted entities and every persisted column require Chinese XML and PostgreSQL comments, represented in the migration.
- Keep P4-07 limited to upload/download support; integration into report or inspection save workflows remains out of scope.

---

### Task 1: File metadata persistence and security contract

**Files:**
- Create: `Domain/Entities/Files/StoredFile.cs`
- Create: `Domain/Interfaces/Files/IStoredFileRepository.cs`
- Create: `Infrastructure/Data/EntityConfigurations/Files/StoredFileConfiguration.cs`
- Create: `Infrastructure/Repositories/Files/StoredFileRepository.cs`
- Modify: `Infrastructure/Data/ApplicationDbContext.cs`
- Modify: `Shared/Constants/PermissionCodes.cs`
- Modify: `SkyRoc.Tests/Architecture/DatabaseCommentTests.cs`

- [ ] **Step 1: Write failing model/comment tests**
- [ ] **Step 2: Run the focused tests and confirm they fail because `StoredFile` does not exist**
- [ ] **Step 3: Add the documented entity, repository seam, configuration and permission constants**
- [ ] **Step 4: Generate `AddStoredFiles` migration and inspect Up/Down comments and constraints**
- [ ] **Step 5: Run focused tests and confirm they pass**

### Task 2: Safe storage service

**Files:**
- Create: `Shared/Common/FileStorageOptions.cs`
- Create: `Application/DTOs/Files/UploadFileDto.cs`
- Create: `Application/DTOs/Files/StoredFileDto.cs`
- Create: `Application/interfaces/Files/IFileStorageService.cs`
- Create: `Application/Services/Files/FileStorageService.cs`
- Create: `SkyRoc.Tests/Files/FileStorageServiceTests.cs`
- Modify: `Application/DependencyInjection.cs`
- Modify: `SkyRoc/appsettings.json`

- [ ] **Step 1: Write failing service tests for accepted PDF/JPEG/PNG uploads, oversized files, spoofed extensions/MIME types and cross-user reads**
- [ ] **Step 2: Run the focused tests and confirm failure because the service is unavailable**
- [ ] **Step 3: Implement signature verification, path containment, generated names, write rollback and owner isolation**
- [ ] **Step 4: Run focused service tests and confirm they pass**

### Task 3: Protected HTTP contract and documentation

**Files:**
- Create: `SkyRoc/Controllers/FilesController.cs`
- Create: `SkyRoc.Tests/Authorization/FilesControllerPermissionTests.cs`
- Modify: `SkyRoc.Tests/Documentation/SwaggerResponseSchemaTests.cs`
- Modify: `SkyRoc/SkyRoc.http`
- Modify: `docs/business-flows/00-global.md`

- [ ] **Step 1: Write failing controller permission and Swagger contract tests**
- [ ] **Step 2: Run them and confirm failure because the controller route is absent**
- [ ] **Step 3: Implement multipart upload and protected download endpoints with typed `ApiResponse` payloads and XML documentation**
- [ ] **Step 4: Run focused authorization and documentation tests**
- [ ] **Step 5: Run build, full test suite, migration-drift check and final strict review**
