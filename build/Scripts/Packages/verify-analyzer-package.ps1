param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-DotNet([string[]]$Arguments, [int]$ExpectedExitCode = 0) {
    $output = & dotnet @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne $ExpectedExitCode) {
        throw "dotnet $($Arguments -join ' ') returned exit code $LASTEXITCODE instead of $ExpectedExitCode.$([Environment]::NewLine)$output"
    }

    return $output
}

function Write-Utf8File([string]$Path, [string]$Content) {
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("AnaalIJzer-package-smoke-" + [Guid]::NewGuid().ToString("N"))
$previousNuGetPackages = $env:NUGET_PACKAGES

try {
    $packageDirectory = Split-Path -Parent $resolvedPackagePath
    $extractedPackageDirectory = Join-Path $temporaryDirectory "package"
    $consumerDirectory = Join-Path $temporaryDirectory "consumer"
    $targetDirectory = Join-Path $consumerDirectory "target"
    $applicationDirectory = Join-Path $consumerDirectory "application"
    $globalPackagesDirectory = Join-Path $temporaryDirectory "global-packages"

    [System.IO.Directory]::CreateDirectory($extractedPackageDirectory) | Out-Null
    [System.IO.Directory]::CreateDirectory($targetDirectory) | Out-Null
    [System.IO.Directory]::CreateDirectory($applicationDirectory) | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($resolvedPackagePath, $extractedPackageDirectory)

    $nuspecPath = Get-ChildItem -LiteralPath $extractedPackageDirectory -Filter "*.nuspec" | Select-Object -First 1 -ExpandProperty FullName
    if ([string]::IsNullOrWhiteSpace($nuspecPath)) {
        throw "The package does not contain a .nuspec file."
    }

    [xml]$nuspec = Get-Content -Raw -LiteralPath $nuspecPath
    $dependencies = @($nuspec.SelectNodes("//*[local-name()='dependency']"))
    if ($dependencies.Count -gt 0) {
        $names = $dependencies.id -join ", "
        throw "The self-contained analyzer package must not declare NuGet dependencies: $names."
    }

    $packageVersion = [string]$nuspec.package.metadata.version
    foreach ($relativePath in @(
        "analyzers/dotnet/cs/RonSijm.AnaalIJzer.Diagnostics.dll",
        "analyzers/dotnet/cs/RonSijm.AnaalIJzer.Engine.dll",
        "buildTransitive/RonSijm.AnaalIJzer.props",
        "buildTransitive/RonSijm.AnaalIJzer.targets")) {
        $fullPath = Join-Path $extractedPackageDirectory ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $fullPath)) {
            throw "The analyzer package is missing required asset '$relativePath'."
        }
    }

    $escapedPackageDirectory = [System.Security.SecurityElement]::Escape($packageDirectory)
    $nuGetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="package-smoke" value="$escapedPackageDirectory" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="package-smoke">
      <package pattern="RonSijm.AnaalIJzer" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@
    $targetProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
"@
    $consumerProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="RonSijm.AnaalIJzer" Version="$packageVersion" />
    <ProjectReference Include="..\target\PackageSmoke.Target.csproj" />
    <AdditionalFiles Include="Architecture.anl" />
  </ItemGroup>
</Project>
"@
    $configuration = @"
<ArchitecturalLevels>
  <Layer name="Consumer">
    <Class endsWith="Service" />
  </Layer>
  <Layer name="Target">
    <Class endsWith="Repository" />
  </Layer>
  <ProjectArchitecture requireRecognizedProjects="true">
    <ProjectGroup name="Consumer">
      <Project exactName="PackageSmoke.Consumer" />
    </ProjectGroup>
    <ProjectGroup name="Target">
      <Project exactName="PackageSmoke.Target" />
    </ProjectGroup>
    <BlockedProjectReference from="Consumer" to="Target" />
  </ProjectArchitecture>
</ArchitecturalLevels>
"@
    $targetSource = @"
namespace PackageSmoke.Target;

public sealed class TargetRepository
{
}
"@
    $consumerSource = @"
using PackageSmoke.Target;

namespace PackageSmoke.Consumer;

public sealed class ConsumerService(TargetRepository repository)
{
}
"@

    Write-Utf8File (Join-Path $applicationDirectory "NuGet.Config") $nuGetConfig
    Write-Utf8File (Join-Path $targetDirectory "PackageSmoke.Target.csproj") $targetProject
    Write-Utf8File (Join-Path $applicationDirectory "PackageSmoke.Consumer.csproj") $consumerProject
    Write-Utf8File (Join-Path $applicationDirectory "Architecture.anl") $configuration
    Write-Utf8File (Join-Path $targetDirectory "TargetRepository.cs") $targetSource
    Write-Utf8File (Join-Path $applicationDirectory "ConsumerService.cs") $consumerSource

    Push-Location $applicationDirectory
    try {
        $env:NUGET_PACKAGES = $globalPackagesDirectory
        Invoke-DotNet @("restore", "PackageSmoke.Consumer.csproj", "--configfile", "NuGet.Config", "--no-cache") | Out-Null
        $buildOutput = Invoke-DotNet @("build", "PackageSmoke.Consumer.csproj", "--no-restore", "--nologo") 1
        foreach ($diagnosticId in @("ARCH001", "ARCH010")) {
            if ($buildOutput -notmatch $diagnosticId) {
                throw "A clean consumer PackageReference build did not report $diagnosticId.$([Environment]::NewLine)$buildOutput"
            }
        }
    }
    finally {
        Pop-Location
    }

    Write-Host "Verified package $([System.IO.Path]::GetFileName($resolvedPackagePath)) with a clean PackageReference consumer."
}
finally {
    if ($null -eq $previousNuGetPackages) {
        Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
    }
    else {
        $env:NUGET_PACKAGES = $previousNuGetPackages
    }

    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}
