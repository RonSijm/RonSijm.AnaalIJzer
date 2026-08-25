[CmdletBinding()]
param(
	[string]$Configuration = "Release",
	[string]$OutputDirectory = "",
	[ValidateSet("Flat", "PreserveStructure", "SideBySide", "FlatAndSideBySide", "All")]
	[string]$Placement = "FlatAndSideBySide",
	[switch]$NoBuild,
	[switch]$FailOnError,
	[int]$Width = 1600,
	[int]$Height = 1000
)

$ErrorActionPreference = "Stop"
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptDirectory "..\..\..")
$usingDefaultOutputDirectory = [string]::IsNullOrWhiteSpace($OutputDirectory)
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
	$OutputDirectory = Join-Path $repositoryRoot "build\Artifacts\ExampleGraphImages"
}

$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot "build\Artifacts"))
$fullOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

function Test-IsInsideDirectory([string]$CandidatePath, [string]$ParentPath) {
	$fullCandidatePath = [System.IO.Path]::GetFullPath($CandidatePath)
	$fullParentPath = [System.IO.Path]::GetFullPath($ParentPath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
	$result = $fullCandidatePath.Equals($fullParentPath, [System.StringComparison]::OrdinalIgnoreCase) -or
		$fullCandidatePath.StartsWith($fullParentPath + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)

	return $result
}

function Clear-DirectoryIfSafe([string]$DirectoryPath) {
	if ((Test-IsInsideDirectory $DirectoryPath $artifactsRoot) -and (Test-Path -LiteralPath $DirectoryPath)) {
		Remove-Item -LiteralPath $DirectoryPath -Recurse -Force
	}
}

$writesFlatOutput = $Placement -eq "Flat" -or $Placement -eq "FlatAndSideBySide" -or $Placement -eq "All"
$writesPreservedOutput = $Placement -eq "PreserveStructure" -or $Placement -eq "All"
$writesSideBySideOutput = $Placement -eq "SideBySide" -or $Placement -eq "FlatAndSideBySide" -or $Placement -eq "All"

if ($usingDefaultOutputDirectory) {
	Clear-DirectoryIfSafe $fullOutputDirectory
}

$stagingDirectory = if ($writesFlatOutput) {
	$fullOutputDirectory
}
else {
	Join-Path $artifactsRoot "ExampleGraphImages.Staging"
}

$fullStagingDirectory = [System.IO.Path]::GetFullPath($stagingDirectory)
if (-not $writesFlatOutput) {
	Clear-DirectoryIfSafe $fullStagingDirectory
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
	$stagingDirectory,
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

Write-Host "Exporting example graph images with placement '$Placement'..."
Write-Host "Staging graph images in $stagingDirectory..."
$process = Start-Process -FilePath $executablePath -ArgumentList $arguments -Wait -PassThru
$exampleProjects = Get-ChildItem $examplesRoot -Recurse -File -Filter "*.csproj" | Sort-Object FullName
$missingImages = [System.Collections.Generic.List[string]]::new()
$sideBySideCount = 0
$preservedCount = 0
foreach ($exampleProject in $exampleProjects) {
	$projectName = [System.IO.Path]::GetFileNameWithoutExtension($exampleProject.Name)
	$artifactImage = Join-Path $stagingDirectory "$projectName-Graph.png"
	if (-not (Test-Path -LiteralPath $artifactImage)) {
		$missingImages.Add($artifactImage)

		continue
	}

	if ($writesSideBySideOutput) {
		$exampleImage = Join-Path $exampleProject.DirectoryName "$projectName-Graph.png"
		Copy-Item -LiteralPath $artifactImage -Destination $exampleImage -Force
		$sideBySideCount++
	}

	if ($writesPreservedOutput) {
		$relativeDirectory = [System.IO.Path]::GetRelativePath($examplesRoot, $exampleProject.DirectoryName)
		$preservedDirectory = Join-Path $OutputDirectory $relativeDirectory
		$preservedImage = Join-Path $preservedDirectory "$projectName-Graph.png"
		New-Item -ItemType Directory -Force -Path $preservedDirectory | Out-Null
		Copy-Item -LiteralPath $artifactImage -Destination $preservedImage -Force
		$preservedCount++
	}
}

$flatImageCount = if ($writesFlatOutput) {
	(Get-ChildItem $OutputDirectory -File -Filter "*.png" -ErrorAction SilentlyContinue | Measure-Object).Count
}
else {
	0
}

$projectCount = ($exampleProjects | Measure-Object).Count
Write-Host "Processed $projectCount example project(s)."
if ($writesFlatOutput) {
	Write-Host "Flat output: $flatImageCount image(s) in $OutputDirectory."
}

if ($writesPreservedOutput) {
	Write-Host "Preserved-structure output: $preservedCount image(s) under $OutputDirectory."
}

if ($writesSideBySideOutput) {
	Write-Host "Side-by-side output: $sideBySideCount image(s) copied next to their example projects."
}

if ($missingImages.Count -gt 0) {
	Write-Warning "Missing graph image(s):"
	foreach ($missingImage in $missingImages) {
		Write-Warning "  $missingImage"
	}

	if ($FailOnError) {
		exit 1
	}
}

if (-not $writesFlatOutput -and -not $fullOutputDirectory.Equals($fullStagingDirectory, [System.StringComparison]::OrdinalIgnoreCase)) {
	Clear-DirectoryIfSafe $fullStagingDirectory
}

exit $process.ExitCode
