[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$InstallerOutput = 'BuildInstaller'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Builds the Electron NSIS installer (UniversalDeviceToolkit.Electron) in
# both flavors, mirroring the retired WPF installer output layout:
#   BuildInstaller\UniversalDeviceToolkitSetup.exe
#   BuildInstaller\UniversalDeviceToolkitOnlineSetup.exe
# The Electron app embeds the self-contained .NET host via extraResources.
# Full is packed first (all Host satellites). Online is packed after pruning
# the Host publish dir to English-only satellites.

$repoRoot = Split-Path -Parent $PSScriptRoot
$electronProject = Join-Path $repoRoot 'UniversalDeviceToolkit.Electron'
$hostPublishDir = Join-Path $repoRoot 'UniversalDeviceToolkit.Host\publish\win-x64'
$pruneScript = Join-Path $PSScriptRoot 'Prune-ShippingFootprint.ps1'
$channelFile = Join-Path $electronProject 'resources\install-channel'
$distDir = Join-Path $electronProject 'dist'

if (-not (Test-Path -LiteralPath (Join-Path $electronProject 'package.json'))) {
    throw "Electron project not found at '$electronProject'."
}

$installerOutputPath = Join-Path $repoRoot $InstallerOutput
New-Item -ItemType Directory -Path $installerOutputPath -Force | Out-Null

function Set-InstallChannel {
    param([Parameter(Mandatory = $true)][string]$Channel)

    $resourcesDir = Split-Path -Parent $channelFile
    New-Item -ItemType Directory -Path $resourcesDir -Force | Out-Null
    Set-Content -LiteralPath $channelFile -Value $Channel -Encoding ascii -NoNewline
}

function Invoke-ElectronWinTarget {
    param([Parameter(Mandatory = $true)][string]$Target)

    npx electron-builder --config electron-builder.yml --win $Target
    if ($LASTEXITCODE -ne 0) {
        throw "electron-builder ($Target) failed."
    }
}

function Get-LatestArtifact {
    param(
        [Parameter(Mandatory = $true)][string]$Filter,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $artifact = Get-ChildItem -LiteralPath $distDir -Filter $Filter -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -eq $artifact) {
        throw "$Description artifact not found under '$distDir'."
    }
    return $artifact
}

function Assert-ElectronZip {
    param([Parameter(Mandatory = $true)][string]$Path)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entries = @($archive.Entries | ForEach-Object { $_.FullName.TrimStart('/') })
        if (-not ($entries | Where-Object { $_ -match '(^|/)UniversalDeviceToolkit\.exe$' })) {
            throw "Electron ZIP '$Path' does not contain UniversalDeviceToolkit.exe."
        }
        if (-not ($entries | Where-Object { $_ -match '(^|/)resources/host/|(^|/)resources\\host\\' })) {
            throw "Electron ZIP '$Path' does not contain resources/host."
        }
    }
    finally {
        $archive.Dispose()
    }
}

# Build the renderer + main process, then package with electron-builder.
Push-Location $electronProject
try {
    if (Test-Path -LiteralPath $distDir) {
        Remove-Item -LiteralPath $distDir -Recurse -Force
    }

    # electron-builder reads the version from package.json; sync it with the
    # release version resolved from Directory.Build.props.
    $packageJsonPath = Join-Path $electronProject 'package.json'
    $packageJson = Get-Content -LiteralPath $packageJsonPath -Raw | ConvertFrom-Json
    $packageJson.version = $Version
    $packageJson | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $packageJsonPath -Encoding utf8

    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw 'Electron build failed.'
    }

    Set-InstallChannel -Channel 'full'
    Invoke-ElectronWinTarget -Target 'nsis'
    Invoke-ElectronWinTarget -Target 'zip'

    $fullZipArtifact = Get-LatestArtifact -Filter 'UniversalDeviceToolkitSetup-*.zip' -Description 'Electron Full ZIP'
    $fullZipPath = Join-Path $installerOutputPath "UniversalDeviceToolkit_v${Version}_Full_win-x64.zip"
    Copy-Item -LiteralPath $fullZipArtifact.FullName -Destination $fullZipPath -Force
    Assert-ElectronZip -Path $fullZipPath
    Write-Host "Electron Full ZIP built: $fullZipPath"

    if (Test-Path -LiteralPath $hostPublishDir) {
        & $pruneScript -PayloadPath $hostPublishDir -AllowedCultures 'en'
    }
    else {
        Write-Warning "Host publish directory not found at '$hostPublishDir'; Online payload will not be English-pruned."
    }

    Set-InstallChannel -Channel 'online'
    Invoke-ElectronWinTarget -Target 'nsis-web'
    Invoke-ElectronWinTarget -Target 'zip'
}
finally {
    Pop-Location
}

