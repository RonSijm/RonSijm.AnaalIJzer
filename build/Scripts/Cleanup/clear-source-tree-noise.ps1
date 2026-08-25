param()

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDirectory "..\..\.."))

$roots = @(
    [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot "src")),
    [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot "Examples"))
)

$generatedDirectoryNames = @("bin", "obj", "build", "publish")
$generatedFilePatterns = @("*.usersettings", "*.csproj.user")

function Assert-PathWithinRoot {
    param(
        [string]$RootPath,
        [string]$TargetPath
    )

    $normalizedRoot = [System.IO.Path]::TrimEndingDirectorySeparator([System.IO.Path]::GetFullPath($RootPath))
    $normalizedTarget = [System.IO.Path]::TrimEndingDirectorySeparator([System.IO.Path]::GetFullPath($TargetPath))

    $result = $normalizedTarget.StartsWith($normalizedRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -or
              $normalizedTarget.Equals($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)

    return $result
}

function Remove-GeneratedDirectory {
    param(
        [string]$RootPath,
        [string]$DirectoryPath
    )

    if (-not (Assert-PathWithinRoot -RootPath $RootPath -TargetPath $DirectoryPath)) {
        throw "Refusing to remove directory outside cleanup root: $DirectoryPath"
    }

    if (-not (Test-Path -LiteralPath $DirectoryPath -PathType Container)) {
        return
    }

    Remove-Item -LiteralPath $DirectoryPath -Recurse -Force
    Write-Host "Removed directory: $DirectoryPath"
}

function Remove-GeneratedFile {
    param(
        [string]$RootPath,
        [string]$FilePath
    )

    if (-not (Assert-PathWithinRoot -RootPath $RootPath -TargetPath $FilePath)) {
        throw "Refusing to remove file outside cleanup root: $FilePath"
    }

    if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
        return
    }

    Remove-Item -LiteralPath $FilePath -Force
    Write-Host "Removed file: $FilePath"
}

function Remove-EmptyDirectories {
    param([string]$RootPath)

    $directories = Get-ChildItem -LiteralPath $RootPath -Directory -Recurse -Force |
        Sort-Object { $_.FullName.Length } -Descending

    foreach ($directory in $directories) {
        if (@($directory.GetFileSystemInfos()).Count -gt 0) {
            continue
        }

        if (-not (Assert-PathWithinRoot -RootPath $RootPath -TargetPath $directory.FullName)) {
            throw "Refusing to remove empty directory outside cleanup root: $($directory.FullName)"
        }

        Remove-Item -LiteralPath $directory.FullName -Force
        Write-Host "Removed empty directory: $($directory.FullName)"
    }
}

foreach ($root in $roots) {
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        continue
    }

    foreach ($directoryName in $generatedDirectoryNames) {
        $directories = Get-ChildItem -LiteralPath $root -Directory -Recurse -Force |
            Where-Object { $_.Name.Equals($directoryName, [System.StringComparison]::OrdinalIgnoreCase) }

        foreach ($directory in $directories) {
            Remove-GeneratedDirectory -RootPath $root -DirectoryPath $directory.FullName
        }
    }

    foreach ($pattern in $generatedFilePatterns) {
        $files = Get-ChildItem -LiteralPath $root -File -Recurse -Force -Filter $pattern

        foreach ($file in $files) {
            Remove-GeneratedFile -RootPath $root -FilePath $file.FullName
        }
    }

    Remove-EmptyDirectories -RootPath $root
}
