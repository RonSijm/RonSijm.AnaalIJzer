[CmdletBinding()]
param(
	[string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptDirectory "..\..\..")).Path
$project = Join-Path $repositoryRoot "src\Tools\RonSijm.Anaaltomy\RonSijm.Anaaltomy.csproj"
$sourceOutput = Join-Path $repositoryRoot "src\Tools\RonSijm.Anaaltomy\bin\$Configuration\net10.0"
$artifactOutput = Join-Path $repositoryRoot "build\Artifacts\Anaaltomy"
$packageOutput = Join-Path $artifactOutput "Packages"

function Remove-DirectoryInsideRepository([string]$Path) {
	$fullPath = [System.IO.Path]::GetFullPath($Path)
	if (-not $fullPath.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
		throw "Refusing to remove a path outside the repository: $fullPath"
	}

	if (Test-Path -LiteralPath $fullPath) {
		Remove-Item -LiteralPath $fullPath -Recurse -Force
	}
}

Remove-DirectoryInsideRepository $artifactOutput
New-Item -ItemType Directory -Force -Path $artifactOutput | Out-Null
New-Item -ItemType Directory -Force -Path $packageOutput | Out-Null

Write-Host "Building and packing Anaaltomy..."
& dotnet build $project --configuration $Configuration --disable-build-servers -m:1 -p:UseSharedCompilation=false -p:PackageOutputPath=$packageOutput -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) {
	exit $LASTEXITCODE
}

Copy-Item -Path (Join-Path $sourceOutput "*") -Destination $artifactOutput -Recurse -Force

$package = Get-ChildItem -LiteralPath $packageOutput -Filter "RonSijm.Anaaltomy.*.nupkg" |
	Where-Object { $_.Name -notlike "*.snupkg" } |
	Select-Object -First 1
if ($null -eq $package) {
	throw "Expected Anaaltomy NuGet package was not created in $packageOutput."
}

Write-Host ""
Write-Host "Build succeeded."
Write-Host "Artifacts: $artifactOutput"
Write-Host "NuGet package: $($package.FullName)"
