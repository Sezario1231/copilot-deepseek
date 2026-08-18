@echo off
setlocal
rem One-time setup: deploy an isolated DeepSeek Harness into .\harness (DSH_HOME=.\home), port 3081.
rem Requires: Git, Node.js >= 20, pnpm.
cd /d "%~dp0"

set "HARNESS=%~dp0harness"
set "HOME=%~dp0home"

where git >nul 2>nul || (echo [ERROR] Git not found. Install: https://git-scm.com & exit /b 1)
where node >nul 2>nul || (echo [ERROR] Node.js ^>= 20 not found. Install: https://nodejs.org & exit /b 1)
where pnpm >nul 2>nul || (echo [ERROR] pnpm not found. Run: npm install -g pnpm & exit /b 1)

echo [1/3] Deploying DeepSeek Harness into %HARNESS%...
if not exist "%HARNESS%\.git" (
  git clone --depth 1 https://github.com/deepseek-ai/deepseek-harness.git "%HARNESS%" || (echo Clone failed & exit /b 1)
) else (
  echo   harness already present, skipping clone.
)

echo [2/3] Installing dependencies ^(pnpm install, may take a few minutes^)...
pushd "%HARNESS%"
call pnpm install || (popd & echo Install failed & exit /b 1)
popd

echo [3/3] Preparing isolated home ^(DSH_HOME^)...
if not exist "%HOME%" mkdir "%HOME%"
if not exist "%HOME%\.env" (
  echo DEEPSEEK_API_KEY=> "%HOME%\.env"
  echo DEEPSEEK_API_BASE=https://api.deepseek.com>> "%HOME%\.env"
  echo   Created template: "%HOME%\.env"
) else (
  echo   .env already exists, keeping it.
)

echo.
echo === Setup complete. Next steps: ===
echo   1. Fill in your API key:  notepad "%HOME%\.env"
echo   2. Start the harness:      start-web.cmd   ^(leave this window open^)
echo   3. Build the app:          build.cmd
echo   4. Run the app:            run.cmd   ^(auto-starts the harness too^)
echo.
echo   No API key? Chat mode ^(DeepSeek web login^) still works; only Agent mode needs a key.
pause