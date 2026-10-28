#Requires -Version 7.0
<#
.SYNOPSIS
    Restore, build, test, publish and package a clean Windows release.
.DESCRIPTION
    Compile and test the portable solution before restoring only the app for
    the publish RID. No --no-build/--no-restore assumptions across RIDs.
    Returns a non-zero exit code on any failed command.
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = (Join-Path $PSScriptRoot "Output"),
    [string]$LogDir = (Join-Path $PSScriptRoot "Logs"),
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$solution = Join-Path $root "AvaloniaApp.slnx"
$app = Join-Path $root "AvaloniaApp.csproj"
$tests = Join-Path $root "AvaloniaApp.Tests/AvaloniaApp.Tests.csproj"
$publishDir = Join-Path $OutputDir "publish"
$zipName = "R-D_Gen_doc-$Configuration-$Runtime.zip"
$zipPath = Join-Path $OutputDir $zipName
$logPath = Join-Path $LogDir "build.log"

New-Item -ItemType Directory -Force -Path $OutputDir, $LogDir | Out-Null
if (Test-Path $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
if (Test-Path "$zipPath.sha256") { Remove-Item -LiteralPath "$zipPath.sha256" -Force }
Set-Content -Path $logPath -Value "R-D Gen Doc release build" -Encoding utf8

function Invoke-DotNet {
    param([string]$Step, [string[]]$Arguments)
    Write-Host "==> $Step" -ForegroundColor Cyan
    & dotnet @Arguments 2>&1 | Tee-Object -FilePath $logPath -Append
    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed (exit code $LASTEXITCODE)"
    }
}

try {
    Invoke-DotNet "Restore solution" @("restore", $solution)
    Invoke-DotNet "Build solution" @("build", $solution, "-c", $Configuration, "--no-restore")

    if (-not $SkipTests) {
        Invoke-DotNet "Run tests" @(
            "test", $tests, "-c", $Configuration, "--no-build", "--no-restore",
            "--results-directory", $LogDir, "--logger", "trx;LogFileName=test-results.trx"
        )
    }

    # RID-specific restore and build happen here. Do not reuse the portable build
    # outputs with --no-build; a self-contained RID publish needs its own assets.
    Invoke-DotNet "Publish standalone" @(
        "publish", $app, "-c", $Configuration, "-r", $Runtime,
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:PublishReadyToRun=false",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:IncludeAllContentForSelfExtract=false",
        "-p:DebugType=none",
        "-p:DebugSymbols=false",
        "-o", $publishDir
    )

    $exePath = Join-Path $publishDir "AvaloniaApp.exe"
    if (-not (Test-Path -LiteralPath $exePath)) {
        throw "Publish did not produce AvaloniaApp.exe"
    }

    # Ship sample configuration, never developer secrets or local databases.
    Copy-Item (Join-Path $root "appsettings.example.json") -Destination $publishDir
    Copy-Item (Join-Path $root "appsettings.README.md") -Destination $publishDir

    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
    $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -Path "$zipPath.sha256" -Value "$hash  $zipName" -Encoding utf8NoBOM

    Write-Host "[OK] Package: $zipPath" -ForegroundColor Green
    Write-Host "[OK] SHA256: $hash" -ForegroundColor Green
}
catch {
    Write-Error $_
    exit 1
}
