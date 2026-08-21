param(
    [string]$SourceDir = "",
    [switch]$ForceRefresh
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$manifestPath = Join-Path $repoRoot "HostBaseline\host-release.json"
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Host baseline manifest was not found in HostBaseline."
}
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$hostVersion = [string]$manifest.hostVersion
if ([string]::IsNullOrWhiteSpace($hostVersion)) {
    throw "hostVersion is missing from host-release.json."
}

$targetDir = Join-Path $repoRoot (Join-Path ".host" $hostVersion)
$refreshScript = Join-Path $PSScriptRoot "refresh-host-references.ps1"

# Keep this list in sync with refresh-host-references.ps1 (see
# Plugins/KNOWLEDGE_BASE.md). The retired WPF assembly
# "Universal Device Toolkit.dll" is not part of the host reference set:
# plugins compile against Lib/Lib.Plugins, shipped by UniversalDeviceToolkit.Host.
$requiredFiles = @(
    "UniversalDeviceToolkit.Lib.dll",
    "UniversalDeviceToolkit.Lib.Plugins.dll",
    "UniversalDeviceToolkit.Lib.Abstractions.dll",
    "UniversalDeviceToolkit.Lib.Shared.dll",
    "UniversalDeviceToolkit.Lib.Automation.dll",
    "UniversalDeviceToolkit.Lib.Macro.dll",
    "Serilog.dll",
    "Serilog.Sinks.Async.dll",
    "Serilog.Sinks.File.dll"
)

function Test-HostDependenciesComplete {
    foreach ($file in $requiredFiles) {
        if (-not (Test-Path (Join-Path $targetDir $file))) {
            return $false
        }
    }

    return $true
}

function Copy-FromSourceDir {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSourceDir
    )

    & $refreshScript -SourceDir $ResolvedSourceDir -TargetDir $targetDir
}

function Copy-FromReleaseZip {
    $downloadUrl = [string]$manifest.downloadUrl
    if ([string]::IsNullOrWhiteSpace($downloadUrl)) {
        $hostVersion = [string]$manifest.hostVersion
        $hostTag = [string]$manifest.hostTag
        $packageName = [string]$manifest.artifacts.package

        if ([string]::IsNullOrWhiteSpace($packageName) -and -not [string]::IsNullOrWhiteSpace($hostVersion)) {
            $packageName = "UniversalDeviceToolkit_v$hostVersion`_win-x64.zip"
        }

        if (-not [string]::IsNullOrWhiteSpace($hostTag) -and -not [string]::IsNullOrWhiteSpace($packageName)) {
            $downloadUrl = "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/$hostTag/$packageName"
        }
    }

    if ([string]::IsNullOrWhiteSpace($downloadUrl)) {
        throw "downloadUrl is missing from host-release.json and could not be derived from hostTag/hostVersion."
    }

    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("llt-host-" + [guid]::NewGuid().ToString("N"))
    $zipPath = Join-Path $tempRoot "host.zip"
    $extractDir = Join-Path $tempRoot "extract"

    New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $extractDir -Force | Out-Null
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

    try {
        Write-Host "Downloading host dependencies from $downloadUrl"
        Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath
        Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

        foreach ($file in $requiredFiles) {
            $candidate = Get-ChildItem -Path $extractDir -Filter $file -File -Recurse | Select-Object -First 1
            if ($null -eq $candidate) {
                throw "Required host file '$file' was not found in downloaded archive."
            }

            Copy-Item -Path $candidate.FullName -Destination (Join-Path $targetDir $file) -Force
            Write-Host "Updated $file from release archive"
        }

        $abstractionsArchiveCandidate = Get-ChildItem -Path $extractDir -Filter "UniversalDeviceToolkit.Plugins.Abstractions.dll" -File -Recurse | Select-Object -First 1
        if ($null -ne $abstractionsArchiveCandidate) {
            Copy-Item -Path $abstractionsArchiveCandidate.FullName -Destination (Join-Path $targetDir "UniversalDeviceToolkit.Plugins.Abstractions.dll") -Force
            Write-Host "Updated UniversalDeviceToolkit.Plugins.Abstractions.dll from release archive"
        } else {
            Write-Warning "UniversalDeviceToolkit.Plugins.Abstractions.dll was not found in downloaded archive. Skipping."
        }
    }
    finally {
        if (Test-Path $tempRoot) {
            Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

if ((Test-HostDependenciesComplete) -and -not $ForceRefresh) {
    Write-Host "Host dependencies already available in $targetDir"
    exit 0
}

if (-not [string]::IsNullOrWhiteSpace($SourceDir)) {
    Copy-FromSourceDir -ResolvedSourceDir $SourceDir
    exit 0
}

Copy-FromReleaseZip
