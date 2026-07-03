#Requires -Version 7.0
<#
.SYNOPSIS
    CI release pipeline: restore → build → test → publish standalone → package.
.DESCRIPTION
    AvaloniaApp CI. Produces self-contained win-x64 single-file exe.
    Test results → CI\Logs\test-results.trx
    Build log    → CI\Logs\build.log
    Output       → CI\Output\AvaloniaApp.zip
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "$PSScriptRoot\Output",
    [string]$LogDir = "$PSScriptRoot\Logs",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$solutionRoot = Resolve-Path "$PSScriptRoot\.."
$solutionFile = "$solutionRoot\AvaloniaApp.slnx"
$projectFile = "$solutionRoot\AvaloniaApp.csproj"
$testProject  = "$solutionRoot\AvaloniaApp.Tests\AvaloniaApp.Tests.csproj"
$timestamp    = Get-Date -Format "yyyy-MM-dd_HHmmss"

# ── Init ──────────────────────────────────────────────
New-Item -ItemType Directory -Force -Path $OutputDir, $LogDir | Out-Null
$buildLog = "$LogDir\build-$timestamp.log"
$testLog  = "$LogDir\test-results-$timestamp.trx"

function Write-Step($msg) {
    $line = "── $msg ──"
    Write-Host $line -ForegroundColor Cyan
    Add-Content -Path $buildLog -Value $line
}

function Write-Result($ok, $msg) {
    $color = if ($ok) { "Green" } else { "Red" }
    $text  = if ($ok) { "[OK]  $msg" } else { "[FAIL] $msg" }
    Write-Host $text -ForegroundColor $color
    Add-Content -Path $buildLog -Value $text
}

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# ── 1. Restore ────────────────────────────────────────
Write-Step "1/4: dotnet restore"
dotnet restore $solutionFile --runtime $Runtime 2>&1 | Tee-Object -FilePath $buildLog -Append
if ($LASTEXITCODE -ne 0) { Write-Result $false "restore"; exit 1 }
Write-Result $true "restore"

# ── 2. Build ──────────────────────────────────────────
Write-Step "2/4: dotnet build"
dotnet build $solutionFile -c $Configuration --no-restore 2>&1 | Tee-Object -FilePath $buildLog -Append
if ($LASTEXITCODE -ne 0) { Write-Result $false "build"; exit 1 }
Write-Result $true "build"

# ── 3. Test ───────────────────────────────────────────
if (-not $SkipTests) {
    Write-Step "3/4: dotnet test"
    dotnet test $testProject -c $Configuration --no-build --logger "trx;LogFileName=$testLog" 2>&1 | Tee-Object -FilePath $buildLog -Append
    if ($LASTEXITCODE -ne 0) { Write-Result $false "test"; exit 1 }
    Write-Result $true "test"
} else {
    Write-Step "3/4: SKIP tests (--SkipTests)"
}

# ── 4. Publish standalone ────────────────────────────
Write-Step "4/4: dotnet publish (standalone single-file)"
$publishDir = "$OutputDir\publish"
dotnet publish $projectFile -c $Configuration -r $Runtime --no-restore --no-build --self-contained -o $publishDir 2>&1 | Tee-Object -FilePath $buildLog -Append
if ($LASTEXITCODE -ne 0) { Write-Result $false "publish"; exit 1 }
Write-Result $true "publish"

# ── Package ───────────────────────────────────────────
$zipName = "AvaloniaApp-$Configuration-$Runtime-$timestamp.zip"
$zipPath = "$OutputDir\$zipName"
Write-Step "Package: $zipName"
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force
Write-Result $true "package → $zipPath"

# ── Summary ──────────────────────────────────────────
$stopwatch.Stop()
$summary = @"
========================================
AvaloniaApp CI — Build Summary
========================================
Timestamp : $timestamp
Config    : $Configuration
Runtime   : $Runtime
Duration  : $($stopwatch.Elapsed.ToString('mm\:ss'))
Artifact  : $zipName
Tests     : $(if ($SkipTests) {'SKIPPED'} else {$testLog})
========================================
"@
Add-Content -Path $buildLog -Value $summary
Write-Host "`n$summary" -ForegroundColor Green
