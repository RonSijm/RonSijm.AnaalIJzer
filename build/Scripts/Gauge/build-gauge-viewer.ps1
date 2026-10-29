[CmdletBinding()]
param(
	[string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptDirectory "..\..\..")).Path
$project = Join-Path $repositoryRoot "src\Tools\RonSijm.IJzer.Gauge.Viewer\RonSijm.IJzer.Gauge.Viewer.csproj"
$artifactDirectory = Join-Path $repositoryRoot "build\Artifacts\Gauge\Viewer"

if (Test-Path -LiteralPath $artifactDirectory) {
	Remove-Item -LiteralPath $artifactDirectory -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null

dotnet publish $project --configuration $Configuration --output $artifactDirectory --disable-build-servers -m:1 -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) {
	exit $LASTEXITCODE
}

Write-Host "IJzer Gauge viewer published to $artifactDirectory"
