@echo off
REM One release path: delegate to the tested PowerShell pipeline.
REM Requires PowerShell 7 (pwsh) and .NET 10 SDK.
setlocal
where pwsh >nul 2>&1
if errorlevel 1 (
    echo ERROR: PowerShell 7 is required. Install pwsh before building.
    exit /b 1
)
pwsh -NoProfile -File "%~dp0..\CI\build-release.ps1" %*
set "RC=%ERRORLEVEL%"
endlocal & exit /b %RC%
