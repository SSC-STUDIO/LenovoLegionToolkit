[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [ValidatePattern('^[A-Fa-f0-9]{64}$')]
    [string]$InstallerSha256,

    [string]$HashManifestPath,

    [Parameter(Mandatory)]
    [ValidatePattern('^\d{4}-\d{2}-\d{2}$')]
    [string]$ReleaseDate,

    [string]$InstallerUrl,

    [string]$RootPath = '',

    [switch]$UpdatePublishedScoopManifest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$null = [DateTime]::ParseExact(
    $ReleaseDate,
    'yyyy-MM-dd',
    [System.Globalization.CultureInfo]::InvariantCulture)

if ([string]::IsNullOrWhiteSpace($RootPath))
{
    $RootPath = if ($PSScriptRoot)
    {
        (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
    else
    {
        throw 'RootPath was not provided and script root could not be determined.'
    }
}

$repoRoot = (Resolve-Path $RootPath).Path

function Resolve-RepositoryPath
{
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path))
    {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

function Get-HashFromManifest
{
    param(
        [Parameter(Mandatory)]
        [string]$ManifestPath,

        [Parameter(Mandatory)]
        [string]$AssetName
    )

    if (-not (Test-Path -LiteralPath $ManifestPath))
    {
        throw "SHA256 manifest not found at '$ManifestPath'."
    }

    foreach ($line in Get-Content -LiteralPath $ManifestPath)
    {
        if ($line -match '^(?<hash>[0-9a-fA-F]{64})\s+(?<name>.+)$' -and
            $Matches.name.Trim() -ieq $AssetName)
        {
            return $Matches.hash
        }
    }

    throw "SHA256 manifest '$ManifestPath' does not contain '$AssetName'."
}

$legacyAssetName = "UniversalDeviceToolkit_v${Version}_Setup.exe"

if (-not [string]::IsNullOrWhiteSpace($HashManifestPath))
{
    $manifestHash = Get-HashFromManifest -ManifestPath (Resolve-RepositoryPath $HashManifestPath) -AssetName $legacyAssetName
    if ([string]::IsNullOrWhiteSpace($InstallerSha256))
    {
        $InstallerSha256 = $manifestHash
    }
    elseif ($InstallerSha256.ToUpperInvariant() -cne $manifestHash.ToUpperInvariant())
    {
        throw "Installer SHA256 '$InstallerSha256' does not match '$legacyAssetName' in '$HashManifestPath'."
    }
}

if ([string]::IsNullOrWhiteSpace($InstallerSha256))
{
    throw 'Provide either -InstallerSha256 or -HashManifestPath.'
}

$installerSha256Upper = $InstallerSha256.ToUpperInvariant()
$installerSha256Lower = $InstallerSha256.ToLowerInvariant()

if ([string]::IsNullOrWhiteSpace($InstallerUrl))
{
    $InstallerUrl = "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/v${Version}/$legacyAssetName"
}

function Ensure-Directory
{
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path))
    {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Write-Utf8NoBomFile
{
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Content
    )

    $parent = Split-Path -Parent $Path
    Ensure-Directory -Path $parent
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Get-ScoopManifestContent
{
    param(
        [Parameter(Mandatory)]
        [string]$Version,

        [Parameter(Mandatory)]
        [string]$InstallerUrl,

        [Parameter(Mandatory)]
        [string]$InstallerSha256,

        [Parameter(Mandatory)]
        [string]$Notes
    )

    return @"
{
  "version": "$Version",
  "description": "Universal Device Toolkit is a lightweight, open-source utility for supported Lenovo Legion, LOQ, Ideapad Gaming, and related laptops.",
  "homepage": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit",
  "license": "GPL-3.0-only",
  "notes": "$Notes",
  "architecture": {
    "64bit": {
      "url": "$InstallerUrl",
      "hash": "$InstallerSha256"
    }
  },
  "innosetup": true,
  "shortcuts": [
    [
      "Universal Device Toolkit.exe",
      "Universal Device Toolkit"
    ]
  ],
  "checkver": {
    "url": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest",
    "regex": "/releases/tag/v([\\d.]+)"
  },
  "autoupdate": {
    "architecture": {
      "64bit": {
        "url": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/v`$version/UniversalDeviceToolkit_v`$version_Setup.exe"
      }
    }
  }
}
"@
}

$wingetVersionDirectory = Join-Path $repoRoot "Packaging\winget\manifests\s\SSC-STUDIO\UniversalDeviceToolkit\$Version"
Ensure-Directory -Path $wingetVersionDirectory

$wingetVersionManifestPath = Join-Path $wingetVersionDirectory 'SSC-STUDIO.UniversalDeviceToolkit.yaml'
$wingetLocaleManifestPath = Join-Path $wingetVersionDirectory 'SSC-STUDIO.UniversalDeviceToolkit.locale.en-US.yaml'
$wingetInstallerManifestPath = Join-Path $wingetVersionDirectory 'SSC-STUDIO.UniversalDeviceToolkit.installer.yaml'

$wingetVersionManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.10.0.schema.json

PackageIdentifier: SSC-STUDIO.UniversalDeviceToolkit
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.10.0
"@

$wingetLocaleManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.10.0.schema.json

PackageIdentifier: SSC-STUDIO.UniversalDeviceToolkit
PackageVersion: $Version
PackageLocale: en-US
Publisher: SSC-STUDIO
PublisherUrl: https://github.com/SSC-STUDIO
PublisherSupportUrl: https://github.com/SSC-STUDIO/UniversalDeviceToolkit/issues
PackageName: Universal Device Toolkit
PackageUrl: https://github.com/SSC-STUDIO/UniversalDeviceToolkit
License: GPL-3.0
LicenseUrl: https://github.com/SSC-STUDIO/UniversalDeviceToolkit/blob/master/LICENSE
Copyright: Copyright (C) Universal Device Toolkit contributors
ShortDescription: Lightweight open-source Windows device toolkit — Legion hardware control, plugins, no telemetry.
Description: Universal Device Toolkit (UDT) is a lightweight, open-source Windows utility. Supported Lenovo Legion, LOQ, and IdeaPad Gaming laptops get direct hardware controls (power modes, RGB, dGPU, battery, Custom Mode, and more). Other Lenovo models and non-Lenovo PCs run in basic mode with plugins, system optimization, themes, and updates. No background service, no telemetry, no account. CLI automation via udt-cli.exe. Legacy package ID SSC-STUDIO.UniversalDeviceToolkit is retained for in-place upgrades from Lenovo Legion Toolkit.
Moniker: universaldevicetoolkit
Tags:
- lenovo
- legion
- loq
- vantage
- toolkit
- rgb
- laptop
- hardware-control
ReleaseNotesUrl: https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/tag/v$Version
ManifestType: defaultLocale
ManifestVersion: 1.10.0
"@

$wingetInstallerManifest = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.10.0.schema.json

PackageIdentifier: SSC-STUDIO.UniversalDeviceToolkit
PackageVersion: $Version
InstallerType: inno
Scope: machine
UpgradeBehavior: install
ReleaseDate: $ReleaseDate
Installers:
- Architecture: x64
  InstallerUrl: $InstallerUrl
  InstallerSha256: $installerSha256Upper
ManifestType: installer
ManifestVersion: 1.10.0
"@

Write-Utf8NoBomFile -Path $wingetVersionManifestPath -Content $wingetVersionManifest
Write-Utf8NoBomFile -Path $wingetLocaleManifestPath -Content $wingetLocaleManifest
Write-Utf8NoBomFile -Path $wingetInstallerManifestPath -Content $wingetInstallerManifest

$draftScoopManifestPath = Join-Path $repoRoot "Packaging\scoop\lenovolegiontoolkit.$Version.draft.json"
$publishedScoopManifestPath = Join-Path $repoRoot 'Packaging\scoop\lenovolegiontoolkit.json'

$draftScoopManifest = Get-ScoopManifestContent `
    -Version $Version `
    -InstallerUrl $InstallerUrl `
    -InstallerSha256 $installerSha256Lower `
    -Notes "Draft manifest generated from release metadata for version $Version. Publish it to the Scoop bucket only after validating install and upgrade behavior."

Write-Utf8NoBomFile -Path $draftScoopManifestPath -Content $draftScoopManifest

if ($UpdatePublishedScoopManifest)
{
    $publishedScoopManifest = Get-ScoopManifestContent `
        -Version $Version `
        -InstallerUrl $InstallerUrl `
        -InstallerSha256 $installerSha256Lower `
        -Notes 'Universal Device Toolkit keeps the legacy lenovolegiontoolkit Scoop package name so existing installs can upgrade in place.'

    Write-Utf8NoBomFile -Path $publishedScoopManifestPath -Content $publishedScoopManifest
}

Write-Host "Updated winget manifests in: $wingetVersionDirectory"
Write-Host "Updated scoop draft manifest: $draftScoopManifestPath"

if ($UpdatePublishedScoopManifest)
{
    Write-Host "Updated published scoop manifest: $publishedScoopManifestPath"
}
