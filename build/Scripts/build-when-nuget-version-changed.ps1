param(
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [string]$NuGetSource = $(if ($env:NUGET_SOURCE) { $env:NUGET_SOURCE } else { "https://api.nuget.org/v3/index.json" }),
    [string]$NuGetApiKey = $env:NUGET_API_KEY
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
        Write-Host "NuGet version file changed; running build and publishing packages."
        & (Join-Path $scriptDirectory "build-base.ps1") -Configuration $Configuration -SkipTests:$SkipTests -PublishNuGet -NuGetSource $NuGetSource -NuGetApiKey $NuGetApiKey
    }
    else {
        Write-Host "NuGet version file did not change; running build without publishing."
        & (Join-Path $scriptDirectory "build-base.ps1") -Configuration $Configuration -SkipTests:$SkipTests
    }
}
finally {
    Pop-Location
}
