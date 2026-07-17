param(
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$SkipPack,
    [switch]$PublishNuGet,
    [string]$NuGetSource = $(if ($env:NUGET_SOURCE) { $env:NUGET_SOURCE } else { "https://api.nuget.org/v3/index.json" }),
    [string]$NuGetApiKey = $env:NUGET_API_KEY
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptDirectory "..\..")
$artifactRoot = Join-Path $repositoryRoot "build\Artifacts"
$packageOutput = Join-Path $artifactRoot "packages"
$sourceSolution = Join-Path $repositoryRoot "src\RonSijm.AnaalIJzer.CrossPlatform.slnx"
$analyzerProject = Join-Path $repositoryRoot "src\Main\RonSijm.AnaalIJzer\RonSijm.AnaalIJzer.csproj"
$arseProject = Join-Path $repositoryRoot "src\Tools\RonSijm.AnaalIJzer.Arse\RonSijm.AnaalIJzer.Arse.csproj"
$testProjects = @(
    "src\Tests\RonSijm.AnaalIJzer.UnitTests\RonSijm.AnaalIJzer.UnitTests.csproj",
    "src\Tests\RonSijm.AnaalIJzer.ConfigurationEditing.Tests\RonSijm.AnaalIJzer.ConfigurationEditing.Tests.csproj",
    "src\Tests\RonSijm.AnaalIJzer.Graphing.Tests\RonSijm.AnaalIJzer.Graphing.Tests.csproj",
    "src\Tests\RonSijm.AnaalIJzer.Tooling.Tests\RonSijm.AnaalIJzer.Tooling.Tests.csproj",
    "src\Tests\RonSijm.AnaalIJzer.Arse.Tests\RonSijm.AnaalIJzer.Arse.Tests.csproj",
    "src\Tests\RonSijm.AnaalIJzer.IntegrationTests\RonSijm.AnaalIJzer.IntegrationTests.csproj"
)

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "=== $Name ==="
    & $Action
}

function Invoke-NativeCommand {
    param(
        [string]$FileName,
        [string[]]$Arguments
    )

    & $FileName @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FileName $($Arguments -join ' ')"
    }
}

Invoke-Step "Restore" {
    Invoke-NativeCommand "dotnet" @("restore", $sourceSolution)
}

Invoke-Step "Build src" {
    Invoke-NativeCommand "dotnet" @("build", $sourceSolution, "--configuration", $Configuration, "--no-restore", "-p:GeneratePackageOnBuild=false")
}

if (-not $SkipTests) {
    Invoke-Step "Test" {
        foreach ($testProject in $testProjects) {
            Invoke-NativeCommand "dotnet" @("test", (Join-Path $repositoryRoot $testProject), "--configuration", $Configuration, "--no-build")
        }
    }
}

if (-not $SkipPack -or $PublishNuGet) {
    Invoke-Step "Pack NuGet artifacts" {
        if (Test-Path $packageOutput) {
            Remove-Item $packageOutput -Recurse -Force
        }

        New-Item -ItemType Directory -Path $packageOutput | Out-Null
        Invoke-NativeCommand "dotnet" @("pack", $analyzerProject, "--configuration", $Configuration, "--no-build", "--output", $packageOutput)
        Invoke-NativeCommand "dotnet" @("pack", $arseProject, "--configuration", $Configuration, "--no-build", "--output", $packageOutput)
    }
}

if ($PublishNuGet) {
    if ([string]::IsNullOrWhiteSpace($NuGetApiKey)) {
        throw "NuGet publishing was requested, but NUGET_API_KEY was not provided."
    }

    Invoke-Step "Publish NuGet artifacts" {
        $packages = Get-ChildItem $packageOutput -Filter "*.nupkg"
        foreach ($package in $packages) {
            Invoke-NativeCommand "dotnet" @("nuget", "push", $package.FullName, "--api-key", $NuGetApiKey, "--source", $NuGetSource, "--skip-duplicate")
        }
    }
}

Write-Host ""
Write-Host "Build script completed."
