@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..\..") do set "ROOT=%%~fI"
set "ANALYZER_PROJECT=%ROOT%\src\Main\RonSijm.AnaalIJzer\RonSijm.AnaalIJzer.csproj"
set "ARSE_PROJECT=%ROOT%\src\Tools\RonSijm.AnaalIJzer.Arse\RonSijm.AnaalIJzer.Arse.csproj"
set "OUT=%ROOT%\build\Artifacts\Packages"

echo.
echo === Building NuGet packages ===
echo Analyzer: %ANALYZER_PROJECT%
echo Arse    : %ARSE_PROJECT%
echo Output  : %OUT%
echo.

if exist "%OUT%" rmdir /s /q "%OUT%"
mkdir "%OUT%" >nul 2>nul

dotnet pack "%ANALYZER_PROJECT%" --configuration Release --output "%OUT%"
if %ERRORLEVEL% neq 0 (
    echo.
    echo Analyzer package build FAILED.
    exit /b %ERRORLEVEL%
)

dotnet pack "%ARSE_PROJECT%" --configuration Release --output "%OUT%"
if %ERRORLEVEL% neq 0 (
    echo.
    echo Arse package build FAILED.
    exit /b %ERRORLEVEL%
)

echo.
echo Packages written to %OUT%:
dir /b "%OUT%\*.nupkg" 2>nul
dir /b "%OUT%\*.snupkg" 2>nul

endlocal
