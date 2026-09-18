[CmdletBinding()]
param(
	[string]$Configuration = "Release",
	[string]$Framework,
	[ValidateSet("auto", "never", "always")]
	[string]$RestoreMode = "never",
	[switch]$IncludeGenerated
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptDirectory "..")).Path
$solution = Join-Path $repositoryRoot "RonSijm.AnaalIJzer.WithExamples.slnx"
$project = Join-Path $repositoryRoot "src\Tools\RonSijm.Anaaltomy\RonSijm.Anaaltomy.csproj"
$tool = Join-Path $repositoryRoot "src\Tools\RonSijm.Anaaltomy\bin\$Configuration\net10.0\RonSijm.Anaaltomy.dll"
$artifactDirectory = Join-Path $repositoryRoot "build\Artifacts\Anaaltomy"
$databasePath = Join-Path $artifactDirectory "anaalijzer.db"

function Invoke-Anaaltomy([string[]]$Arguments) {
	& dotnet $tool @Arguments
	if ($LASTEXITCODE -ne 0) {
		exit $LASTEXITCODE
	}
}

Write-Host "Building Anaaltomy..."
& dotnet build $project --configuration $Configuration --disable-build-servers -m:1 -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) {
	exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $tool)) {
	throw "Anaaltomy was not built at expected path: $tool"
}

New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null

if (Test-Path -LiteralPath $databasePath) {
	Remove-Item -LiteralPath $databasePath -Force
}

Get-ChildItem -LiteralPath $artifactDirectory -File -Filter "*.png" | Remove-Item -Force

if (Test-Path -LiteralPath $solution) {
	Write-Host "Scan target: $solution"
	$scanArguments = @("scan", "--solution", $solution)
} else {
	Write-Host "Scan target: $repositoryRoot"
	$scanArguments = @("scan", "--directory", $repositoryRoot)
}

$scanArguments += @(
	"--database", $databasePath,
	"--configuration", $Configuration,
	"--restore-mode", $RestoreMode,
	"--allow-partial"
)

if (-not [string]::IsNullOrWhiteSpace($Framework)) {
	$scanArguments += @("--framework", $Framework)
}

if ($IncludeGenerated) {
	$scanArguments += "--include-generated"
}

Write-Host ""
Write-Host "Scanning AnaalIJzer source tree..."
Invoke-Anaaltomy $scanArguments

Write-Host ""
Write-Host "Rendering AnaalIJzer charts..."
Invoke-Anaaltomy @("chart", "--database", $databasePath, "--output-directory", $artifactDirectory)
Invoke-Anaaltomy @("chart", "--database", $databasePath, "--output-directory", $artifactDirectory, "--group")

Write-Host ""
Write-Host "Anaaltomy graph generation completed."
Write-Host "Database: $databasePath"
Write-Host "Charts: $artifactDirectory"
