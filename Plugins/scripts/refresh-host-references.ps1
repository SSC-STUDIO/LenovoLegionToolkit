param(
    [string]$SourceDir = "",
    [switch]$UseSiblingRepoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$targetDir = Join-Path $repoRoot "Dependencies\\Host"

$requiredFiles = @(
    "LenovoLegionToolkit.Lib.dll",
    "Lenovo Legion Toolkit.dll"
)

if ([string]::IsNullOrWhiteSpace($SourceDir) -and $UseSiblingRepoBuild) {
    $SourceDir = Join-Path $repoRoot "..\\LenovoLegionToolkit\\LenovoLegionToolkit.WPF\\bin\\Release\\net10.0-windows\\win-x64"
}

if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    throw "Please provide -SourceDir, or pass -UseSiblingRepoBuild to use sibling repo Release output."
}

$resolvedSource = (Resolve-Path $SourceDir).Path
if (-not (Test-Path $resolvedSource)) {
    throw "Source directory not found: $SourceDir"
}

New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

foreach ($file in $requiredFiles) {
    $sourceFile = Join-Path $resolvedSource $file
    if (-not (Test-Path $sourceFile)) {
        throw "Missing required file: $sourceFile"
    }

    Copy-Item -Path $sourceFile -Destination (Join-Path $targetDir $file) -Force
    Write-Host "Updated $file"
}

Write-Host "Host references refreshed in $targetDir"
