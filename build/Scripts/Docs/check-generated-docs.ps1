Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptDirectory "..\..\..")
$readmePath = Join-Path $repositoryRoot "README.md"
$wikiOutputPath = Join-Path $repositoryRoot "build\Artifacts\WikiCheck"

& (Join-Path $repositoryRoot "docs\build-readme.ps1")
& (Join-Path $scriptDirectory "export-docs-to-wiki.ps1") -OutputPath $wikiOutputPath

$status = git -C $repositoryRoot status --porcelain -- README.md
if (-not [string]::IsNullOrWhiteSpace($status)) {
    git -C $repositoryRoot diff --ignore-cr-at-eol --exit-code -- $readmePath
    if ($LASTEXITCODE -ne 0) {
        throw "Generated README.md is stale. Run docs\build-readme.ps1 and commit the result."
    }
}

if (!(Test-Path -LiteralPath (Join-Path $wikiOutputPath "Home.md"))) {
    throw "Wiki export did not create Home.md."
}

if (!(Test-Path -LiteralPath (Join-Path $wikiOutputPath "_Sidebar.md"))) {
    throw "Wiki export did not create _Sidebar.md."
}

Write-Host "Generated docs are current."
