@echo off
setlocal

if /i "%~1"=="--help" goto help
if /i "%~1"=="/?" goto help
set "DETECT_ONLY="
if /i "%~1"=="--detect" set "DETECT_ONLY=1"
set "DEPLOY_ONLY="
if /i "%~1"=="--deploy-only" set "DEPLOY_ONLY=1"

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..\..") do set "ROOT=%%~fI"
set "VSIX_PROJECT=%ROOT%\src\Extensions\RonSijm.AnaalIJzer.VisualStudio\RonSijm.AnaalIJzer.VisualStudio.csproj"
set "VSIX_OUT=%ROOT%\src\Extensions\RonSijm.AnaalIJzer.VisualStudio\bin\Release\net472\RonSijm.AnaalIJzer.VisualStudio.vsix"
set "TARGET=%~1"
if defined DETECT_ONLY set "TARGET="
if defined DEPLOY_ONLY set "TARGET="
set "ROOT_SUFFIX=%~2"
if "%ROOT_SUFFIX%"=="" set "ROOT_SUFFIX=Exp"

set "VSWHERE="
set "DEFAULT_VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%DEFAULT_VSWHERE%" set "VSWHERE=%DEFAULT_VSWHERE%"
if defined VSWHERE goto haveVswhere

for /f "delims=" %%I in ('where vswhere 2^>nul') do (
    if not defined VSWHERE set "VSWHERE=%%I"
)

:haveVswhere
if not exist "%VSWHERE%" (
    echo Could not find vswhere.exe.
    echo Install Visual Studio 2026 or add vswhere.exe to PATH.
    exit /b 1
)

set "DEVENV="
set "MSBUILD="
set "VS_INSTANCE_ID="
set "VSWHERE_RESULT=%TEMP%\AnaalIJzer-vswhere-%RANDOM%-%RANDOM%.txt"
"%VSWHERE%" -latest -prerelease -products * -version "[18.0,19.0)" -requires Microsoft.VisualStudio.Component.CoreEditor -find "Common7\IDE\devenv.exe" > "%VSWHERE_RESULT%"
if %ERRORLEVEL% neq 0 (
    del "%VSWHERE_RESULT%" >nul 2>nul
    echo vswhere failed while searching for Visual Studio 2026.
    exit /b %ERRORLEVEL%
)

set /p "DEVENV="<"%VSWHERE_RESULT%"
del "%VSWHERE_RESULT%" >nul 2>nul

set "VSWHERE_RESULT=%TEMP%\AnaalIJzer-vswhere-%RANDOM%-%RANDOM%.txt"
"%VSWHERE%" -latest -prerelease -products * -version "[18.0,19.0)" -requires Microsoft.VisualStudio.Component.CoreEditor -find "MSBuild\Current\Bin\MSBuild.exe" > "%VSWHERE_RESULT%"
if %ERRORLEVEL% neq 0 (
    del "%VSWHERE_RESULT%" >nul 2>nul
    echo vswhere failed while searching for Visual Studio 2026 MSBuild.
    exit /b %ERRORLEVEL%
)

set /p "MSBUILD="<"%VSWHERE_RESULT%"
del "%VSWHERE_RESULT%" >nul 2>nul

set "VSWHERE_RESULT=%TEMP%\AnaalIJzer-vswhere-%RANDOM%-%RANDOM%.txt"
"%VSWHERE%" -latest -prerelease -products * -version "[18.0,19.0)" -requires Microsoft.VisualStudio.Component.CoreEditor -property instanceId > "%VSWHERE_RESULT%"
if %ERRORLEVEL% neq 0 (
    del "%VSWHERE_RESULT%" >nul 2>nul
    echo vswhere failed while reading the Visual Studio 2026 instance id.
    exit /b %ERRORLEVEL%
)

set /p "VS_INSTANCE_ID="<"%VSWHERE_RESULT%"
del "%VSWHERE_RESULT%" >nul 2>nul

if not defined DEVENV (
    echo Could not find Visual Studio 2026 devenv.exe.
    echo Expected a VS installation in version range [18.0,19.0^).
    exit /b 1
)

if not defined MSBUILD (
    echo Could not find Visual Studio 2026 MSBuild.exe.
    exit /b 1
)

if not defined VS_INSTANCE_ID (
    echo Could not determine the Visual Studio 2026 instance id.
    exit /b 1
)

for %%I in ("%DEVENV%") do set "VS_IDE=%%~dpI"

if not defined DETECT_ONLY goto afterDetectOnly
echo vswhere       : %VSWHERE%
echo devenv        : %DEVENV%
echo MSBuild       : %MSBUILD%
echo InstanceId    : %VS_INSTANCE_ID%
exit /b 0

:afterDetectOnly

tasklist /fi "imagename eq devenv.exe" 2>nul | findstr /i "devenv.exe" >nul
if %ERRORLEVEL% equ 0 (
echo Visual Studio is already running.
echo That is OK for your source-code Visual Studio instance. If deployment fails,
echo close only the experimental instance for rootSuffix '%ROOT_SUFFIX%' and rerun.
echo.
)

