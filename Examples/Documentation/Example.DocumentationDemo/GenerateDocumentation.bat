@echo off
setlocal

set "EXAMPLE_DIRECTORY=%~dp0"
set "FORGE_PROJECT=%EXAMPLE_DIRECTORY%..\..\..\src\Tools\RonSijm.IJzer.Forge\RonSijm.IJzer.Forge.csproj"
set "SETTINGS_FILE=%EXAMPLE_DIRECTORY%Architecture.anl"

dotnet run --project "%FORGE_PROJECT%" -- documentation --config "%SETTINGS_FILE%" --force
exit /b %ERRORLEVEL%
