param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-DotNet([string[]]$Arguments) {
    $output = & dotnet @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') returned exit code $LASTEXITCODE.$([Environment]::NewLine)$output"
    }

    return $output
}

function Write-Utf8File([string]$Path, [string]$Content) {
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("IJzer-iac-package-smoke-" + [Guid]::NewGuid().ToString("N"))
$previousNuGetPackages = $env:NUGET_PACKAGES

try {
    $packageDirectory = Split-Path -Parent $resolvedPackagePath
    $extractedPackageDirectory = Join-Path $temporaryDirectory "package"
    $consumerDirectory = Join-Path $temporaryDirectory "consumer"
    $globalPackagesDirectory = Join-Path $temporaryDirectory "global-packages"
    [System.IO.Directory]::CreateDirectory($extractedPackageDirectory) | Out-Null
    [System.IO.Directory]::CreateDirectory($consumerDirectory) | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($resolvedPackagePath, $extractedPackageDirectory)

    $nuspecPath = Get-ChildItem -LiteralPath $extractedPackageDirectory -Filter "*.nuspec" | Select-Object -First 1 -ExpandProperty FullName
    if ([string]::IsNullOrWhiteSpace($nuspecPath)) {
        throw "The IaC package does not contain a .nuspec file."
    }

    [xml]$nuspec = Get-Content -Raw -LiteralPath $nuspecPath
    $dependencies = @($nuspec.SelectNodes("//*[local-name()='dependency']"))
    if ($dependencies.Count -gt 0) {
        $names = $dependencies.id -join ", "
        throw "The IaC package must not declare runtime dependencies: $names."
    }

    $assemblyPath = Join-Path $extractedPackageDirectory "lib\netstandard2.0\RonSijm.IJzer.IaC.dll"
    if (-not (Test-Path -LiteralPath $assemblyPath)) {
        throw "The IaC package is missing lib/netstandard2.0/RonSijm.IJzer.IaC.dll."
    }

    $packageVersion = [string]$nuspec.package.metadata.version
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
      <package pattern="RonSijm.IJzer.IaC" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@
    $consumerProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="RonSijm.IJzer.IaC" Version="$packageVersion" />
  </ItemGroup>
</Project>
"@
    $consumerSource = @"
using RonSijm.IJzer.IaC;

var configuration = new ArchitectureConfiguration
{
    Elements =
    {
        new Layer("Waiter") { Elements = { new ClassMatcher { EndsWith = "Waiter" } } },
        new Layer("Chef") { Elements = { new ClassMatcher { EndsWith = "Chef" } } },
        new AllowedDependency("Waiter", "Chef")
    }
};

configuration.Save("Architecture.anl");
"@

    Write-Utf8File (Join-Path $consumerDirectory "NuGet.Config") $nuGetConfig
    Write-Utf8File (Join-Path $consumerDirectory "PackageSmoke.csproj") $consumerProject
    Write-Utf8File (Join-Path $consumerDirectory "Program.cs") $consumerSource

    Push-Location $consumerDirectory
    try {
        $env:NUGET_PACKAGES = $globalPackagesDirectory
        Invoke-DotNet @("restore", "PackageSmoke.csproj", "--configfile", "NuGet.Config", "--no-cache", "-p:NuGetAudit=false") | Out-Null
        Invoke-DotNet @("run", "--project", "PackageSmoke.csproj", "--no-restore", "--nologo") | Out-Null
    }
    finally {
        Pop-Location
    }

    $generatedPath = Join-Path $consumerDirectory "Architecture.anl"
    if (-not (Test-Path -LiteralPath $generatedPath)) {
        throw "The clean IaC package consumer did not generate Architecture.anl."
    }

    $generatedConfiguration = Get-Content -Raw -LiteralPath $generatedPath
    if (-not $generatedConfiguration.Contains('<Layer name="Waiter">') -or
        -not $generatedConfiguration.Contains('<AllowedDependency from="Waiter" to="Chef" />')) {
        throw "The clean IaC package consumer generated unexpected configuration content."
    }

    Write-Host "Verified dependency-free IaC package $([System.IO.Path]::GetFileName($resolvedPackagePath))."
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
