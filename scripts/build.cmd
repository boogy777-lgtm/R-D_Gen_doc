@echo off
echo === Building AvaloniaApp ===
dotnet build
if errorlevel 1 exit /b 1

echo === Running tests ===
dotnet test AvaloniaApp.Tests\AvaloniaApp.Tests.csproj
if errorlevel 1 exit /b 1

echo === Running smoke test ===
dotnet run -- --smoke
if errorlevel 1 exit /b 1

echo === All checks passed ===
