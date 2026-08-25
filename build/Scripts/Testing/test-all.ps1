[CmdletBinding()]
param(
	[string]$Configuration = "Debug",
	[switch]$NoBuild,
	[switch]$NoRestore,
	[switch]$CollectCoverage,
	[string]$CoverageRunSettings = "src/Tests/RonSijm.AnaalIJzer.Analyzer.Tests/coverlet.runsettings",
	[string]$ResultsDirectory = "./coverage",
	[switch]$SkipWindowsOnly
)

$ErrorActionPreference = "Stop"
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptDirectory "..\..\..")).Path
$runningOnWindows = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT

function Get-RelativePath {
	param(
		[string]$BasePath,
		[string]$TargetPath
	)

	$baseUri = [System.Uri]::new(([System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
	$targetUri = [System.Uri]::new([System.IO.Path]::GetFullPath($TargetPath))
	$result = $baseUri.MakeRelativeUri($targetUri).ToString()
	$result = [System.Uri]::UnescapeDataString($result).Replace('/', [System.IO.Path]::DirectorySeparatorChar)

	return $result
}

function Test-IsWindowsOnlyProject {
	param(
		[string]$ProjectPath
	)

	[xml]$project = Get-Content -LiteralPath $ProjectPath
	$propertyGroups = @($project.Project.PropertyGroup)

	$targetFrameworks = @(
		$propertyGroups.TargetFramework,
		$propertyGroups.TargetFrameworks
	) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

	$frameworkTokens = $targetFrameworks |
		ForEach-Object { $_ -split ';' } |
		ForEach-Object { $_.Trim() } |
		Where-Object { $_ -ne "" }

	$usesWpf = @($propertyGroups.UseWPF) -contains "true"
	$hasWindowsFramework = $frameworkTokens | Where-Object { $_ -like "net4*" -or $_ -like "*-windows*" }

	$result = $usesWpf -or $hasWindowsFramework.Count -gt 0

	return $result
}

function Get-TestProjects {
	param(
		[string]$RepositoryRoot
	)

	$projectFiles = Get-ChildItem (Join-Path $RepositoryRoot "src/Tests") -Recurse -Filter *.csproj |
		Sort-Object FullName

	$result = foreach ($projectFile in $projectFiles) {
		$projectPath = $projectFile.FullName
		$relativePath = Get-RelativePath -BasePath $RepositoryRoot -TargetPath $projectPath
		$isWindowsOnly = Test-IsWindowsOnlyProject -ProjectPath $projectPath

		[pscustomobject]@{
			Path = $relativePath
			WindowsOnly = $isWindowsOnly
		}
	}

	return $result
}

function Get-AssetsFilePath {
	param(
		[string]$ProjectPath
	)

	$projectDirectory = Split-Path -Parent $ProjectPath
	$result = Join-Path $projectDirectory "obj/project.assets.json"

	return $result
}

$projects = Get-TestProjects -RepositoryRoot $repositoryRoot
$coverageRunSettingsPath = Join-Path $repositoryRoot $CoverageRunSettings
$resolvedResultsDirectory = Join-Path $repositoryRoot $ResultsDirectory

if ($CollectCoverage -and -not (Test-Path -LiteralPath $coverageRunSettingsPath)) {
	throw "Coverage runsettings file was not found: $coverageRunSettingsPath"
}

if ($CollectCoverage -and -not (Test-Path -LiteralPath $resolvedResultsDirectory)) {
	New-Item -ItemType Directory -Force -Path $resolvedResultsDirectory | Out-Null
}

Write-Host "Discovered $($projects.Count) test project(s)."

foreach ($project in $projects) {
	if ($project.WindowsOnly -and ($SkipWindowsOnly -or -not $runningOnWindows)) {
		Write-Host "Skipping Windows-only tests: $($project.Path)"
		continue
	}

	$projectPath = Join-Path $repositoryRoot $project.Path
	$assetsFilePath = Get-AssetsFilePath -ProjectPath $projectPath

	if ($NoRestore -and -not (Test-Path -LiteralPath $assetsFilePath)) {
		Write-Host ""
		Write-Host "Missing restore assets for $($project.Path). Running a targeted restore so --no-restore tests can continue."

		$restoreArguments = @("restore", $projectPath, "--disable-build-servers", "-m:1")
		Push-Location $repositoryRoot
		try {
			& dotnet @restoreArguments
			if ($LASTEXITCODE -ne 0) {
				exit $LASTEXITCODE
			}
		}
		finally {
			Pop-Location
		}
	}

	$arguments = @("test", $projectPath, "--configuration", $Configuration, "--disable-build-servers", "-m:1", "-p:UseSharedCompilation=false")
	if ($NoBuild) {
		$arguments += "--no-build"
	}
	if ($NoRestore) {
		$arguments += "--no-restore"
	}
	if ($CollectCoverage) {
		$arguments += @(
			'--collect:XPlat Code Coverage',
			'--settings', $coverageRunSettingsPath,
			'--results-directory', $resolvedResultsDirectory
		)
	}

	Write-Host ""
	Write-Host "==> dotnet $($arguments -join ' ')"
	Push-Location $repositoryRoot
	try {
		& dotnet @arguments
		if ($LASTEXITCODE -ne 0) {
			exit $LASTEXITCODE
		}
	}
	finally {
		Pop-Location
	}
}

Write-Host ""
Write-Host "All selected test projects passed."
