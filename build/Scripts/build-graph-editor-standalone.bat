@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..\..") do set "ROOT=%%~fI"
set "PROJECT=%ROOT%\src\Tools\RonSijm.AnaalIJzer.GraphEditor.Standalone\RonSijm.AnaalIJzer.GraphEditor.Standalone.csproj"
set "SOURCE_OUT=%ROOT%\src\Tools\RonSijm.AnaalIJzer.GraphEditor.Standalone\bin\Release\net10.0-windows"
set "ARTIFACT_OUT=%ROOT%\build\Artifacts\GraphEditor.Standalone"
set "EXE_OUT=%ARTIFACT_OUT%\RonSijm.AnaalIJzer.GraphEditor.Standalone.exe"

echo Building AnaalIJzer standalone graph editor...
dotnet build "%PROJECT%" --configuration Release
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

if exist "%ARTIFACT_OUT%" rmdir /s /q "%ARTIFACT_OUT%"
mkdir "%ARTIFACT_OUT%" >nul 2>nul
xcopy "%SOURCE_OUT%\*" "%ARTIFACT_OUT%\" /e /i /y >nul
if %ERRORLEVEL% geq 4 exit /b %ERRORLEVEL%

echo.
echo Build succeeded.
echo Artifacts: %ARTIFACT_OUT%
echo EXE: %EXE_OUT%
exit /b 0
