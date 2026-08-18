@echo off
for /f "tokens=5" %%p in ('netstat -ano ^| findstr /r "127.0.0.1:3081.*LISTENING"') do taskkill /f /pid %%p >nul 2>nul
echo Harness stopped (if it was running).