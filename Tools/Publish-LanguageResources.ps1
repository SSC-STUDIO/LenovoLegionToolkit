#Requires -Version 5.1
<#
.SYNOPSIS
  Publishes online language-pack resources (resources/stable) from the built WPF output.

.DESCRIPTION
  Zips every satellite resource assembly (one per culture) into
  resources/stable/<version>/languages/<culture>.zip and generates
  resources/stable/catalog.json with SHA256 + size for each pack.

  The catalog is served through the repo (raw.githubusercontent.com / jsdelivr),
  so commit + push the resources folder after running this script.

.PARAMETER RuntimeDir
  Directory containing the built "Universal Device Toolkit.exe" and culture folders.
  Auto-detected under UniversalDeviceToolkit.WPF\bin when omitted.

.PARAMETER Version
  Resource version segment used in the output path (default: app file version).

.PARAMETER BaseUrl
  Public base URL that catalog entries point at.
#>
param(
    [string] $RuntimeDir = "",
    [string] $Version = "",
    [string] $BaseUrl = "https://cdn.jsdelivr.net/gh/SSC-STUDIO/UniversalDeviceToolkit@master/resources"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$sharedCatalogPath = Join-Path $repoRoot "UniversalDeviceToolkit.Lib.Abstractions\Localization\LocalizationCatalog.cs"
if (-not (Test-Path -LiteralPath $sharedCatalogPath)) {
    throw "Shared localization catalog not found: $sharedCatalogPath"
}

$sharedCatalogText = Get-Content -LiteralPath $sharedCatalogPath -Raw -Encoding UTF8
$sharedCatalogBlock = [regex]::Match(
    $sharedCatalogText,
    'SupportedCultures\s*\{\s*get;\s*\}\s*=\s*\[(?<values>[\s\S]*?)\];')
$sharedCultures = @([regex]::Matches($sharedCatalogBlock.Groups['values'].Value, 'new\("([^"]+)"\)') |
    ForEach-Object { $_.Groups[1].Value })
if ($sharedCultures.Count -eq 0) {
    throw "Could not read supported cultures from $sharedCatalogPath"
}

function Resolve-CanonicalCulture([string]$Name) {
    $match = $sharedCultures | Where-Object {
        $_.Equals($Name, [StringComparison]::OrdinalIgnoreCase)
    } | Select-Object -First 1
    if (-not $match) {
        throw "Runtime satellite directory '$Name' is not in the shared culture catalog."
    }

    return [string]$match
}

if (-not $RuntimeDir) {
    $binRoot = Join-Path $repoRoot "UniversalDeviceToolkit.WPF\bin"
    $exe = Get-ChildItem -Path $binRoot -Filter "Universal Device Toolkit.exe" -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if (-not $exe) { throw "Built app not found under $binRoot. Build the WPF project first." }
    $RuntimeDir = $exe.DirectoryName
}

if (-not $Version) {
    $exePath = Join-Path $RuntimeDir "Universal Device Toolkit.exe"
    $v = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath)
    $Version = "$($v.FileMajorPart).$($v.FileMinorPart).$($v.FileBuildPart)"
}

$stableRoot = Join-Path $repoRoot "resources\stable"
$langDir = Join-Path $stableRoot "$Version\languages"
New-Item -ItemType Directory -Force $langDir | Out-Null

$entries = @()
$cultureDirs = Get-ChildItem -Path $RuntimeDir -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName "Universal Device Toolkit.resources.dll") } |
    Sort-Object Name

foreach ($dir in $cultureDirs) {
    $culture = Resolve-CanonicalCulture $dir.Name
    if ($culture -eq "en") { continue } # English is built into the app; no pack needed.

    $zipPath = Join-Path $langDir "$culture.zip"
    $stage = Join-Path ([System.IO.Path]::GetTempPath()) "udt-lang-$culture-$([Guid]::NewGuid().ToString('N'))"
    try {
        New-Item -ItemType Directory -Force (Join-Path $stage $culture) | Out-Null
        Copy-Item (Join-Path $dir.FullName "Universal Device Toolkit.resources.dll") (Join-Path $stage $culture)
        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        Compress-Archive -Path (Join-Path $stage $culture) -DestinationPath $zipPath -CompressionLevel Optimal

        $hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
        $size = (Get-Item $zipPath).Length
        $displayName = [System.Globalization.CultureInfo]::GetCultureInfo($culture).NativeName

        $entries += [ordered]@{
            culture     = $culture
            displayName = $displayName
            url         = "$BaseUrl/stable/$Version/languages/$culture.zip"
            sha256      = $hash
            size        = $size
        }
        Write-Host "[publish] $culture -> $([Math]::Round($size / 1KB, 1)) KB"
    }
    finally {
        Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$catalog = [ordered]@{
    productName  = "Universal Device Toolkit"
    generatedAt  = [DateTime]::UtcNow.ToString("o")
    appVersion   = $Version
    schemaVersion = 1
    languages    = $entries
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$json = $catalog | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText((Join-Path $stableRoot "catalog.json"), $json, $utf8NoBom)

Write-Host "[publish] Wrote $($entries.Count) language packs + catalog.json (version $Version)"
