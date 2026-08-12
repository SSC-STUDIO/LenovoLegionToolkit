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
# The Electron app embeds the self-contained .NET host via extraResources.
# (Inno Setup and the WPF Tools/Installer are retired.)

$repoRoot = Split-Path -Parent $PSScriptRoot
$electronProject = Join-Path $repoRoot 'UniversalDeviceToolkit.Electron'

if (-not (Test-Path -LiteralPath (Join-Path $electronProject 'package.json'))) {
    throw "Electron project not found at '$electronProject'."
}

$installerOutputPath = Join-Path $repoRoot $InstallerOutput
New-Item -ItemType Directory -Path $installerOutputPath -Force | Out-Null

# Build the renderer + main process, then package with electron-builder.
Push-Location $electronProject
try {
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

    npx electron-builder --config electron-builder.yml --win
    if ($LASTEXITCODE -ne 0) {
        throw 'electron-builder failed.'
    }
}
finally {
    Pop-Location
}

# Locate the produced NSIS setup artifact.
$distDir = Join-Path $electronProject 'dist'
$setupArtifact = Get-ChildItem -LiteralPath $distDir -Filter 'UniversalDeviceToolkitSetup-*.exe' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $setupArtifact) {
    throw "NSIS setup artifact not found under '$distDir'."
}

$finalSetupPath = Join-Path $installerOutputPath 'UniversalDeviceToolkitSetup.exe'
Copy-Item -LiteralPath $setupArtifact.FullName -Destination $finalSetupPath -Force

Write-Host "Electron installer built: $finalSetupPath"

Get-ChildItem -LiteralPath $installerOutputPath -Filter '*.exe' |
    Select-Object Name, Length, LastWriteTime |
    Format-Table -AutoSize
