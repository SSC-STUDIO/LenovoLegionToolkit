param(
    [string]$SourceDir = "",
    [switch]$UseSiblingRepoBuild,
    [switch]$ForceRefresh
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$targetDir = Join-Path $repoRoot "Dependencies\Host"
$manifestPath = Join-Path $targetDir "host-release.json"
$refreshScript = Join-Path $PSScriptRoot "refresh-host-references.ps1"

$requiredFiles = @(
    "UniversalDeviceToolkit.Lib.dll",
    "UniversalDeviceToolkit.Lib.Plugins.dll",
    "Universal Device Toolkit.dll",
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

function Resolve-SiblingSourceDir {
    $candidates = @(
        (Join-Path $repoRoot "..\UniversalDeviceToolkit\Build")
    )

    foreach ($candidate in $candidates) {
        $libCandidate = Join-Path $candidate "UniversalDeviceToolkit.Lib.dll"
        $libPluginsCandidate = Join-Path $candidate "UniversalDeviceToolkit.Lib.Plugins.dll"
        $wpfCandidate = Join-Path $candidate "Universal Device Toolkit.dll"
        $serilogCandidate = Join-Path $candidate "Serilog.dll"
        $serilogAsyncCandidate = Join-Path $candidate "Serilog.Sinks.Async.dll"
        $serilogFileCandidate = Join-Path $candidate "Serilog.Sinks.File.dll"
        $abstractionsCandidate = Join-Path $candidate "UniversalDeviceToolkit.Plugins.Abstractions.dll"
        if ((Test-Path $libCandidate) -and (Test-Path $libPluginsCandidate) -and (Test-Path $wpfCandidate) -and (Test-Path $serilogCandidate) -and (Test-Path $serilogAsyncCandidate) -and (Test-Path $serilogFileCandidate) -and (Test-Path $abstractionsCandidate)) {
            return $candidate
        }
    }

    return $null
}

function Copy-FromSourceDir {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSourceDir
    )

    & $refreshScript -SourceDir $ResolvedSourceDir
}

function Copy-FromReleaseZip {
    if (-not (Test-Path $manifestPath)) {
        throw "Host dependency manifest not found: $manifestPath"
    }

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
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

$siblingSourceDir = Resolve-SiblingSourceDir
if ($UseSiblingRepoBuild -or -not [string]::IsNullOrWhiteSpace($siblingSourceDir)) {
    if (-not [string]::IsNullOrWhiteSpace($siblingSourceDir)) {
        Copy-FromSourceDir -ResolvedSourceDir $siblingSourceDir
        exit 0
    }

        Write-Warning "UseSiblingRepoBuild was requested but no sibling UniversalDeviceToolkit build output was found."
    }

Copy-FromReleaseZip
