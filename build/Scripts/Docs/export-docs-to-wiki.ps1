param(
    [string]$OutputPath,
    [string]$Repository = $env:GITHUB_REPOSITORY,
    [string]$RefName = $(if ($env:GITHUB_REF_NAME) { $env:GITHUB_REF_NAME } else { "main" })
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptDirectory "..\..\..")
$docsRoot = Resolve-Path (Join-Path $repositoryRoot "docs")
$manifestPath = Join-Path $docsRoot "_readme-order.txt"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot "build\Artifacts\Wiki"
}

if ([string]::IsNullOrWhiteSpace($Repository)) {
    $Repository = "RonSijm/RonSijm.AnaalIJzer"
}

function Get-NormalizedRelativePath {
    param(
        [string]$Root,
        [string]$Path
    )

    $relativePath = [System.IO.Path]::GetRelativePath($Root, $Path)
    $result = $relativePath.Replace("\", "/")

    return $result
}

function Test-IsPathInside {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootPath = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $candidatePath = [System.IO.Path]::GetFullPath($Path).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $result = $candidatePath.Equals($rootPath, [System.StringComparison]::OrdinalIgnoreCase) -or
        $candidatePath.StartsWith($rootPath + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -or
        $candidatePath.StartsWith($rootPath + [System.IO.Path]::AltDirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)

    return $result
}

function ConvertTo-WikiFileName {
    param(
        [string]$DocsRelativePath
    )

    if ($DocsRelativePath.Equals("README.md", [System.StringComparison]::OrdinalIgnoreCase)) {
        $result = "Home.md"
        return $result
    }

    $withoutExtension = $DocsRelativePath -replace "\.md$", ""
    $normalized = $withoutExtension -replace "[\\/]+", "-"
    $normalized = $normalized -replace "[^A-Za-z0-9._-]", "-"
    $result = "$normalized.md"

    return $result
}

function Get-MarkdownTitle {
    param(
        [string]$Content,
        [string]$Fallback
    )

    foreach ($line in $Content -split "\r?\n") {
        if ($line -match "^\s*#\s+(.+?)\s*$") {
            $title = $Matches[1]
            $title = $title -replace "\*", ""
            $title = $title -replace "`"", ""
            $title = $title -replace "`'", ""
            $result = $title.Trim()

            return $result
        }
    }

    $result = [System.IO.Path]::GetFileNameWithoutExtension($Fallback)

    return $result
}

function Resolve-DocumentationTarget {
    param(
        [string]$SourceDocsRelativePath,
        [string]$TargetPath
    )

    if ([string]::IsNullOrWhiteSpace($TargetPath)) {
        return $null
    }

    if ([System.IO.Path]::IsPathRooted($TargetPath)) {
        $trimmedTarget = $TargetPath.TrimStart("/", "\")
        $result = Join-Path $repositoryRoot $trimmedTarget

        return $result
    }

    $sourceDirectory = Split-Path -Parent (Join-Path $docsRoot $SourceDocsRelativePath)
    if ([string]::IsNullOrWhiteSpace($sourceDirectory)) {
        $sourceDirectory = $docsRoot
    }

    $result = [System.IO.Path]::GetFullPath((Join-Path $sourceDirectory $TargetPath))

    return $result
}

function ConvertTo-WikiLink {
    param(
        [string]$SourceDocsRelativePath,
        [string]$Target,
        [System.Collections.Generic.Dictionary[string, string]]$PageNames
    )

    $trimmedTarget = $Target.Trim()
    if ($trimmedTarget.StartsWith("http://", [System.StringComparison]::OrdinalIgnoreCase) -or
        $trimmedTarget.StartsWith("https://", [System.StringComparison]::OrdinalIgnoreCase) -or
        $trimmedTarget.StartsWith("mailto:", [System.StringComparison]::OrdinalIgnoreCase) -or
        $trimmedTarget.StartsWith("#", [System.StringComparison]::Ordinal)) {
        return $Target
    }

    $anchor = ""
    $targetPath = $trimmedTarget
    $hashIndex = $trimmedTarget.IndexOf("#", [System.StringComparison]::Ordinal)
    if ($hashIndex -ge 0) {
        $targetPath = $trimmedTarget.Substring(0, $hashIndex)
        $anchor = $trimmedTarget.Substring($hashIndex)
    }

    if ([string]::IsNullOrWhiteSpace($targetPath)) {
        return $Target
    }

    $resolvedTarget = Resolve-DocumentationTarget $SourceDocsRelativePath $targetPath
    if ($null -eq $resolvedTarget) {
        return $Target
    }

    if ((Test-Path -LiteralPath $resolvedTarget -PathType Container)) {
        $indexPath = Join-Path $resolvedTarget "index.md"
        $readmePath = Join-Path $resolvedTarget "README.md"
        if (Test-Path -LiteralPath $indexPath -PathType Leaf) {
            $resolvedTarget = $indexPath
        } elseif (Test-Path -LiteralPath $readmePath -PathType Leaf) {
            $resolvedTarget = $readmePath
        }
    }

    if (Test-IsPathInside $docsRoot $resolvedTarget) {
        $docsRelativeTarget = Get-NormalizedRelativePath $docsRoot $resolvedTarget
        if ($PageNames.ContainsKey($docsRelativeTarget)) {
            $result = [System.IO.Path]::GetFileNameWithoutExtension($PageNames[$docsRelativeTarget]) + $anchor

            return $result
        }
    }

    if (Test-IsPathInside $repositoryRoot $resolvedTarget) {
        $repositoryRelativeTarget = Get-NormalizedRelativePath $repositoryRoot $resolvedTarget
        $kind = "blob"
        if (Test-Path -LiteralPath $resolvedTarget -PathType Container) {
            $kind = "tree"
        }

        $result = "https://github.com/$Repository/$kind/$RefName/$repositoryRelativeTarget$anchor"

        return $result
    }

    return $Target
}

function Convert-MarkdownLinks {
    param(
        [string]$Content,
        [string]$SourceDocsRelativePath,
        [System.Collections.Generic.Dictionary[string, string]]$PageNames
    )

    $result = [regex]::Replace(
        $Content,
        "(?<prefix>!?\[[^\]]*\]\()(?<target>[^)]+)(?<suffix>\))",
        {
            param($match)

            $target = $match.Groups["target"].Value
            $convertedTarget = ConvertTo-WikiLink $SourceDocsRelativePath $target $PageNames
            $replacement = $match.Groups["prefix"].Value + $convertedTarget + $match.Groups["suffix"].Value

            return $replacement
        })

    return $result
}

function Get-ManifestEntries {
    if (!(Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        return @()
    }

    $result = Get-Content -LiteralPath $manifestPath -Encoding UTF8 |
        ForEach-Object { $_.Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith("#", [System.StringComparison]::Ordinal) }

    return $result
}

$manifestEntries = @(Get-ManifestEntries)
$allDocs = Get-ChildItem -LiteralPath $docsRoot -Recurse -File -Filter "*.md" |
    ForEach-Object { Get-NormalizedRelativePath $docsRoot $_.FullName }

$orderedDocs = [System.Collections.Generic.List[string]]::new()
if ($allDocs -contains "README.md") {
    $orderedDocs.Add("README.md")
}

foreach ($entry in $manifestEntries) {
    if ($allDocs -contains $entry -and -not $orderedDocs.Contains($entry)) {
        $orderedDocs.Add($entry)
    }
}

foreach ($doc in ($allDocs | Sort-Object)) {
    if (-not $orderedDocs.Contains($doc)) {
        $orderedDocs.Add($doc)
    }
}

$pageNames = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($doc in $orderedDocs) {
    $pageNames[$doc] = ConvertTo-WikiFileName $doc
}

if (Test-Path -LiteralPath $OutputPath) {
    Remove-Item -LiteralPath $OutputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $OutputPath | Out-Null

$generatedFiles = [System.Collections.Generic.List[string]]::new()
$pageTitles = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$marker = "<!-- Generated from RonSijm.AnaalIJzer repository docs. Do not edit in the wiki; edit docs/ and let the publish-wiki workflow sync it. -->"

foreach ($doc in $orderedDocs) {
    $sourcePath = Join-Path $docsRoot $doc
    $content = Get-Content -LiteralPath $sourcePath -Raw -Encoding UTF8
    $convertedContent = Convert-MarkdownLinks $content.Trim() $doc $pageNames
    $wikiFileName = $pageNames[$doc]
    $wikiPath = Join-Path $OutputPath $wikiFileName
    $wikiContent = "$marker`r`n`r`n$convertedContent`r`n"

    Set-Content -LiteralPath $wikiPath -Value $wikiContent -Encoding UTF8
    $generatedFiles.Add($wikiFileName)
    $pageTitles[$doc] = Get-MarkdownTitle $content $doc
}

$sidebarLines = [System.Collections.Generic.List[string]]::new()
$sidebarLines.Add($marker)
$sidebarLines.Add("")
$sidebarLines.Add("# Documentation")
$sidebarLines.Add("")
$sidebarLines.Add("- [Home](Home)")

foreach ($doc in $orderedDocs) {
    if ($doc.Equals("README.md", [System.StringComparison]::OrdinalIgnoreCase)) {
        continue
    }

    $title = $pageTitles[$doc]
    $pageName = [System.IO.Path]::GetFileNameWithoutExtension($pageNames[$doc])
    $sidebarLines.Add("- [$title]($pageName)")
}

Set-Content -LiteralPath (Join-Path $OutputPath "_Sidebar.md") -Value ($sidebarLines -join "`r`n") -Encoding UTF8
$generatedFiles.Add("_Sidebar.md")

Set-Content -LiteralPath (Join-Path $OutputPath ".anaalijzer-generated-files") -Value ($generatedFiles | Sort-Object) -Encoding UTF8

Write-Host "Exported $($generatedFiles.Count) wiki file(s) to $OutputPath"
