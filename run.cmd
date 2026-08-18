@echo off
setlocal
cd /d "%~dp0"

rem Auto-start the isolated harness on 3081 if it is not already listening.
netstat -ano | findstr /r "127.0.0.1:3081.*LISTENING" >nul
if errorlevel 1 (
  if exist "%~dp0start-web.cmd" (
    echo Harness not running - starting it...
    start "" "%~dp0start-web.cmd"
  )
  timeout /t 15 /nobreak >nul
)

rem Build first if no binary yet.
if not exist "%~dp0bin\Release\net10.0-windows\win-x64\deepseek-copilot.exe" (
  echo Not built yet - building...
  call "%~dp0build.cmd"
  if errorlevel 1 exit /b 1
)

start "" "%~dp0bin\Release\net10.0-windows\win-x64\deepseek-copilot.exe"