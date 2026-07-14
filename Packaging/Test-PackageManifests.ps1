param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Repository = 'SSC-STUDIO/UniversalDeviceToolkit',

    [string]$HashManifestPath,

    [string]$ExpectedInstallerSha256,

    [string]$ExpectedPackageIdentifier = 'SSC-STUDIO.UniversalDeviceToolkit',

    [string]$ExpectedPackageName = 'Universal Device Toolkit',

    [string]$ExpectedPublisher = 'SSC-STUDIO',

    [string]$InstallerScriptPath = 'MakeInstaller.iss',

    [string]$WingetManifestDirectory,

    [string[]]$ScoopManifestPaths
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

function Resolve-RepositoryPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

function Read-YamlScalar {
    param(
        [AllowEmptyString()][string[]]$Lines,
        [Parameter(Mandatory = $true)][string]$Key
    )

    $pattern = '^\s*' + [regex]::Escape($Key) + '\s*:\s*(.+?)\s*$'
    foreach ($line in $Lines) {
        if ($line -match $pattern) {
            return $Matches[1].Trim("'`"")
        }
    }

    throw "Could not find YAML key '$Key'."
}

function Get-HashFromManifest {
    param(
        [Parameter(Mandatory = $true)][string]$ManifestPath,
        [Parameter(Mandatory = $true)][string]$AssetName
    )

    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        throw "SHA256 manifest not found at '$ManifestPath'."
    }

    foreach ($line in Get-Content -LiteralPath $ManifestPath) {
        if ($line -match '^(?<hash>[0-9a-fA-F]{64})\s+(?<name>.+)$' -and
            $Matches.name.Trim() -ieq $AssetName) {
            return $Matches.hash.ToUpperInvariant()
        }
    }

    throw "SHA256 manifest '$ManifestPath' does not contain '$AssetName'."
}

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][string]$Actual,
        [AllowNull()][string]$Expected
    )

    if ($Actual -cne $Expected) {
        throw "$Name mismatch. Expected '$Expected', got '$Actual'."
    }
}

function Get-InnoDefine {
    param(
        [Parameter(Mandatory = $true)][string]$ScriptPath,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not (Test-Path -LiteralPath $ScriptPath)) {
        throw "Inno Setup script not found at '$ScriptPath'."
    }

    $pattern = '^\s*#define\s+' + [regex]::Escape($Name) + '\s+"(?<value>[^"]+)"\s*$'
    foreach ($line in Get-Content -LiteralPath $ScriptPath) {
        if ($line -match $pattern) {
            return $Matches.value
        }
    }

    throw "Could not find Inno Setup define '$Name'."
}

$legacyAssetName = "UniversalDeviceToolkit_v${Version}_Setup.exe"
$expectedInstallerUrl = "https://github.com/$Repository/releases/download/v$Version/$legacyAssetName"

if (-not [string]::IsNullOrWhiteSpace($HashManifestPath)) {
    $resolvedHashManifestPath = Resolve-RepositoryPath $HashManifestPath
    $manifestHash = Get-HashFromManifest -ManifestPath $resolvedHashManifestPath -AssetName $legacyAssetName
    if ([string]::IsNullOrWhiteSpace($ExpectedInstallerSha256)) {
        $ExpectedInstallerSha256 = $manifestHash
    }
    else {
        Assert-Equal 'Expected installer SHA256 and release SHA256 manifest' $ExpectedInstallerSha256.ToUpperInvariant() $manifestHash
    }
}

if ([string]::IsNullOrWhiteSpace($ExpectedInstallerSha256)) {
    throw 'Provide either -HashManifestPath or -ExpectedInstallerSha256.'
}

$expectedInstallerSha256Upper = $ExpectedInstallerSha256.ToUpperInvariant()
$expectedInstallerSha256Lower = $ExpectedInstallerSha256.ToLowerInvariant()

if ([string]::IsNullOrWhiteSpace($WingetManifestDirectory)) {
    $WingetManifestDirectory = "Packaging\winget\manifests\s\SSC-STUDIO\UniversalDeviceToolkit\$Version"
}

$resolvedWingetDirectory = Resolve-RepositoryPath $WingetManifestDirectory
$wingetVersionManifestPath = Join-Path $resolvedWingetDirectory 'SSC-STUDIO.UniversalDeviceToolkit.yaml'
$wingetLocaleManifestPath = Join-Path $resolvedWingetDirectory 'SSC-STUDIO.UniversalDeviceToolkit.locale.en-US.yaml'
$wingetInstallerManifestPath = Join-Path $resolvedWingetDirectory 'SSC-STUDIO.UniversalDeviceToolkit.installer.yaml'
if (-not (Test-Path -LiteralPath $wingetVersionManifestPath)) {
    throw "winget version manifest not found at '$wingetVersionManifestPath'."
}
if (-not (Test-Path -LiteralPath $wingetLocaleManifestPath)) {
    throw "winget locale manifest not found at '$wingetLocaleManifestPath'."
}
if (-not (Test-Path -LiteralPath $wingetInstallerManifestPath)) {
    throw "winget installer manifest not found at '$wingetInstallerManifestPath'."
}

