<#
.SYNOPSIS
    一键发布 SkyRoc Linux x64 包。

.DESCRIPTION
    默认 Framework-dependent（服务器需安装 ASP.NET Core 9 Runtime，包更小）。
    加 -SelfContained 则打出自包含包（服务器无需安装 Runtime）。

.EXAMPLE
    .\scripts\Publish-LinuxX64.ps1

.EXAMPLE
    .\scripts\Publish-LinuxX64.ps1 -SelfContained
#>
param(
    [switch]$SelfContained,
    [switch]$NoReadyToRun
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'SkyRoc\SkyRoc.csproj'
$modeFolder = if ($SelfContained) { 'linux-x64-sc' } else { 'linux-x64-fd' }
$outputPath = Join-Path $repositoryRoot "publish\$modeFolder"

Write-Host "Repository : $repositoryRoot"
Write-Host "Mode       : $(if ($SelfContained) { 'Self-contained' } else { 'Framework-dependent' })"
Write-Host "ReadyToRun : $(-not $NoReadyToRun)"
Write-Host "Output     : $outputPath"
Write-Host ''

$publishArgs = @(
    'publish', $projectPath,
    '-c', 'Release',
    '-r', 'linux-x64',
    '--self-contained', ($(if ($SelfContained) { 'true' } else { 'false' })),
    '-o', $outputPath
)

if (-not $NoReadyToRun) {
    $publishArgs += '/p:PublishReadyToRun=true'
}

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$files = Get-ChildItem -LiteralPath $outputPath -Recurse -File
$sizeMb = [math]::Round((($files | Measure-Object -Property Length -Sum).Sum / 1MB), 1)

Write-Host ''
Write-Host 'Publish succeeded.'
Write-Host "Files : $($files.Count)"
Write-Host "Size  : $sizeMb MB"
Write-Host "Path  : $outputPath"
Write-Host ''
if ($SelfContained) {
    Write-Host 'Server start: chmod +x SkyRoc && ./SkyRoc --urls http://0.0.0.0:5293'
}
else {
    Write-Host 'Server needs: ASP.NET Core 9 Runtime (Microsoft.AspNetCore.App 9.x)'
    Write-Host 'Server start: dotnet SkyRoc.dll --urls http://0.0.0.0:5293'
}
