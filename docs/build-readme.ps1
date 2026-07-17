param(
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'

$docsRoot = $PSScriptRoot
$repoRoot = Resolve-Path -LiteralPath (Join-Path $docsRoot '..')
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot 'README.md'
}

$manifestPath = Join-Path $docsRoot '_readme-order.txt'
if (!(Test-Path -LiteralPath $manifestPath)) {
    throw "Missing manifest: $manifestPath"
}

function Convert-DocLinksForReadme([string] $content) {
    $result = $content
    foreach ($rootPath in @('Examples', 'src', 'docs', 'build', '.github')) {
        $result = [regex]::Replace($result, "\]\((?:\.\./)+$([regex]::Escape($rootPath))/", "]($rootPath/")
        $result = [regex]::Replace($result, "src=`"(?:\.\./)+$([regex]::Escape($rootPath))/", "src=`"$rootPath/")
    }

    return $result
}

$parts = [System.Collections.Generic.List[string]]::new()
$parts.Add('<!-- This README is generated from docs/*.md. Edit docs and run docs/build-readme.ps1. -->')

foreach ($entry in Get-Content -LiteralPath $manifestPath -Encoding UTF8) {
    $line = $entry.Trim()
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#', [StringComparison]::Ordinal)) {
        continue
    }

    $path = Join-Path $docsRoot $line
    if (!(Test-Path -LiteralPath $path)) {
        throw "Manifest entry does not exist: $line"
    }

    $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    $parts.Add((Convert-DocLinksForReadme $content).Trim())
}

$readme = ($parts -join "`r`n`r`n") + "`r`n"
Set-Content -LiteralPath $OutputPath -Value $readme -Encoding UTF8
Write-Host "Generated $OutputPath from $manifestPath"

