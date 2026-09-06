Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptDirectory "..\..\..")
$readmePath = Join-Path $repositoryRoot "README.md"
$wikiOutputPath = Join-Path $repositoryRoot "build\Artifacts\WikiCheck"

$readmeExisted = Test-Path -LiteralPath $readmePath
$readmeBeforeGeneration = if ($readmeExisted) { Get-Content -LiteralPath $readmePath -Raw -Encoding UTF8 } else { $null }
& (Join-Path $repositoryRoot "docs\build-readme.ps1")
$readmeAfterGeneration = Get-Content -LiteralPath $readmePath -Raw -Encoding UTF8
$normalizedReadmeBeforeGeneration = if ($null -eq $readmeBeforeGeneration) { $null } else { $readmeBeforeGeneration.Replace("`r`n", "`n").Replace("`r", "`n") }
$normalizedReadmeAfterGeneration = $readmeAfterGeneration.Replace("`r`n", "`n").Replace("`r", "`n")

if (!$readmeExisted -or $normalizedReadmeBeforeGeneration -cne $normalizedReadmeAfterGeneration) {
    throw "Generated README.md is stale. Run docs\build-readme.ps1 and commit the result."
}

& (Join-Path $scriptDirectory "export-docs-to-wiki.ps1") -OutputPath $wikiOutputPath

if (!(Test-Path -LiteralPath (Join-Path $wikiOutputPath "Home.md"))) {
    throw "Wiki export did not create Home.md."
}

if (!(Test-Path -LiteralPath (Join-Path $wikiOutputPath "_Sidebar.md"))) {
    throw "Wiki export did not create _Sidebar.md."
}

Write-Host "Generated docs are current."
