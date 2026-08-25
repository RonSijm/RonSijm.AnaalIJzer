param(
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'

$docsRoot = $PSScriptRoot
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $docsRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot 'README.md'
}

$manifestPath = Join-Path $docsRoot '_readme-order.txt'
if (!(Test-Path -LiteralPath $manifestPath)) {
    throw "Missing manifest: $manifestPath"
}

function Convert-ToLf([string] $content) {
    $result = $content.Replace("`r`n", "`n").Replace("`r", "`n")

    return $result
}

function Get-NormalizedRepositoryRelativePath([string] $path) {
    $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $path)
    $result = $relativePath.Replace('\', '/')

    return $result
}

function Resolve-ReadmeLinkTarget([string] $sourcePath, [string] $target) {
    if ([string]::IsNullOrWhiteSpace($target) -or
        $target.StartsWith('#') -or
        $target.StartsWith('http://', [System.StringComparison]::OrdinalIgnoreCase) -or
        $target.StartsWith('https://', [System.StringComparison]::OrdinalIgnoreCase) -or
        $target.StartsWith('mailto:', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $target
    }

    $anchor = ''
    $pathPart = $target
    $hashIndex = $target.IndexOf('#', [System.StringComparison]::Ordinal)
    if ($hashIndex -ge 0) {
        $pathPart = $target.Substring(0, $hashIndex)
        $anchor = $target.Substring($hashIndex)
    }

    if ([string]::IsNullOrWhiteSpace($pathPart)) {
        return $target
    }

    $resolvedPath = if ([System.IO.Path]::IsPathRooted($pathPart)) {
        Join-Path $repoRoot $pathPart.TrimStart('/', '\')
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $sourcePath) $pathPart))
    }

    $repositoryRootPath = [System.IO.Path]::GetFullPath($repoRoot)
    $candidateRootPath = [System.IO.Path]::GetFullPath($resolvedPath)
    $isInsideRepository = $candidateRootPath.Equals($repositoryRootPath, [System.StringComparison]::OrdinalIgnoreCase) -or
        $candidateRootPath.StartsWith($repositoryRootPath + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -or
        $candidateRootPath.StartsWith($repositoryRootPath + [System.IO.Path]::AltDirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)

    if (-not $isInsideRepository) {
        return $target
    }

    $result = (Get-NormalizedRepositoryRelativePath $resolvedPath) + $anchor

    return $result
}

function Convert-DocLinksForReadme([string] $content, [string] $sourcePath) {
    $result = [regex]::Replace(
        $content,
        '(?<prefix>!?\[[^\]]*\]\()(?<target>[^)\s]+)(?<suffix>\))',
        {
            param($match)

            $convertedTarget = Resolve-ReadmeLinkTarget $sourcePath $match.Groups['target'].Value
            $replacement = $match.Groups['prefix'].Value + $convertedTarget + $match.Groups['suffix'].Value

            return $replacement
        })

    $result = [regex]::Replace(
        $result,
        '(?<prefix>\b(?:src|href)=\")(?<target>[^"]+)(?<suffix>\")',
        {
            param($match)

            $convertedTarget = Resolve-ReadmeLinkTarget $sourcePath $match.Groups['target'].Value
            $replacement = $match.Groups['prefix'].Value + $convertedTarget + $match.Groups['suffix'].Value

            return $replacement
        })

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
    $normalizedContent = Convert-ToLf $content
    $parts.Add((Convert-DocLinksForReadme $normalizedContent $path).Trim())
}

$readme = ($parts -join "`n`n") + "`n"
$encoding = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($OutputPath, $readme, $encoding)
Write-Host "Generated $OutputPath from $manifestPath"

