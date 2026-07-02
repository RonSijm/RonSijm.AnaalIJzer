@echo off
setlocal

set "EXAMPLE_DIRECTORY=%~dp0"
set "RIDDER_PROJECT=%EXAMPLE_DIRECTORY%..\..\..\src\Tools\RonSijm.AnaalIJzer.Ridder\RonSijm.AnaalIJzer.Ridder.csproj"
set "SETTINGS_FILE=%EXAMPLE_DIRECTORY%ArchitecturalLevels.xml"

dotnet run --project "%RIDDER_PROJECT%" -- documentation --config "%SETTINGS_FILE%" --force
exit /b %ERRORLEVEL%
