@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..\..") do set "ROOT=%%~fI"
set "SOLUTION=%ROOT%\src\Extensions\RonSijm.AnaalIJzer.VisualStudio\RonSijm.AnaalIJzer.VisualStudio.slnx"
set "VSIX_OUT=%ROOT%\src\Extensions\RonSijm.AnaalIJzer.VisualStudio\bin\Release\net472\RonSijm.AnaalIJzer.VisualStudio.vsix"
set "ARTIFACT_OUT=%ROOT%\build\Artifacts\VisualStudio"
set "ARTIFACT_VSIX=%ARTIFACT_OUT%\RonSijm.AnaalIJzer.VisualStudio.vsix"

echo Building AnaalIJzer Visual Studio extension...
dotnet build "%SOLUTION%" --configuration Release
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

if not exist "%VSIX_OUT%" (
    echo Expected VSIX was not created:
    echo %VSIX_OUT%
    exit /b 1
)

if exist "%ARTIFACT_OUT%" rmdir /s /q "%ARTIFACT_OUT%"
mkdir "%ARTIFACT_OUT%" >nul 2>nul
copy /y "%VSIX_OUT%" "%ARTIFACT_VSIX%" >nul
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo.
echo Build succeeded.
echo Artifacts: %ARTIFACT_OUT%
echo VSIX: %ARTIFACT_VSIX%
exit /b 0
