[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = (Resolve-Path (Join-Path $scriptDirectory "..\..\..")).Path
$legacyAllowedPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@(
	"src/Main/RonSijm.AnaalIJzer.Core.Findings/Diagnostics/ArchitectureDiagnosticIdMigrationCatalog.cs",
	"src/Main/RonSijm.AnaalIJzer.Engine/AnalyzerReleases.Shipped.md",
	"src/Main/RonSijm.AnaalIJzer.Engine/AnalyzerReleases.Unshipped.md",
	"src/Tests/RonSijm.AnaalIJzer.Application.Tests/ApplicationOperations/ApplicationOperationsTests.DiagnosticIdMigration.cs"
) | ForEach-Object { [void]$legacyAllowedPaths.Add($_) }

$textExtensions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@(".anl", ".cmd", ".cs", ".csproj", ".editorconfig", ".json", ".md", ".props", ".ps1", ".razor", ".slnx", ".targets", ".txt", ".xaml", ".xml", ".yaml", ".yml") |
	ForEach-Object { [void]$textExtensions.Add($_) }

$legacyPattern = [regex]'\b(?:ARCH|TOPO)[0-9]{3}\b'
$hardCodedNewIdPattern = [regex]'"ARCH_[A-Z][A-Z0-9]*_[0-9]{3}"'
$publishedIdSource = "src/Main/RonSijm.AnaalIJzer.Core.Findings/ArchitecturalDiagnosticIds.cs"
$failures = [System.Collections.Generic.List[string]]::new()
$trackedFiles = & git -C $repositoryRoot ls-files --cached --others --exclude-standard
if ($LASTEXITCODE -ne 0) {
	throw "Could not enumerate tracked files with git."
}

foreach ($relativePath in $trackedFiles) {
	$normalizedPath = $relativePath.Replace('\', '/')
	if (-not $textExtensions.Contains([System.IO.Path]::GetExtension($normalizedPath))) {
		continue
	}

	$fullPath = Join-Path $repositoryRoot $relativePath
	if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
		continue
	}

	$content = [System.IO.File]::ReadAllText($fullPath)
	if (-not $legacyAllowedPaths.Contains($normalizedPath)) {
		foreach ($match in $legacyPattern.Matches($content)) {
			$failures.Add("$normalizedPath contains legacy diagnostic ID '$($match.Value)'.")
		}
	}

	$isProductionSource = $normalizedPath.EndsWith(".cs", [System.StringComparison]::OrdinalIgnoreCase) -and
		($normalizedPath.StartsWith("src/Main/", [System.StringComparison]::OrdinalIgnoreCase) -or
		 $normalizedPath.StartsWith("src/Tools/", [System.StringComparison]::OrdinalIgnoreCase) -or
		 $normalizedPath.StartsWith("src/Extensions/", [System.StringComparison]::OrdinalIgnoreCase))
	if ($isProductionSource -and $normalizedPath -ne $publishedIdSource) {
		foreach ($match in $hardCodedNewIdPattern.Matches($content)) {
			$failures.Add("$normalizedPath hard-codes published diagnostic ID $($match.Value); use ArchitecturalDiagnosticIds or the catalog.")
		}
	}
}

if ($failures.Count -gt 0) {
	$failures | Sort-Object -Unique | ForEach-Object { Write-Error $_ }
	throw "$($failures.Count) diagnostic ID consistency failure(s)."
}

Write-Host "Diagnostic ID consistency check passed."
