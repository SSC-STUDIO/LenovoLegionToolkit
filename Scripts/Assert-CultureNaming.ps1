<#
.SYNOPSIS
  Fail if any UDT culture name is not written in BCP 47 canonical form.

.DESCRIPTION
  Enforces the culture naming convention (CONTRIBUTING.md "Culture naming
  convention"). Culture names must be byte-for-byte canonical (language
  lowercase, script TitleCase, region UPPERCASE) everywhere UDT writes them:
    - resource file names (Resource.zh-Hans.resx, never zh-hans)
    - Directory.Build.props UdtSatelliteResourceLanguages
    - crowdin.yml locale mappings
    - Scripts/Build-LanguageAssets.ps1 pack definitions
    - catalog.json "culture" fields and language pack URLs
    - LocalizationHelper.Languages (C# source)
    - installer AppLanguages (FirstRunState.cs)

  Reads are allowed to stay lenient (case-insensitive matching in code), but
  written artifacts must use the canonical set below.
#>
param(
    [string]$RepositoryRoot = ''
)

$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    if (-not [string]::IsNullOrWhiteSpace($RepositoryRoot) -and (Test-Path (Join-Path $RepositoryRoot 'UniversalDeviceToolkit.sln'))) {
        return (Resolve-Path $RepositoryRoot).Path
    }
    $here = $PSScriptRoot
    $candidate = Resolve-Path (Join-Path $here '..')
    if (Test-Path (Join-Path $candidate 'UniversalDeviceToolkit.sln')) {
        return $candidate.Path
    }
    throw 'Could not resolve repository root. Pass -RepositoryRoot.'
}

# Canonical BCP 47 set — single source of truth, mirrors LocalizationHelper.Languages.
# Read the canonical list from the shared runtime catalog so every host and
# every release script validates the same cultures.
$catalogPath = Join-Path (Resolve-RepoRoot) 'UniversalDeviceToolkit.Lib.Abstractions\Localization\LocalizationCatalog.cs'
if (-not (Test-Path -LiteralPath $catalogPath)) {
    throw "Shared localization catalog not found: $catalogPath"
}

$catalogText = Get-Content -LiteralPath $catalogPath -Raw
$catalogBlock = [regex]::Match(
    $catalogText,
    'SupportedCultures\s*\{\s*get;\s*\}\s*=\s*\[(?<values>[\s\S]*?)\];')
$canonicalCultures = @([regex]::Matches($catalogBlock.Groups['values'].Value, 'new\("([^"]+)"\)') |
    ForEach-Object { $_.Groups[1].Value })
if ($canonicalCultures.Count -eq 0) {
    throw "Could not read supported cultures from $catalogPath"
}

$canonicalSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
foreach ($c in $canonicalCultures) { [void]$canonicalSet.Add($c) }

# Cultures with valid canonical spelling that are allowed in plugin scan
# exclusion lists and satellite build matrices even without app resx.
$extraAllowed = @('bs','ca','ko','no','tools')
foreach ($c in $extraAllowed) { [void]$canonicalSet.Add($c) }

$failures = New-Object System.Collections.Generic.List[string]

function Assert-Canonical([string]$Name, [string]$Where, [string]$Context) {
    if (-not $canonicalSet.Contains($Name)) {
        $failures.Add("Non-canonical culture '$Name' in $Where ($Context). Use one of: $($canonicalCultures -join ', ')")
    }
}

# 1. resx file names
$resxFiles = Get-ChildItem -Path (Join-Path (Resolve-RepoRoot) '.') -Recurse -Filter 'Resource.*.resx' -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
foreach ($file in $resxFiles) {
    $name = $file.Name
    $culture = $name.Substring('Resource.'.Length, $name.Length - 'Resource.'.Length - '.resx'.Length)
    if ($culture -eq '') { continue }
    Assert-Canonical $culture "resx file name" $file.FullName
}

# CLI resources use a different prefix (CLI.Resources.*.resx)
$cliFiles = Get-ChildItem -Path (Join-Path (Resolve-RepoRoot) '.') -Recurse -Filter 'CLI.Resources.*.resx' -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
foreach ($file in $cliFiles) {
    $name = $file.Name
    $culture = $name.Substring('CLI.Resources.'.Length, $name.Length - 'CLI.Resources.'.Length - '.resx'.Length)
    if ($culture -eq '') { continue }
    Assert-Canonical $culture "resx file name" $file.FullName
}

$repo = Resolve-RepoRoot

