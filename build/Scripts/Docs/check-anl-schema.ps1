Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = Resolve-Path (Join-Path $scriptDirectory "..\..\..")
$schemaPath = Join-Path $repositoryRoot "src\Main\RonSijm.AnaalIJzer\Scheme\AnaalIJzer.xsd"

Add-Type -AssemblyName System.Xml

$schemas = [System.Xml.Schema.XmlSchemaSet]::new()
$schemas.Add($null, $schemaPath) | Out-Null

$settings = [System.Xml.XmlReaderSettings]::new()
$settings.Schemas = $schemas
$settings.ValidationType = [System.Xml.ValidationType]::Schema

$failures = [System.Collections.Generic.List[string]]::new()
$settings.add_ValidationEventHandler({
    param($sender, $eventArgs)

    $failures.Add($eventArgs.Message)
})

$files = Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter "*.anl" |
    Where-Object {
        $_.FullName -notmatch "\\(bin|obj|build\\Artifacts)\\"
    }

foreach ($file in $files) {
    $before = $failures.Count
    try {
        $reader = [System.Xml.XmlReader]::Create($file.FullName, $settings)
        try {
            while ($reader.Read()) { }
        } finally {
            $reader.Dispose()
        }
    } catch {
        $failures.Add($_.Exception.Message)
    }

    if ($failures.Count -gt $before) {
        for ($index = $before; $index -lt $failures.Count; $index++) {
            $failures[$index] = "$($file.FullName): $($failures[$index])"
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "$($failures.Count) .anl schema validation failure(s)."
}

Write-Host "Validated $($files.Count) .anl file(s) against AnaalIJzer.xsd."
