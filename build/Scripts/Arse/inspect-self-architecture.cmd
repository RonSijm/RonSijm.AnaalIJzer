@echo off
setlocal

set "RepositoryRoot=%~dp0..\..\.."
set "ArseProject=%RepositoryRoot%\src\Tools\RonSijm.AnaalIJzer.Arse\RonSijm.AnaalIJzer.Arse.csproj"
set "SolutionPath=%RepositoryRoot%\src\RonSijm.AnaalIJzer.slnx"
set "OutputPath=%RepositoryRoot%\build\Artifacts\architecture-health-self.md"

dotnet run --project "%ArseProject%" -- inspect --solution "%SolutionPath%" --output "%OutputPath%" --force
exit /b %ERRORLEVEL%
