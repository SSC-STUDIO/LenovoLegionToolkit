[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$ReleaseOutput = 'release-assets',
    [string]$InstallerOutput = 'BuildInstaller'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Builds the self-contained WPF installer (Tools/Installer) in both flavors:
#   Online (~0.3 MB) downloads the Online payload zip at install time
#     (direct + mirror fallbacks, SHA-256 verified).
#   Full embeds the Full payload zip as a resource for fully offline installs.
# Outputs BuildInstaller\UniversalDeviceToolkitSetup-{Full,Online}.exe, the names
# Scripts/Build-LanguageAssets.ps1 -FinalizeOnly expects.

$repoRoot = Split-Path -Parent $PSScriptRoot

$onlineZipName = "UniversalDeviceToolkit_v${Version}_Online_win-x64.zip"
$fullZipName = "UniversalDeviceToolkit_v${Version}_Full_win-x64.zip"

$releaseOutputPath = Join-Path $repoRoot $ReleaseOutput
$installerOutputPath = Join-Path $repoRoot $InstallerOutput
$onlineZipPath = Join-Path $releaseOutputPath $onlineZipName
$fullZipPath = Join-Path $releaseOutputPath $fullZipName

foreach ($zipPath in @($onlineZipPath, $fullZipPath)) {
    if (-not (Test-Path -LiteralPath $zipPath)) {
        throw "Payload zip not found at '$zipPath'."
    }
}

New-Item -ItemType Directory -Path $installerOutputPath -Force | Out-Null

$onlineZipHash = (Get-FileHash -LiteralPath $onlineZipPath -Algorithm SHA256).Hash
$directUrl = "https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/v$Version/$onlineZipName"

# Regenerate the payload manifest so the Online flavor ships the real version,
# asset name, hash and mirror URLs for this release.
$payloadManifest = @"
namespace UniversalDeviceToolkit.Installer;

internal static class PayloadManifest
{
    public const string Version = "$Version";
    public const string AssetName = "$onlineZipName";
    public const string Sha256 = "$onlineZipHash";

    public static readonly string[] Urls =
    [
        "$directUrl",
        "https://gh-proxy.com/$directUrl",
        "https://ghfast.top/$directUrl",
    ];
}
"@
Set-Content -LiteralPath (Join-Path $repoRoot 'Tools\Installer\PayloadManifest.cs') -Value $payloadManifest -Encoding utf8

$installerProject = Join-Path $repoRoot 'Tools\Installer\UniversalDeviceToolkit.Installer.csproj'
$onlinePublishDir = Join-Path $installerOutputPath 'tmp-online'
$fullPublishDir = Join-Path $installerOutputPath 'tmp-full'

& dotnet publish $installerProject -c Release -r win-x64 --self-contained false -p:NuGetAudit=false "-p:PublishDir=$onlinePublishDir"
if ($LASTEXITCODE -ne 0) {
    throw 'Online installer publish failed.'
}

& dotnet publish $installerProject -c Release -r win-x64 --self-contained false -p:NuGetAudit=false "-p:PublishDir=$fullPublishDir" "-p:PayloadZipPath=$fullZipPath"
if ($LASTEXITCODE -ne 0) {
    throw 'Full installer publish failed.'
}

$fullSetupPath = Join-Path $installerOutputPath 'UniversalDeviceToolkitSetup-Full.exe'
$onlineSetupPath = Join-Path $installerOutputPath 'UniversalDeviceToolkitSetup-Online.exe'

Copy-Item -LiteralPath (Join-Path $onlinePublishDir 'UniversalDeviceToolkit.Installer.exe') -Destination $onlineSetupPath -Force
Copy-Item -LiteralPath (Join-Path $fullPublishDir 'UniversalDeviceToolkit.Installer.exe') -Destination $fullSetupPath -Force
Remove-Item -LiteralPath $onlinePublishDir, $fullPublishDir -Recurse -Force

foreach ($installerPath in @($fullSetupPath, $onlineSetupPath)) {
    if (-not (Test-Path -LiteralPath $installerPath)) {
        throw "Expected installer output was not created: $installerPath"
    }
}

Get-ChildItem -LiteralPath $installerOutputPath -Filter '*.exe' |
    Select-Object Name, Length, LastWriteTime |
    Format-Table -AutoSize
