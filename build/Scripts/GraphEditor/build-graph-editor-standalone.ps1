[CmdletBinding()]
param(
	[string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptDirectory "..\..\..")).Path
$project = Join-Path $repositoryRoot "src\Tools\RonSijm.AnaalIJzer.GraphEditor.Standalone\RonSijm.AnaalIJzer.GraphEditor.Standalone.csproj"
$sourceOutput = Join-Path $repositoryRoot "src\Tools\RonSijm.AnaalIJzer.GraphEditor.Standalone\bin\$Configuration\net10.0-windows"
$artifactOutput = Join-Path $repositoryRoot "build\Artifacts\GraphEditor.Standalone"
$executableOutput = Join-Path $artifactOutput "RonSijm.AnaalIJzer.GraphEditor.Standalone.exe"

function Remove-DirectoryInsideRepository([string]$Path) {
	$fullPath = [System.IO.Path]::GetFullPath($Path)
	if (-not $fullPath.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
		throw "Refusing to remove a path outside the repository: $fullPath"
	}

	if (Test-Path -LiteralPath $fullPath) {
		Remove-Item -LiteralPath $fullPath -Recurse -Force
	}
}

Write-Host "Building AnaalIJzer standalone graph editor..."
& dotnet build $project --configuration $Configuration --disable-build-servers -m:1 -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) {
	exit $LASTEXITCODE
}

Remove-DirectoryInsideRepository (Join-Path $sourceOutput "publish")
Remove-DirectoryInsideRepository $artifactOutput
New-Item -ItemType Directory -Force -Path $artifactOutput | Out-Null
Copy-Item -Path (Join-Path $sourceOutput "*") -Destination $artifactOutput -Recurse -Force

Write-Host ""
Write-Host "Build succeeded."
Write-Host "Artifacts: $artifactOutput"
Write-Host "EXE: $executableOutput"
