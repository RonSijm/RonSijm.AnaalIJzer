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
$repositoryRoot = Resolve-Path (Join-Path $scriptDirectory "..\..\..")
$artifactRoot = Join-Path $repositoryRoot "build\Artifacts"
$packageOutput = Join-Path $artifactRoot "Packages"
$analyzerProject = Join-Path $repositoryRoot "src\Main\RonSijm.IJzer\RonSijm.IJzer.csproj"
$annotationsProject = Join-Path $repositoryRoot "src\Main\RonSijm.IJzer.Annotations\RonSijm.IJzer.Annotations.csproj"
$iacProject = Join-Path $repositoryRoot "src\Main\RonSijm.IJzer.IaC\RonSijm.IJzer.IaC.csproj"
$forgeProject = Join-Path $repositoryRoot "src\Tools\RonSijm.IJzer.Forge\RonSijm.IJzer.Forge.csproj"
$gaugeProject = Join-Path $repositoryRoot "src\Tools\RonSijm.IJzer.Gauge\RonSijm.IJzer.Gauge.csproj"
$testScript = Join-Path $repositoryRoot "build\Scripts\Testing\test-all.ps1"

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

Invoke-Step "Build analyzer" {
    Invoke-NativeCommand "dotnet" @(
        "build",
        $analyzerProject,
        "--configuration", $Configuration,
        "--disable-build-servers",
        "-m:1",
        "-p:UseSharedCompilation=false",
        "-p:GeneratePackageOnBuild=false"
    )
}

Invoke-Step "Build annotations" {
    Invoke-NativeCommand "dotnet" @(
        "build",
        $annotationsProject,
        "--configuration", $Configuration,
        "--disable-build-servers",
        "-m:1",
        "-p:UseSharedCompilation=false",
        "-p:GeneratePackageOnBuild=false"
    )
}

Invoke-Step "Build IaC authoring library" {
    Invoke-NativeCommand "dotnet" @(
        "build",
        $iacProject,
        "--configuration", $Configuration,
        "--disable-build-servers",
        "-m:1",
        "-p:UseSharedCompilation=false",
        "-p:GeneratePackageOnBuild=false"
    )
}

Invoke-Step "Build IJzer Forge" {
    Invoke-NativeCommand "dotnet" @(
        "build",
        $forgeProject,
        "--configuration", $Configuration,
        "--disable-build-servers",
        "-m:1",
        "-p:UseSharedCompilation=false",
        "-p:GeneratePackageOnBuild=false"
    )
}

Invoke-Step "Build IJzer Gauge" {
    Invoke-NativeCommand "dotnet" @(
        "build",
        $gaugeProject,
        "--configuration", $Configuration,
        "--disable-build-servers",
        "-m:1",
        "-p:UseSharedCompilation=false",
        "-p:GeneratePackageOnBuild=false"
    )
}

if (-not $SkipTests) {
    Invoke-Step "Test" {
        $arguments = @(
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", $testScript,
            "-Configuration", $Configuration,
            "-SkipWindowsOnly"
        )

        Invoke-NativeCommand "pwsh" $arguments
    }
}

if (-not $SkipPack -or $PublishNuGet) {
    Invoke-Step "Pack NuGet artifacts" {
        if (Test-Path $packageOutput) {
            Remove-Item $packageOutput -Recurse -Force
        }

        New-Item -ItemType Directory -Path $packageOutput | Out-Null
        Invoke-NativeCommand "dotnet" @("pack", $analyzerProject, "--configuration", $Configuration, "--no-build", "--output", $packageOutput)
        Invoke-NativeCommand "dotnet" @("pack", $annotationsProject, "--configuration", $Configuration, "--no-build", "--output", $packageOutput)
        Invoke-NativeCommand "dotnet" @("pack", $iacProject, "--configuration", $Configuration, "--no-build", "--output", $packageOutput)
        Invoke-NativeCommand "dotnet" @("pack", $forgeProject, "--configuration", $Configuration, "--no-build", "--output", $packageOutput)
        Invoke-NativeCommand "dotnet" @("pack", $gaugeProject, "--configuration", $Configuration, "--no-build", "--output", $packageOutput)
    }

    Invoke-Step "Verify analyzer package consumer" {
        $analyzerPackage = Get-ChildItem -LiteralPath $packageOutput -Filter "RonSijm.IJzer.*.nupkg" |
            Where-Object { $_.BaseName -match '^RonSijm\.IJzer\.\d' } |
            Select-Object -First 1

        if ($null -eq $analyzerPackage) {
            throw "The analyzer package was not found in $packageOutput."
        }

        & (Join-Path $scriptDirectory "verify-analyzer-package.ps1") -PackagePath $analyzerPackage.FullName
    }

    Invoke-Step "Verify annotations package consumer" {
        $annotationsPackage = Get-ChildItem -LiteralPath $packageOutput -Filter "RonSijm.IJzer.Annotations.*.nupkg" |
            Where-Object { $_.Extension -eq ".nupkg" -and -not $_.Name.EndsWith(".snupkg", [StringComparison]::OrdinalIgnoreCase) } |
            Select-Object -First 1

        if ($null -eq $annotationsPackage) {
            throw "The annotations package was not found in $packageOutput."
        }

        & (Join-Path $scriptDirectory "verify-annotations-package.ps1") -PackagePath $annotationsPackage.FullName
    }

    Invoke-Step "Verify IaC package consumer" {
        $iacPackage = Get-ChildItem -LiteralPath $packageOutput -Filter "RonSijm.IJzer.IaC.*.nupkg" |
            Where-Object { $_.Extension -eq ".nupkg" -and -not $_.Name.EndsWith(".snupkg", [StringComparison]::OrdinalIgnoreCase) } |
            Select-Object -First 1

        if ($null -eq $iacPackage) {
            throw "The IaC package was not found in $packageOutput."
        }

        & (Join-Path $scriptDirectory "verify-iac-package.ps1") -PackagePath $iacPackage.FullName
    }

    Invoke-Step "Verify IJzer Gauge global tool package" {
        $gaugePackage = Get-ChildItem -LiteralPath $packageOutput -Filter "RonSijm.IJzer.Gauge.*.nupkg" |
            Where-Object { $_.BaseName -match '^RonSijm\.IJzer\.Gauge\.\d' } |
            Select-Object -First 1

        if ($null -eq $gaugePackage) {
            throw "The IJzer Gauge global tool package was not found in $packageOutput."
        }

        & (Join-Path $scriptDirectory "verify-gauge-package.ps1") -PackagePath $gaugePackage.FullName
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
