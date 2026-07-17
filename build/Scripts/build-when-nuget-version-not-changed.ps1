param(
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$SkipPack
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptDirectory "..\..")
$versionFile = "build/Settings/NuGetVersioning.props"

Push-Location $repositoryRoot
try {
    $statusChange = git status --short -- $versionFile
    $previousCommitExists = $false
    git rev-parse --verify HEAD~1 *> $null
    if ($LASTEXITCODE -eq 0) {
        $previousCommitExists = $true
    }

    $commitChange = @()
    if ($previousCommitExists) {
        $commitChange = @(git diff --name-only HEAD~1 HEAD -- $versionFile)
    }

    $versionChanged = -not [string]::IsNullOrWhiteSpace($statusChange) -or $commitChange.Count -gt 0
    if ($versionChanged) {
        throw "NuGet version file changed. Use build-when-nuget-version-changed.ps1 so packages can be published deliberately."
    }

    Write-Host "NuGet version file did not change; running build without publishing."
    & (Join-Path $scriptDirectory "build-base.ps1") -Configuration $Configuration -SkipTests:$SkipTests -SkipPack:$SkipPack
}
finally {
    Pop-Location
}
