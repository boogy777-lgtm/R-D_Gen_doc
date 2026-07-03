@echo off
REM ============================================================
REM  Build standalone AvaloniaApp.exe (single-file, self-contained)
REM  Output: scripts\dist\AvaloniaApp.exe
REM
REM  Usage:
REM    scripts\build-dist.cmd         - full: R2R AOT, fast startup (~3 min)
REM    scripts\build-dist.cmd /fast   - quick: no R2R, smaller, slower startup
REM
REM  Note: tests NOT run — use scripts\build.cmd for test gate.
REM ============================================================
setlocal
set DIST_DIR=%~dp0dist
set PROJECT=%~dp0..\AvaloniaApp.csproj
set R2R=true

if /I "%~1"=="/fast" set R2R=false
if /I "%~1"=="-fast" set R2R=false

echo [1/2] Cleaning %DIST_DIR%...
if exist "%DIST_DIR%" rmdir /s /q "%DIST_DIR%"
mkdir "%DIST_DIR%"

echo [2/2] Publishing standalone win-x64 (R2R=%R2R%)...
dotnet publish "%PROJECT%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:PublishReadyToRun=%R2R% ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:IncludeAllContentForSelfExtract=true ^
    -p:InvariantGlobalization=true ^
    -p:EventSourceSupport=false ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    -o "%DIST_DIR%"

if errorlevel 1 (
    echo ERROR: Publish failed
    exit /b 1
)

echo.
echo === Output ===
dir "%DIST_DIR%" /b
echo.
echo Dist ready: %DIST_DIR%
endlocal
