@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%export-example-graph-images.ps1" %*
exit /b %ERRORLEVEL%
