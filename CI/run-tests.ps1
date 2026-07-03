#Requires -Version 7.0
<#
.SYNOPSIS
    Run tests only, output trx + console summary.
#>
param(
    [string]$Configuration = "Release",
    [string]$LogDir = "$PSScriptRoot\Logs"
)

$ErrorActionPreference = "Stop"
$solutionRoot = Resolve-Path "$PSScriptRoot\.."
$testProject  = "$solutionRoot\AvaloniaApp.Tests\AvaloniaApp.Tests.csproj"
$timestamp    = Get-Date -Format "yyyy-MM-dd_HHmmss"

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$testLog = "$LogDir\test-results-$timestamp.trx"

Write-Host "── dotnet test ──" -ForegroundColor Cyan
dotnet test $testProject -c $Configuration --logger "trx;LogFileName=$testLog"

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Tests passed → $testLog" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Tests failed → $testLog" -ForegroundColor Red
    exit 1
}
