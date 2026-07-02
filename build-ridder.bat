@echo off
setlocal

set "PROJECT=%~dp0src\Tools\RonSijm.AnaalIJzer.Ridder\RonSijm.AnaalIJzer.Ridder.csproj"

echo Building Ridder...
dotnet build "%PROJECT%" --configuration Release
exit /b %ERRORLEVEL%
