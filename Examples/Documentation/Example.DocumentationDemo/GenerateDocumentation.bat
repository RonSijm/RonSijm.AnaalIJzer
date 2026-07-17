@echo off
setlocal

set "EXAMPLE_DIRECTORY=%~dp0"
set "ARSE_PROJECT=%EXAMPLE_DIRECTORY%..\..\..\src\Tools\RonSijm.AnaalIJzer.Arse\RonSijm.AnaalIJzer.Arse.csproj"
set "SETTINGS_FILE=%EXAMPLE_DIRECTORY%Architecture.anl"

dotnet run --project "%ARSE_PROJECT%" -- documentation --config "%SETTINGS_FILE%" --force
exit /b %ERRORLEVEL%
