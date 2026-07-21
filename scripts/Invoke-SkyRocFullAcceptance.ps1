param(
    [string]$ConnectionString = $env:SKYROC_TEST_CONNECTION_STRING,
    [string]$OutputDirectory = '',
    [switch]$IncludeFullTestSuite
)

<#
.SYNOPSIS
    SkyRoc T14 一键验收：白名单校验、迁移、模型挂起检查、质量/元数据门禁、构建与格式。

.DESCRIPTION
    默认不运行完整 SkyRoc.Tests（约 50 分钟）。完整回归必须由调用方显式传入 -IncludeFullTestSuite，
    且仅应在用户明确要求手动触发时使用。
#>

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$testSettingsPath = Join-Path $repositoryRoot 'SkyRoc.Tests/postgresql-testsettings.json'
$developmentSettingsPath = Join-Path $repositoryRoot 'SkyRoc/appsettings.Development.json'
$applicationSettingsPath = Join-Path $repositoryRoot 'SkyRoc/appsettings.json'
$testProjectPath = Join-Path $repositoryRoot 'SkyRoc.Tests/SkyRoc.Tests.csproj'
$solutionPath = Join-Path $repositoryRoot 'SkyRoc.sln'
$testSettings = Get-Content -LiteralPath $testSettingsPath -Raw | ConvertFrom-Json
$expectedDatabaseName = [string]$testSettings.expectedDatabaseName
$environmentName = [string]$testSettings.environmentName

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $settingsPath = if (Test-Path -LiteralPath $developmentSettingsPath) {
        $developmentSettingsPath
    }
    else {
        $applicationSettingsPath
    }
    $applicationSettings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    $ConnectionString = [string]$applicationSettings.ConnectionStrings.DefaultConnection
}

if ($environmentName -cne 'Testing') {
    throw "Full acceptance requires the exact Testing environment."
}
if ([string]::IsNullOrWhiteSpace($ConnectionString) -or $ConnectionString.Contains('__SET_IN_ENV__')) {
    throw "The PostgreSQL test connection string is not configured."
}
if ([string]::IsNullOrWhiteSpace($expectedDatabaseName)) {
    throw "The PostgreSQL test database allowlist is empty."
}

$databaseMatch = [regex]::Match(
    $ConnectionString,
    '(?i)(?:^|;)\s*(?:Database|Initial Catalog)\s*=\s*([^;]+)')
if (-not $databaseMatch.Success) {
    throw "The PostgreSQL test connection string does not specify a database."
}
$actualDatabaseName = $databaseMatch.Groups[1].Value.Trim().Trim('"')
if ($actualDatabaseName -cne $expectedDatabaseName) {
    throw "Database '$actualDatabaseName' is outside the exact test allowlist '$expectedDatabaseName'."
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $stamp = Get-Date -Format 'yyyyMMddHHmmss'
    $OutputDirectory = Join-Path $repositoryRoot "artifacts/acceptance-bin-$stamp"
}

$env:SKYROC_TEST_ENVIRONMENT = 'Testing'
$env:SKYROC_TEST_CONNECTION_STRING = $ConnectionString
$env:SKYROC_QUERY_TEST_CONNECTION_STRING = $ConnectionString
$env:ConnectionStrings__DefaultConnection = $ConnectionString

$qualityFilter = @(
    'FullyQualifiedName~FullAcceptancePostgreSqlTests'
    'FullyQualifiedName~PostgreSqlInfrastructureTests'
    'FullyQualifiedName~PostgreSqlInfrastructureDocumentationTests'
    'FullyQualifiedName~DatabaseMetadataInventoryTests'
    'FullyQualifiedName~DataQualityReportWriterTests'
    'FullyQualifiedName~DatabaseSafety'
    'FullyQualifiedName~BatchCleanup'
) -join '|'

Push-Location $repositoryRoot
try {
    Write-Host '=== EF database update ==='
    dotnet ef database update --project Infrastructure --startup-project SkyRoc
    if ($LASTEXITCODE -ne 0) {
        throw "EF Core database update failed with exit code $LASTEXITCODE."
    }

    Write-Host '=== has-pending-model-changes ==='
    dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project SkyRoc
    if ($LASTEXITCODE -ne 0) {
        throw "EF Core model has pending changes (exit code $LASTEXITCODE). Add a migration before full acceptance."
    }

    Write-Host "=== build to $OutputDirectory ==="
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    dotnet build $testProjectPath -o $OutputDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }

    Write-Host '=== T14 full acceptance + quality/metadata gates ==='
    dotnet test $testProjectPath --no-build -o $OutputDirectory --filter $qualityFilter
    if ($LASTEXITCODE -ne 0) {
        throw "Full acceptance / quality gates failed with exit code $LASTEXITCODE."
    }

    Write-Host '=== format verify ==='
    dotnet format $solutionPath --verify-no-changes
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet format --verify-no-changes failed with exit code $LASTEXITCODE."
    }

    if ($IncludeFullTestSuite) {
        Write-Host '=== FULL SkyRoc.Tests suite (explicitly requested) ==='
        dotnet test $testProjectPath --no-build -o $OutputDirectory
        if ($LASTEXITCODE -ne 0) {
            throw "Full SkyRoc.Tests suite failed with exit code $LASTEXITCODE."
        }
    }
    else {
        Write-Host 'Skipped full SkyRoc.Tests suite (pass -IncludeFullTestSuite only when manually requested).'
    }

    Write-Host 'SkyRoc full acceptance completed.'
}
finally {
    Pop-Location
}
