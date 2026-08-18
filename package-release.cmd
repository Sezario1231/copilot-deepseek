@echo off
setlocal
rem Publish a self-contained single-file exe and zip it for a GitHub Release.
rem Output: .\dist\DeepSeek-Copilot-win-x64.zip  (double-click exe inside = Chat mode works instantly)
cd /d "%~dp0"

set "OUT=%~dp0dist"
set "APP=%OUT%\app"
set "ZIP=%OUT%\DeepSeek-Copilot-win-x64.zip"

where dotnet >nul 2>nul
if not errorlevel 1 (
  set "DOTNET=dotnet"
) else if exist "%~dp0harness\.runtime\dotnet-sdk\dotnet.exe" (
  set "DOTNET=%~dp0harness\.runtime\dotnet-sdk\dotnet.exe"
) else (
  echo [ERROR] .NET 10 SDK not found. Install from https://dotnet.microsoft.com/download/dotnet/10.0
  exit /b 1
)

if exist "%APP%" rmdir /s /q "%APP%"
"%DOTNET%" publish deepseek-copilot.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "%APP%" || exit /b 1

del /q "%APP%\*.pdb" "%APP%\*.xml" 2>nul
if exist "%ZIP%" del /q "%ZIP%"

echo Zipping %ZIP% ...
powershell -NoProfile -Command "Compress-Archive -Path '%APP%\deepseek-copilot.exe' -DestinationPath '%ZIP%' -CompressionLevel Optimal"
echo.
echo Done: %ZIP%
echo Upload this file to a GitHub Release, or share it directly.