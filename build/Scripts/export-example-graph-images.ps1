[CmdletBinding()]
param(
	[string]$Configuration = "Release",
	[string]$OutputDirectory = "",
	[switch]$NoBuild,
	[switch]$FailOnError,
	[int]$Width = 1600,
	[int]$Height = 1000
)

$ErrorActionPreference = "Stop"
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptDirectory "..\..")
$usingDefaultOutputDirectory = [string]::IsNullOrWhiteSpace($OutputDirectory)
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
	$OutputDirectory = Join-Path $repositoryRoot "build\Artifacts\ExampleGraphImages"
}

$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot "build\Artifacts"))
$fullOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
if ($usingDefaultOutputDirectory -and $fullOutputDirectory.StartsWith($artifactsRoot, [System.StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $fullOutputDirectory)) {
	Remove-Item -LiteralPath $fullOutputDirectory -Recurse -Force
}

$projectPath = Join-Path $repositoryRoot "src\Tools\RonSijm.AnaalIJzer.GraphEditor.Standalone\RonSijm.AnaalIJzer.GraphEditor.Standalone.csproj"
if (-not $NoBuild) {
	Write-Host "Building AnaalIJzer Graph Editor..."
	dotnet build $projectPath --configuration $Configuration
	if ($LASTEXITCODE -ne 0) {
		exit $LASTEXITCODE
	}
}

$executablePath = Join-Path $repositoryRoot "src\Tools\RonSijm.AnaalIJzer.GraphEditor.Standalone\bin\$Configuration\net10.0-windows\RonSijm.AnaalIJzer.GraphEditor.Standalone.exe"
if (-not (Test-Path $executablePath)) {
	throw "AnaalIJzer Graph Editor executable was not found: $executablePath"
}

$examplesRoot = Join-Path $repositoryRoot "Examples"
$arguments = @(
	"--export-examples",
	$examplesRoot,
	$OutputDirectory,
	"--configuration",
	$Configuration,
	"--width",
	$Width,
	"--height",
	$Height
)
if ($FailOnError) {
	$arguments += "--fail-on-error"
}

Write-Host "Exporting example graph images to $OutputDirectory..."
$process = Start-Process -FilePath $executablePath -ArgumentList $arguments -Wait -PassThru
$exampleProjects = Get-ChildItem $examplesRoot -Recurse -File -Filter "*.csproj" | Sort-Object FullName
$missingImages = [System.Collections.Generic.List[string]]::new()
foreach ($exampleProject in $exampleProjects) {
	$projectName = [System.IO.Path]::GetFileNameWithoutExtension($exampleProject.Name)
	$artifactImage = Join-Path $OutputDirectory "$projectName-Graph.png"
	$exampleImage = Join-Path $exampleProject.DirectoryName "$projectName-Graph.png"
	if (Test-Path -LiteralPath $artifactImage) {
		Copy-Item -LiteralPath $artifactImage -Destination $exampleImage -Force
		continue
	}

	$missingImages.Add($artifactImage)
}

$imageCount = (Get-ChildItem $OutputDirectory -File -Filter "*.png" -ErrorAction SilentlyContinue | Measure-Object).Count
$projectCount = ($exampleProjects | Measure-Object).Count
Write-Host "Generated $imageCount graph image(s) for $projectCount example project(s)."
if ($missingImages.Count -gt 0) {
	Write-Warning "Missing graph image(s):"
	foreach ($missingImage in $missingImages) {
		Write-Warning "  $missingImage"
	}

	if ($FailOnError) {
		exit 1
	}
}

exit $process.ExitCode