# Locate the produced NSIS setup artifacts.
# Full (offline) uses UniversalDeviceToolkitSetup-*.exe
# Online (nsis-web stub) uses UniversalDeviceToolkitOnlineSetup-*.exe
$setupArtifact = Get-ChildItem -LiteralPath $distDir -Filter 'UniversalDeviceToolkitSetup-*.exe' -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notmatch 'Online' } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $setupArtifact) {
    throw "NSIS Full setup artifact not found under '$distDir'."
}

$finalSetupPath = Join-Path $installerOutputPath 'UniversalDeviceToolkitSetup.exe'
Copy-Item -LiteralPath $setupArtifact.FullName -Destination $finalSetupPath -Force
Write-Host "Electron Full installer built: $finalSetupPath ($([math]::Round($setupArtifact.Length / 1MB, 1)) MB)"

$onlineArtifact = Get-ChildItem -LiteralPath $distDir -Filter 'UniversalDeviceToolkitOnlineSetup-*.exe' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $onlineArtifact) {
    throw "NSIS Online (nsis-web) setup artifact not found under '$distDir'."
}

$onlineZipArtifact = Get-LatestArtifact -Filter 'UniversalDeviceToolkitSetup-*.zip' -Description 'Electron Online ZIP'

$maxOnlineBytes = 15MB
if ($onlineArtifact.Length -gt $maxOnlineBytes) {
    throw "Online installer is $([math]::Round($onlineArtifact.Length / 1MB, 2)) MB; must be <= 15 MB."
}

$finalOnlinePath = Join-Path $installerOutputPath 'UniversalDeviceToolkitOnlineSetup.exe'
Copy-Item -LiteralPath $onlineArtifact.FullName -Destination $finalOnlinePath -Force
Write-Host "Electron Online installer built: $finalOnlinePath ($([math]::Round($onlineArtifact.Length / 1MB, 2)) MB)"

$onlineZipPath = Join-Path $installerOutputPath "UniversalDeviceToolkit_v${Version}_Online_win-x64.zip"
Copy-Item -LiteralPath $onlineZipArtifact.FullName -Destination $onlineZipPath -Force
Assert-ElectronZip -Path $onlineZipPath
Write-Host "Electron Online ZIP built: $onlineZipPath"

$nsisPackages = @(Get-ChildItem -LiteralPath $distDir -Filter '*.nsis.7z' -Recurse -ErrorAction SilentlyContinue)
if ($nsisPackages.Count -eq 0) {
    throw "nsis-web package (*.nsis.7z) not found under '$distDir'."
}

foreach ($package in $nsisPackages) {
    Copy-Item -LiteralPath $package.FullName -Destination (Join-Path $installerOutputPath $package.Name) -Force
    Write-Host "Copied nsis-web package: $($package.Name) ($([math]::Round($package.Length / 1MB, 1)) MB)"
}

Get-ChildItem -LiteralPath $installerOutputPath -File |
    Select-Object Name, Length, LastWriteTime |
    Format-Table -AutoSize