# 2. Directory.Build.props UdtSatelliteResourceLanguages
$propsPath = Join-Path $repo 'Directory.Build.props'
if (Test-Path -LiteralPath $propsPath) {
    $props = Get-Content -LiteralPath $propsPath -Raw
    foreach ($m in [regex]::Matches($props, '<UdtSatelliteResourceLanguages>([^<]+)</UdtSatelliteResourceLanguages>')) {
        foreach ($c in $m.Groups[1].Value -split ';') {
            if ($c.Trim() -eq '') { continue }
            Assert-Canonical $c.Trim() 'Directory.Build.props UdtSatelliteResourceLanguages' ''
        }
    }
}

# 3. crowdin.yml locale mapping values
$crowdinPath = Join-Path $repo 'crowdin.yml'
if (Test-Path -LiteralPath $crowdinPath) {
    $crowdin = Get-Content -LiteralPath $crowdinPath
    for ($i = 0; $i -lt $crowdin.Count; $i++) {
        if ($crowdin[$i] -match '^\s*[a-zA-Z@-]+:\s*(.*)$' -and $crowdin[$i] -notmatch '^\s*(base_path|preserve_hierarchy|files|source|translation|languages_mapping|locale|#|-\s*source|-\s*translation):') {
            $value = $Matches[1].Trim().Trim('"''')
            if ($value -match '^[a-zA-Z0-9@-]+$') {
                Assert-Canonical $value "crowdin.yml locale mapping" "line $($i + 1)"
            }
        }
    }
}

# 4. Build-LanguageAssets.ps1 pack definitions
$packScript = Join-Path $repo 'Scripts\Build-LanguageAssets.ps1'
if (Test-Path -LiteralPath $packScript) {
    $packText = Get-Content -LiteralPath $packScript -Raw
    foreach ($m in [regex]::Matches($packText, "Culture\s*=\s*'([^']+)'")) {
        Assert-Canonical $m.Groups[1].Value 'Build-LanguageAssets.ps1 pack definition' ''
    }
    foreach ($m in [regex]::Matches($packText, 'Culture\s*=\s*"([^"]+)"')) {
        Assert-Canonical $m.Groups[1].Value 'Build-LanguageAssets.ps1 pack definition' ''
    }
}

# 5. catalog.json "culture" fields and language zip URLs
$catalogFiles = @(
    (Join-Path $repo 'resources\stable\catalog.json'),
    (Join-Path $repo 'Tools\LanguagePackMockBackend\_serve\catalog.json')
)
foreach ($catalogPath in $catalogFiles) {
    if (-not (Test-Path -LiteralPath $catalogPath)) { continue }
    $catalog = Get-Content -LiteralPath $catalogPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($lang in $catalog.languages) {
        $culture = [string]$lang.culture
        Assert-Canonical $culture "catalog.json culture field" (Split-Path $catalogPath -Leaf)
        $url = [string]$lang.url
        if ($url -match "/languages/([^/]+)\.zip") {
            Assert-Canonical $Matches[1] "catalog.json language pack URL" (Split-Path $catalogPath -Leaf)
        }
    }
}

# 6. Shared LocalizationCatalog (C# source)
$catalogNames = @([regex]::Matches($catalogBlock.Groups['values'].Value, 'new\("([a-zA-Z0-9@-]+)"\)') |
    ForEach-Object { $_.Groups[1].Value })
if (((@($catalogNames | Sort-Object -Unique) -join '|') -ne (@($canonicalCultures | Sort-Object -Unique) -join '|'))) {
    $failures.Add('Shared LocalizationCatalog culture list could not be read consistently')
}
foreach ($c in $catalogNames) {
    Assert-Canonical $c 'LocalizationCatalog.SupportedCultures' ''
}

# 7. Installer AppLanguages (FirstRunState.cs)
$installerPath = Join-Path $repo 'Tools\Installer\FirstRunState.cs'
if (Test-Path -LiteralPath $installerPath) {
    $installer = Get-Content -LiteralPath $installerPath -Raw
    foreach ($m in [regex]::Matches($installer, 'new\("[a-zA-Z0-9@-]+",')) {
        $c = ($m.Value -replace '^new\("', '' -replace '"[,;)]$', '')
        Assert-Canonical $c 'Installer AppLanguages' ''
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Culture naming check FAILED:" -ForegroundColor Red
    foreach ($f in ($failures | Sort-Object -Unique)) { Write-Host "  - $f" }
    exit 1
}

Write-Host "Culture naming check OK ($($canonicalCultures.Count) canonical cultures)."
exit 0
