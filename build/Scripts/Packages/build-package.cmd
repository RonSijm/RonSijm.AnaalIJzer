@echo off
setlocal

set "SCRIPT_DIR=%~dp0"

echo.
echo === Building and verifying NuGet packages ===
echo.

pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%build-base.ps1" -Configuration Release -SkipTests
set "EXIT_CODE=%ERRORLEVEL%"

endlocal & exit /b %EXIT_CODE%
