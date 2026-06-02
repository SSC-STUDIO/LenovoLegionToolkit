param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Repository = 'SSC-STUDIO/UniversalDeviceToolkit',

    [string]$HashManifestPath,

    [string]$ExpectedInstallerSha256,

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

$legacyAssetName = "LenovoLegionToolkit_v${Version}_Setup.exe"
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
    $WingetManifestDirectory = "Packaging\winget\manifests\s\SSC-STUDIO\LenovoLegionToolkit\$Version"
}

$resolvedWingetDirectory = Resolve-RepositoryPath $WingetManifestDirectory
$wingetInstallerManifestPath = Join-Path $resolvedWingetDirectory 'SSC-STUDIO.LenovoLegionToolkit.installer.yaml'
if (-not (Test-Path -LiteralPath $wingetInstallerManifestPath)) {
    throw "winget installer manifest not found at '$wingetInstallerManifestPath'."
}

$wingetLines = Get-Content -LiteralPath $wingetInstallerManifestPath
Assert-Equal 'winget PackageVersion' (Read-YamlScalar -Lines $wingetLines -Key 'PackageVersion') $Version
Assert-Equal 'winget Scope' (Read-YamlScalar -Lines $wingetLines -Key 'Scope') 'machine'
Assert-Equal 'winget InstallerUrl' (Read-YamlScalar -Lines $wingetLines -Key 'InstallerUrl') $expectedInstallerUrl
Assert-Equal 'winget InstallerSha256' (Read-YamlScalar -Lines $wingetLines -Key 'InstallerSha256').ToUpperInvariant() $expectedInstallerSha256Upper

if ($ScoopManifestPaths.Count -eq 0) {
    $defaultScoopPaths = @(
        'Packaging\scoop\lenovolegiontoolkit.json',
        "Packaging\scoop\lenovolegiontoolkit.$Version.draft.json"
    )

    $ScoopManifestPaths = $defaultScoopPaths |
        Where-Object { Test-Path -LiteralPath (Resolve-RepositoryPath $_) }
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
