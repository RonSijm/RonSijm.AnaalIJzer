@echo off
setlocal

set PROJECT=src\Main\RonSijm.AnaalIJzer\RonSijm.AnaalIJzer.csproj
set OUT=artifacts\packages

echo.
echo === Building NuGet package ===
echo Project : %PROJECT%
echo Output  : %OUT%
echo.

dotnet pack %PROJECT% --configuration Release --output %OUT%

if %ERRORLEVEL% neq 0 (
    echo.
    echo Build FAILED.
    exit /b %ERRORLEVEL%
)

echo.
echo Package written to %OUT%:
dir /b %OUT%\*.nupkg 2>nul
dir /b %OUT%\*.snupkg 2>nul

endlocal
