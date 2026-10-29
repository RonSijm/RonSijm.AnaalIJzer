@echo off
setlocal

set "RepositoryRoot=%~dp0..\..\.."
set "ForgeProject=%RepositoryRoot%\src\Tools\RonSijm.IJzer.Forge\RonSijm.IJzer.Forge.csproj"
set "SolutionPath=%RepositoryRoot%\src\RonSijm.IJzer.slnx"
set "OutputPath=%RepositoryRoot%\build\Artifacts\architecture-health-self.md"

dotnet run --project "%ForgeProject%" -- inspect --solution "%SolutionPath%" --output "%OutputPath%" --force
exit /b %ERRORLEVEL%
