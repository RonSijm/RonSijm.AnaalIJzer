@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "SCRIPT=%SCRIPT_DIR%build-arse.ps1"

where pwsh >nul 2>nul
if %ERRORLEVEL% neq 0 goto useWindowsPowerShell
pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%

:useWindowsPowerShell
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%
