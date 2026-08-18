@echo off
setlocal
rem Build DeepSeek Copilot. Uses the system dotnet if present, otherwise a portable
rem .NET 10 SDK under harness\.runtime (created by a manual portable-SDK install).
cd /d "%~dp0"

where dotnet >nul 2>nul
if not errorlevel 1 (
  dotnet build deepseek-copilot.csproj -c Release
  exit /b %errorlevel%
)

if exist "%~dp0harness\.runtime\dotnet-sdk\dotnet.exe" (
  echo Using portable SDK: harness\.runtime\dotnet-sdk
  "%~dp0harness\.runtime\dotnet-sdk\dotnet.exe" build deepseek-copilot.csproj -c Release
  exit /b %errorlevel%
)

echo [ERROR] .NET 10 SDK not found.
echo Install it from https://dotnet.microsoft.com/download/dotnet/10.0
pause
exit /b 1