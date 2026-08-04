param(
    [string]$SourceDir = "",
    [switch]$UseSiblingRepoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$targetDir = Join-Path $repoRoot "Dependencies\\Host"

$requiredFiles = @(
    "UniversalDeviceToolkit.Lib.dll",
    "UniversalDeviceToolkit.Lib.Plugins.dll",
    "UniversalDeviceToolkit.Plugins.Abstractions.dll",
    "Universal Device Toolkit.dll",
    "Serilog.dll",
    "Serilog.Sinks.Async.dll",
    "Serilog.Sinks.File.dll"
)

if ([string]::IsNullOrWhiteSpace($SourceDir) -and $UseSiblingRepoBuild) {
    $SourceDir = Join-Path $repoRoot "..\\UniversalDeviceToolkit\\Build"
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
        if ($file -eq "UniversalDeviceToolkit.Plugins.Abstractions.dll") {
            Write-Warning "Optional host file '$file' not found in source. Skipping."
            continue
        }
        throw "Missing required file: $sourceFile"
    }

    Copy-Item -Path $sourceFile -Destination (Join-Path $targetDir $file) -Force
    Write-Host "Updated $file"
}

Write-Host "Host references refreshed in $targetDir"
