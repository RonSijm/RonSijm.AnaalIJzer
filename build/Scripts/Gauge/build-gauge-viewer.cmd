@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-gauge-viewer.ps1" %*
