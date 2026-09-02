Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptDirectory "..\..\..")
$files = @(
    Join-Path $repositoryRoot "README.md"
) + @(Get-ChildItem -LiteralPath (Join-Path $repositoryRoot "docs") -Recurse -File -Filter "*.md" | Select-Object -ExpandProperty FullName)

$failures = [System.Collections.Generic.List[string]]::new()
$linkPattern = [regex]'!?\[[^\]]*\]\((?<target>[^)\s]+)(?:\s+"[^"]*")?\)'

function Test-PathWithExactCasing([string]$Path) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPath = [System.IO.Path]::GetPathRoot($fullPath)
    if ([string]::IsNullOrWhiteSpace($rootPath)) {
        return $false
    }

    $relativePath = $fullPath.Substring($rootPath.Length)
    $segments = $relativePath.Split([char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar), [System.StringSplitOptions]::RemoveEmptyEntries)
    $currentPath = $rootPath

    foreach ($segment in $segments) {
        $matchingEntry = Get-ChildItem -LiteralPath $currentPath -Force | Where-Object { $_.Name -ceq $segment } | Select-Object -First 1
        if ($null -eq $matchingEntry) {
            return $false
        }

        $currentPath = $matchingEntry.FullName
    }

    return $true
}

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file -Raw -Encoding UTF8
    foreach ($match in $linkPattern.Matches($content)) {
        $target = $match.Groups["target"].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($target)) {
            continue
        }

        if ($target.StartsWith("#") -or
            $target.StartsWith("http://", [System.StringComparison]::OrdinalIgnoreCase) -or
            $target.StartsWith("https://", [System.StringComparison]::OrdinalIgnoreCase) -or
            $target.StartsWith("mailto:", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $pathPart = $target.Split("#")[0]
        if ([string]::IsNullOrWhiteSpace($pathPart)) {
            continue
        }

        $pathPart = [System.Uri]::UnescapeDataString($pathPart)
        $baseDirectory = Split-Path -Parent $file
        $candidate = if ([System.IO.Path]::IsPathRooted($pathPart)) {
            Join-Path $repositoryRoot $pathPart.TrimStart("/", "\")
        } else {
            [System.IO.Path]::GetFullPath((Join-Path $baseDirectory $pathPart))
        }

        if (!(Test-Path -LiteralPath $candidate) -or !(Test-PathWithExactCasing $candidate)) {
            $relativeFile = [System.IO.Path]::GetRelativePath($repositoryRoot, $file)
            $failures.Add("${relativeFile}: missing or incorrectly cased markdown link target '$target'")
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "$($failures.Count) broken markdown link(s)."
}

Write-Host "Checked markdown links in $($files.Count) file(s)."
