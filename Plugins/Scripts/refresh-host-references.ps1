param(
    [string]$SourceDir = "",
    [string]$TargetDir = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$targetDir = if ([string]::IsNullOrWhiteSpace($TargetDir)) {
    Join-Path $repoRoot ".host\manual"
} else {
    $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($TargetDir)
}

# Plugins compile against the Lib/Lib.Plugins assembly graph shipped by
# UniversalDeviceToolkit.Host (see Plugins/Directory.Build.props). The retired
# WPF assembly "Universal Device Toolkit.dll" is no longer published anywhere
# and must not be required here. Keep this list in sync with
# ensure-host-dependencies.ps1 (see Plugins/KNOWLEDGE_BASE.md).
$requiredFiles = @(
    "UniversalDeviceToolkit.Lib.dll",
    "UniversalDeviceToolkit.Lib.Plugins.dll",
    "UniversalDeviceToolkit.Lib.Abstractions.dll",
    "UniversalDeviceToolkit.Lib.Shared.dll",
    "UniversalDeviceToolkit.Plugins.Abstractions.dll",
    "Serilog.dll",
    "Serilog.Sinks.Async.dll",
    "Serilog.Sinks.File.dll"
)

if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    throw "Please provide -SourceDir from the main repository Build output."
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
