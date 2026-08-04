<#
.SYNOPSIS
    Downloads Nilesoft Shell binaries from GitHub Releases for the ShellIntegration plugin.

.DESCRIPTION
    Fetches the latest release archive from the Shell GitHub repository, extracts the
    required files (shell.exe, shell.dll, shell.nss, imports/), and places them into
    the Dependencies/Shell/ directory. Skips download if files already exist unless
    -Force is specified.

.PARAMETER Force
    Re-download and overwrite existing files even if they are already present.

.NOTES
    GitHub Repository: https://github.com/moudey/Shell
    If the URL changes or you use a fork, update $GitHubRepo below.
#>

[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# --- Configuration ---
# Update this if the repo URL changes or you use a different fork/mirror.
$GitHubRepo = 'moudey/Shell'
$GitHubApiUrl = "https://api.github.com/repos/$GitHubRepo/releases/latest"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..')).Path
$OutputDir = Join-Path (Join-Path $RepoRoot 'Dependencies') 'Shell'
$TempDir = Join-Path $OutputDir '.tmp'

$RequiredFiles = @('shell.exe', 'shell.dll', 'shell.nss')
$RequiredDirs = @('imports')

# --- Functions ---

function Test-ShellBinariesExist {
    foreach ($file in $RequiredFiles) {
        if (-not (Test-Path (Join-Path $OutputDir $file))) {
            return $false
        }
    }
    foreach ($dir in $RequiredDirs) {
        if (-not (Test-Path (Join-Path $OutputDir $dir))) {
            return $false
        }
    }
    return $true
}

function Get-LatestReleaseAssetUrl {
    Write-Host "Querying GitHub API for latest release of $GitHubRepo..."

    $headers = @{ 'User-Agent' = 'fetch-shell-binaries/1.0' }
    if ($env:GITHUB_TOKEN) {
        $headers['Authorization'] = "Bearer $env:GITHUB_TOKEN"
    }

    try {
        $release = Invoke-RestMethod -Uri $GitHubApiUrl -Headers $headers -TimeoutSec 30
    }
    catch {
        throw "Failed to query GitHub API: $_`nIf you are rate-limited, set the GITHUB_TOKEN environment variable."
    }

    Write-Host "Latest release: $($release.tag_name)"

    # Look for a zip asset. Nilesoft Shell releases typically have a zip archive.
    # Prefer an asset whose name contains 'shell' and ends with '.zip'.
    $asset = $release.assets |
        Where-Object { $_.name -match '\.zip$' } |
        Sort-Object -Property size -Descending |
        Select-Object -First 1

    if (-not $asset) {
        # Fallback: use the source zipball
        $url = $release.zipball_url
        if (-not $url) {
            throw "No suitable zip asset found in the latest release and no zipball_url available."
        }
        Write-Host "No zip asset found; falling back to source zipball."
        return @{ Url = $url; FileName = "source.zip" }
    }

    Write-Host "Selected asset: $($asset.name) ($([math]::Round($asset.size / 1MB, 2)) MB)"
    return @{ Url = $asset.browser_download_url; FileName = $asset.name }
}

function Expand-ShellArchive {
    param(
        [string]$ArchivePath,
        [string]$ExtractTo
    )

    Write-Host "Extracting archive to $ExtractTo..."

    if (Test-Path $ExtractTo) {
        Remove-Item $ExtractTo -Recurse -Force
    }
    New-Item -ItemType Directory -Path $ExtractTo -Force | Out-Null

    Expand-Archive -Path $ArchivePath -DestinationPath $ExtractTo -Force
}

function Find-AndCopyBinaries {
    param(
        [string]$SearchRoot,
        [string]$Destination
    )

    # Search for shell.exe anywhere in the extracted tree
    $shellExe = Get-ChildItem -Path $SearchRoot -Recurse -Filter 'shell.exe' -File | Select-Object -First 1

    if (-not $shellExe) {
        throw "Could not find shell.exe in the extracted archive. The release format may have changed."
    }

    $binDir = $shellExe.DirectoryName
    Write-Host "Found binaries in: $binDir"

    if (-not (Test-Path $Destination)) {
        New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    }

    # Copy required files
    foreach ($file in $RequiredFiles) {
        $src = Join-Path $binDir $file
        if (Test-Path $src) {
            Copy-Item $src -Destination $Destination -Force
            Write-Host "  Copied $file"
        }
        else {
            Write-Warning "  $file not found in $binDir"
        }
    }

    # Copy required directories
    foreach ($dir in $RequiredDirs) {
        $src = Join-Path $binDir $dir
        if (Test-Path $src) {
            $dest = Join-Path $Destination $dir
            if (Test-Path $dest) {
                Remove-Item $dest -Recurse -Force
            }
            Copy-Item $src -Destination $dest -Recurse -Force
            $count = (Get-ChildItem $dest -Recurse -File).Count
            Write-Host "  Copied $dir/ ($count files)"
        }
        else {
            Write-Warning "  $dir/ not found in $binDir"
        }
    }
}

# --- Main ---

Write-Host ''
Write-Host '=== Fetch Shell Binaries ===' -ForegroundColor Cyan
Write-Host ''

if ((Test-ShellBinariesExist) -and (-not $Force)) {
    Write-Host "Shell binaries already present in $OutputDir" -ForegroundColor Green
    Write-Host "Use -Force to re-download."
    exit 0
}

if ($Force -and (Test-ShellBinariesExist)) {
    Write-Host "Force flag set; re-downloading..." -ForegroundColor Yellow
}

try {
    $assetInfo = Get-LatestReleaseAssetUrl

    # Download
    $archivePath = Join-Path $OutputDir $assetInfo.FileName
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    Write-Host "Downloading $($assetInfo.FileName)..."
    $ProgressPreference = 'SilentlyContinue'  # Speed up Invoke-WebRequest
    Invoke-WebRequest -Uri $assetInfo.Url -OutFile $archivePath -TimeoutSec 120 -Headers @{ 'User-Agent' = 'fetch-shell-binaries/1.0' }
    Write-Host "Download complete."

    # Extract
    Expand-ShellArchive -ArchivePath $archivePath -ExtractTo $TempDir

    # Find and copy binaries
    Find-AndCopyBinaries -SearchRoot $TempDir -Destination $OutputDir

    # Verify
    if (Test-ShellBinariesExist) {
        Write-Host ''
        Write-Host 'Shell binaries fetched successfully.' -ForegroundColor Green
    }
    else {
        Write-Warning 'Some expected files are missing after extraction. Check the release archive format.'
        exit 1
    }
}
catch {
    Write-Host ''
    Write-Host "ERROR: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    exit 1
}
finally {
    # Cleanup temp files
    if (Test-Path $TempDir) {
        Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    if ($archivePath -and (Test-Path $archivePath)) {
        Remove-Item $archivePath -Force -ErrorAction SilentlyContinue
    }
}
