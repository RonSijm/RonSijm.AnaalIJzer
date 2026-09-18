@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0generate-self-anaaltomy-graphs.ps1" %*
exit /b %ERRORLEVEL%
