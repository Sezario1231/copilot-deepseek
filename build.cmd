@echo off
rem Build DeepSeek Copilot with the portable .NET 10 SDK (isolated from the system dotnet).
set "DOTNET=D:\deepseek-harness-iso\.runtime\dotnet-sdk\dotnet.exe"
cd /d "%~dp0"
"%DOTNET%" build deepseek-copilot.csproj -c Release