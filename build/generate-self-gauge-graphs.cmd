@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0generate-self-gauge-graphs.ps1" %*
exit /b %ERRORLEVEL%