echo Building AnaalIJzer Visual Studio extension...
"%MSBUILD%" "%VSIX_PROJECT%" /nologo /restore /target:Build /verbosity:minimal /property:Configuration=Release
if %ERRORLEVEL% neq 0 (
    echo.
    echo Build failed with exit code %ERRORLEVEL%.
    exit /b %ERRORLEVEL%
)

if not exist "%VSIX_OUT%" (
    echo Expected VSIX was not created:
    echo %VSIX_OUT%
    exit /b 1
)

set "EXPERIMENTAL_ROOT=%LOCALAPPDATA%\Microsoft\VisualStudio\18.0_%VS_INSTANCE_ID%%ROOT_SUFFIX%"
set "EXPERIMENTAL_EXTENSIONS=%EXPERIMENTAL_ROOT%\Extensions"
set "ANAAL_DEV_EXT=%EXPERIMENTAL_EXTENSIONS%\RonSijm\AnaalIJzer Visual Studio Companion\Development"

echo Deploying VSIX files to experimental hive '%ROOT_SUFFIX%'...
echo %ANAAL_DEV_EXT%
powershell -NoProfile -ExecutionPolicy Bypass -Command "$destination = [IO.Path]::GetFullPath($env:ANAAL_DEV_EXT); $expectedRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA ('Microsoft\VisualStudio\18.0_' + $env:VS_INSTANCE_ID + $env:ROOT_SUFFIX + '\Extensions'))); if (-not $destination.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw ('Refusing to deploy outside experimental Extensions root: ' + $destination) }; New-Item -ItemType Directory -Force -Path $expectedRoot | Out-Null; if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }; New-Item -ItemType Directory -Force -Path $destination | Out-Null; Expand-Archive -LiteralPath $env:VSIX_OUT -DestinationPath $destination -Force; New-Item -ItemType File -Force -Path (Join-Path $expectedRoot 'extensions.configurationchanged') | Out-Null"
if %ERRORLEVEL% neq 0 (
    echo.
    echo Deployment failed while writing the experimental extension folder.
    echo If the experimental Visual Studio instance is already open, close only that
    echo experimental instance and rerun this script. Your source-code Visual Studio
    echo instance can stay open.
    exit /b %ERRORLEVEL%
)

echo Refreshing Visual Studio experimental extension cache...
"%DEVENV%" /rootsuffix "%ROOT_SUFFIX%" /updateconfiguration
if %ERRORLEVEL% neq 0 (
    echo Warning: Visual Studio returned %ERRORLEVEL% while refreshing the experimental extension cache.
    echo The extension may not appear until the experimental instance is closed and started again.
)

if not defined DEPLOY_ONLY goto afterDeployOnly
echo.
echo Deployment completed for experimental hive '%ROOT_SUFFIX%'.
echo VSIX: %VSIX_OUT%
exit /b 0

:afterDeployOnly

set "ACTIVITY_LOG=%TEMP%\AnaalIJzer-vs-%ROOT_SUFFIX%-ActivityLog.xml"
echo.
echo Launching Visual Studio experimental instance '%ROOT_SUFFIX%'...
echo Activity log: %ACTIVITY_LOG%
if "%TARGET%"=="" (
    start "" "%DEVENV%" /rootsuffix "%ROOT_SUFFIX%" /log "%ACTIVITY_LOG%"
) else (
    start "" "%DEVENV%" /rootsuffix "%ROOT_SUFFIX%" /log "%ACTIVITY_LOG%" "%TARGET%"
)

exit /b 0

:help
echo Launches Visual Studio 2026 with the AnaalIJzer VSIX installed in an experimental hive.
echo.
echo Usage:
echo   %~nx0 [solution-or-folder] [rootSuffix]
echo   %~nx0 --detect
echo   %~nx0 --deploy-only [rootSuffix]
echo.
echo Examples:
echo   %~nx0
echo   %~nx0 D:\source\Exquise\src\backend\core\Connect\BsExquise.Connect.slnx
echo   %~nx0 D:\source\Exquise\src\backend\core\Connect\BsExquise.Connect.slnx AnaalIJzerExp
echo   %~nx0 --detect
echo   %~nx0 --deploy-only
echo   %~nx0 --deploy-only AnaalIJzerExp
echo.
echo Defaults:
echo   rootSuffix = Exp
echo.
echo Notes:
echo   - The script requires Visual Studio 2026, detected by vswhere as version [18.0,19.0).
echo   - Your source-code Visual Studio instance can stay open.
echo   - If deployment fails, close only the experimental instance for the selected rootSuffix.
echo   - The VSIX is built with Visual Studio MSBuild and extracted into the experimental hive.
echo   - The experimental extension cache is refreshed with devenv /updateconfiguration.
echo   - The normal Visual Studio profile is not used when /rootsuffix is set.
echo   - --detect only prints the Visual Studio paths and does not build, deploy, or launch.
echo   - --deploy-only builds and deploys to the experimental hive without launching Visual Studio.
exit /b 0

