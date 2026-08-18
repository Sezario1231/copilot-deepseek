@echo off
rem Launch DeepSeek Copilot; starts the isolated harness first if it is not listening on 3081.
netstat -ano | findstr /r "127.0.0.1:3081.*LISTENING" >nul
if errorlevel 1 (
  echo Isolated harness is not running - starting it now...
  start "" "D:\deepseek-harness-iso\start-web.cmd"
  timeout /t 15 /nobreak >nul
)
start "" "%~dp0bin\Release\net10.0-windows\win-x64\deepseek-copilot.exe"