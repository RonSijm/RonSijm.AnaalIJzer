[CmdletBinding()]
param(
	[string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptDirectory "..\..\..")).Path
$solution = Join-Path $repositoryRoot "src\Extensions\RonSijm.IJzer.VisualStudio\RonSijm.IJzer.VisualStudio.slnx"
$vsixOutput = Join-Path $repositoryRoot "src\Extensions\RonSijm.IJzer.VisualStudio\bin\$Configuration\net472\RonSijm.IJzer.VisualStudio.vsix"
$artifactOutput = Join-Path $repositoryRoot "build\Artifacts\VisualStudio"
$artifactVsix = Join-Path $artifactOutput "RonSijm.IJzer.VisualStudio.vsix"

function Remove-DirectoryInsideRepository([string]$Path) {
	$fullPath = [System.IO.Path]::GetFullPath($Path)
	if (-not $fullPath.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
		throw "Refusing to remove a path outside the repository: $fullPath"
	}

	if (Test-Path -LiteralPath $fullPath) {
		Remove-Item -LiteralPath $fullPath -Recurse -Force
	}
}

Write-Host "Building IJzer Visual Studio extension..."
& dotnet build $solution --configuration $Configuration --disable-build-servers -m:1 -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) {
	exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $vsixOutput)) {
	throw "Expected VSIX was not created: $vsixOutput"
}

Remove-DirectoryInsideRepository $artifactOutput
New-Item -ItemType Directory -Force -Path $artifactOutput | Out-Null
Copy-Item -LiteralPath $vsixOutput -Destination $artifactVsix -Force

Write-Host ""
Write-Host "Build succeeded."
Write-Host "Artifacts: $artifactOutput"
Write-Host "VSIX: $artifactVsix"
