Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $scriptDirectory "check-generated-docs.ps1")
& (Join-Path $scriptDirectory "check-anl-schema.ps1")
& (Join-Path $scriptDirectory "check-markdown-links.ps1")

Write-Host "Documentation checks completed."