$resolvedInstallerScriptPath = Resolve-RepositoryPath $InstallerScriptPath
$installerPublisher = Get-InnoDefine -ScriptPath $resolvedInstallerScriptPath -Name 'MyAppPublisher'
$wingetVersionLines = Get-Content -LiteralPath $wingetVersionManifestPath
$wingetLocaleLines = Get-Content -LiteralPath $wingetLocaleManifestPath

Assert-Equal 'installer AppPublisher' $installerPublisher $ExpectedPublisher
Assert-Equal 'winget version PackageIdentifier' (Read-YamlScalar -Lines $wingetVersionLines -Key 'PackageIdentifier') $ExpectedPackageIdentifier
Assert-Equal 'winget version PackageVersion' (Read-YamlScalar -Lines $wingetVersionLines -Key 'PackageVersion') $Version
Assert-Equal 'winget version DefaultLocale' (Read-YamlScalar -Lines $wingetVersionLines -Key 'DefaultLocale') 'en-US'
Assert-Equal 'winget version ManifestType' (Read-YamlScalar -Lines $wingetVersionLines -Key 'ManifestType') 'version'
Assert-Equal 'winget locale PackageIdentifier' (Read-YamlScalar -Lines $wingetLocaleLines -Key 'PackageIdentifier') $ExpectedPackageIdentifier
Assert-Equal 'winget locale PackageVersion' (Read-YamlScalar -Lines $wingetLocaleLines -Key 'PackageVersion') $Version
Assert-Equal 'winget Publisher' (Read-YamlScalar -Lines $wingetLocaleLines -Key 'Publisher') $ExpectedPublisher
Assert-Equal 'winget PackageName' (Read-YamlScalar -Lines $wingetLocaleLines -Key 'PackageName') $ExpectedPackageName

$wingetLines = Get-Content -LiteralPath $wingetInstallerManifestPath
Assert-Equal 'winget installer PackageIdentifier' (Read-YamlScalar -Lines $wingetLines -Key 'PackageIdentifier') $ExpectedPackageIdentifier
Assert-Equal 'winget PackageVersion' (Read-YamlScalar -Lines $wingetLines -Key 'PackageVersion') $Version
Assert-Equal 'winget InstallerType' (Read-YamlScalar -Lines $wingetLines -Key 'InstallerType') 'inno'
Assert-Equal 'winget Scope' (Read-YamlScalar -Lines $wingetLines -Key 'Scope') 'machine'
Assert-Equal 'winget InstallerUrl' (Read-YamlScalar -Lines $wingetLines -Key 'InstallerUrl') $expectedInstallerUrl
Assert-Equal 'winget InstallerSha256' (Read-YamlScalar -Lines $wingetLines -Key 'InstallerSha256').ToUpperInvariant() $expectedInstallerSha256Upper

if ($ScoopManifestPaths.Count -eq 0) {
    $defaultScoopPaths = @(
        'Packaging\scoop\lenovolegiontoolkit.json',
        "Packaging\scoop\lenovolegiontoolkit.$Version.draft.json"
    )

    $ScoopManifestPaths = @(
        $defaultScoopPaths |
            Where-Object { Test-Path -LiteralPath (Resolve-RepositoryPath $_) }
    )
}

if ($ScoopManifestPaths.Count -eq 0) {
    throw 'No Scoop manifests were provided or found for validation.'
}

foreach ($scoopManifestPath in $ScoopManifestPaths) {
    $resolvedScoopManifestPath = Resolve-RepositoryPath $scoopManifestPath
    if (-not (Test-Path -LiteralPath $resolvedScoopManifestPath)) {
        throw "Scoop manifest not found at '$resolvedScoopManifestPath'."
    }

    $scoopManifest = Get-Content -Raw -LiteralPath $resolvedScoopManifestPath | ConvertFrom-Json
    Assert-Equal "Scoop version in '$scoopManifestPath'" ([string]$scoopManifest.version) $Version
    Assert-Equal "Scoop URL in '$scoopManifestPath'" ([string]$scoopManifest.architecture.'64bit'.url) $expectedInstallerUrl
    Assert-Equal "Scoop hash in '$scoopManifestPath'" ([string]$scoopManifest.architecture.'64bit'.hash) $expectedInstallerSha256Lower
}

Write-Host "Package manifests match $legacyAssetName and SHA256 $expectedInstallerSha256Upper."
