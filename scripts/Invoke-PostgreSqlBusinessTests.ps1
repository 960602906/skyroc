param(
    [string]$ConnectionString = $env:SKYROC_TEST_CONNECTION_STRING
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$testSettingsPath = Join-Path $repositoryRoot 'SkyRoc.Tests\postgresql-testsettings.json'
$developmentSettingsPath = Join-Path $repositoryRoot 'SkyRoc\appsettings.Development.json'
$applicationSettingsPath = Join-Path $repositoryRoot 'SkyRoc\appsettings.json'
$testSettings = Get-Content -LiteralPath $testSettingsPath -Raw | ConvertFrom-Json
$expectedDatabaseName = [string]$testSettings.expectedDatabaseName
$environmentName = [string]$testSettings.environmentName

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    # 优先本地开发库约定（appsettings.Development.json），再回退到 appsettings.json
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
    throw "PostgreSQL business tests require the exact Testing environment."
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

$env:SKYROC_TEST_ENVIRONMENT = 'Testing'
$env:SKYROC_TEST_CONNECTION_STRING = $ConnectionString
$env:ConnectionStrings__DefaultConnection = $ConnectionString

Push-Location $repositoryRoot
try {
    dotnet ef database update --project Infrastructure --startup-project SkyRoc
    if ($LASTEXITCODE -ne 0) {
        throw "EF Core database update failed with exit code $LASTEXITCODE."
    }

    dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --filter 'FullyQualifiedName~PostgreSqlInfrastructureTests|FullyQualifiedName~PostgreSqlInfrastructureDocumentationTests|FullyQualifiedName~DatabaseMetadataInventoryTests|FullyQualifiedName~DataQualityReportWriterTests|FullyQualifiedName~DatabaseSafety|FullyQualifiedName~BatchCleanup'
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL infrastructure tests failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
