@echo off
setlocal
cd /d "%~dp0"
set "DSH_HOME=%~dp0home"
if not exist "%DSH_HOME%" mkdir "%DSH_HOME%"
if not exist "%~dp0harness\package.json" (
  echo [ERROR] Harness not deployed yet. Run setup-harness.cmd first.
  pause
  exit /b 1
)
cd /d "%~dp0harness"
echo Starting DeepSeek Harness on http://127.0.0.1:3081
echo Keep this window open while using the app. Close it to stop the harness.
call pnpm dsh web --port 3081