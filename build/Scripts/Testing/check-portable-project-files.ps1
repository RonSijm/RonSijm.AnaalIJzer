[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$failures = [System.Collections.Generic.List[string]]::new()
$projectFiles = & git -C $repositoryRoot ls-files --cached --others --exclude-standard -- "*.csproj" "*.props" "*.targets"
if ($LASTEXITCODE -ne 0) {
	throw "Could not enumerate project files with git."
}

foreach ($relativePath in $projectFiles) {
	$projectFilePath = Join-Path $repositoryRoot $relativePath
	if (-not (Test-Path -LiteralPath $projectFilePath -PathType Leaf)) {
		continue
	}

	[xml]$document = Get-Content -LiteralPath $projectFilePath -Raw
	$sourceElements = $document.SelectNodes("//*[local-name()='RestoreAdditionalProjectSources']")
	foreach ($sourceElement in $sourceElements) {
		foreach ($source in ([string]$sourceElement.InnerText).Split(';', [System.StringSplitOptions]::RemoveEmptyEntries)) {
			$value = $source.Trim()
			$isAbsoluteLocalPath = $value -match "^[A-Za-z]:[\\/]" -or $value.StartsWith("\\\\", [System.StringComparison]::Ordinal) -or $value.StartsWith("/", [System.StringComparison]::Ordinal)
			if ($isAbsoluteLocalPath) {
				$normalizedPath = $relativePath.Replace('\', '/')
				$failures.Add("${normalizedPath}: RestoreAdditionalProjectSources contains machine-specific path '${value}'.")
			}
		}
	}
}

if ($failures.Count -gt 0) {
	$failures | ForEach-Object { Write-Error $_ }
	throw "Project files must not contain absolute local NuGet sources. Use a published feed or a repository-relative source configured by the build."
}

Write-Host "Project-file portability check passed."
