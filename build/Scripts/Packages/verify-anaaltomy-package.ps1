param(
	[Parameter(Mandatory = $true)]
	[string]$PackagePath,
	[switch]$KeepTemporaryFiles
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$packageDirectory = Split-Path -Parent $resolvedPackagePath
$packageName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedPackagePath)
if ($packageName -notmatch '^RonSijm\.Anaaltomy\.(?<version>.+)$') {
	throw "The package name does not look like RonSijm.Anaaltomy: $packageName"
}
$packageVersion = $matches.version

$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("AnaaltomyPackageVerification-" + [guid]::NewGuid().ToString("N"))
$toolPath = Join-Path $temporaryRoot "tool"
$globalPackagesPath = Join-Path $temporaryRoot "global-packages"
$previousNuGetPackages = $env:NUGET_PACKAGES
try {
	New-Item -ItemType Directory -Force -Path $toolPath | Out-Null
	New-Item -ItemType Directory -Force -Path $globalPackagesPath | Out-Null
	$env:NUGET_PACKAGES = $globalPackagesPath
	& dotnet tool install --tool-path $toolPath --add-source $packageDirectory RonSijm.Anaaltomy --version $packageVersion --ignore-failed-sources
	if ($LASTEXITCODE -ne 0) {
		throw "Anaaltomy package installation failed with exit code $LASTEXITCODE."
	}

	$runningOnWindows = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT
	$toolName = if ($runningOnWindows) { "anaaltomy.exe" } else { "anaaltomy" }
	$tool = Join-Path $toolPath $toolName
	& $tool --help
	if ($LASTEXITCODE -ne 0) {
		throw "Installed Anaaltomy tool did not respond to --help."
	}

	$sampleDirectory = Join-Path $temporaryRoot "sample"
	$sampleProjectPath = Join-Path $sampleDirectory "Pizza.csproj"
	$databasePath = Join-Path $sampleDirectory "statistics.db"
	New-Item -ItemType Directory -Force -Path $sampleDirectory | Out-Null
	$sampleProjectXml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
"@
	Set-Content -LiteralPath $sampleProjectPath -Encoding utf8 -Value $sampleProjectXml
	Set-Content -LiteralPath (Join-Path $sampleDirectory "Pizza.cs") -Encoding utf8 -Value "public sealed class Pizza { }"
	& $tool scan --project $sampleProjectPath --database $databasePath --restore-mode always
	if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $databasePath)) {
		throw "Installed Anaaltomy tool could not scan a clean sample project."
	}

	$chartDirectory = Join-Path $sampleDirectory "charts"
	& $tool chart --database $databasePath --output-directory $chartDirectory --dimension TypeKind
	$chartPath = Join-Path $chartDirectory "type-kinds.png"
	if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $chartPath)) {
		throw "Installed Anaaltomy tool could not render a chart from a clean sample scan."
	}
}
finally {
	if ($null -eq $previousNuGetPackages) {
		Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
	}
	else {
		$env:NUGET_PACKAGES = $previousNuGetPackages
	}

	if ($KeepTemporaryFiles) {
		Write-Host "Retained package-verification files at $temporaryRoot."
	}
	elseif (Test-Path -LiteralPath $temporaryRoot) {
		Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
	}
}

Write-Host "Verified global tool package $([System.IO.Path]::GetFileName($resolvedPackagePath))."
