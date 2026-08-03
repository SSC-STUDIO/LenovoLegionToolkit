<#
.SYNOPSIS
  Fail if WPF satellite resx packs are missing keys vs base, or leave priority UI in English.

.DESCRIPTION
  Used by CI and local agents. Scans UniversalDeviceToolkit.WPF/Resources.
  - Base: Resource.resx
  - Satellites: Resource.*.resx (except en, which may equal base)
  - Priority prefixes must not match English base for non-en cultures (except short tech tokens).
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

function Get-ResxKeys([string]$Path) {
    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $set = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($m in [regex]::Matches($raw, '<data name="([^"]+)"')) {
        [void]$set.Add($m.Groups[1].Value)
    }
    return $set
}

function Get-ResxMap([string]$Path) {
    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $map = @{}
    foreach ($m in [regex]::Matches($raw, '<data name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)</value>')) {
        $map[$m.Groups[1].Value] = $m.Groups[2].Value
    }
    return $map
}

$repo = Resolve-RepoRoot
$resDir = Join-Path $repo 'UniversalDeviceToolkit.WPF\Resources'
$basePath = Join-Path $resDir 'Resource.resx'
if (-not (Test-Path -LiteralPath $basePath)) {
    throw "Base resx not found: $basePath"
}

$baseKeys = Get-ResxKeys $basePath
$baseMap = Get-ResxMap $basePath
Write-Host "Base keys: $($baseKeys.Count)"

$priorityPrefixes = @(
    'NetworkAcceleration',
    'PluginExtensions',
    'DeviceSetup',
    'CrashReport',
    'AppNotification',
    'SensorsControl',
    'WindowsOptimization',
    'MainWindow_Plugin',
    'NotificationsSettings',
    'FanCurve'
)

# Short technical tokens + loanwords that are identical in many locales (not a translation miss).
$techExact = [regex]::new(
    '^(DNS|DoH|Hosts|ms|KB/s|—|–|-|CPU|GPU|HDR|OSD|PAC|TLS|UDT|FPS|°C|°F|GB|GHz|1% Low|Nilesoft Shell|HWiNFO64|Over Drive|Microphone|Notifications|Exception|Diagnostics|Maximum: \{0\}|\{0\}%?|\{0\})$',
    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
)

$failures = New-Object System.Collections.Generic.List[string]
$satellites = Get-ChildItem -LiteralPath $resDir -Filter 'Resource.*.resx' | Sort-Object Name

foreach ($file in $satellites) {
    $culture = $file.BaseName.Substring('Resource.'.Length)
    $keys = Get-ResxKeys $file.FullName
    $missing = @()
    foreach ($k in $baseKeys) {
        if (-not $keys.Contains($k)) { $missing += $k }
    }
    if ($missing.Count -gt 0) {
        $sample = ($missing | Select-Object -First 8) -join ', '
        $failures.Add("$($file.Name): missing $($missing.Count) keys vs base (e.g. $sample)")
    }

    # en may intentionally match base English
    if ($culture -eq 'en') {
        continue
    }

    $map = Get-ResxMap $file.FullName
    $englishLike = 0
    $samples = New-Object System.Collections.Generic.List[string]
    foreach ($k in $map.Keys) {
        $isPriority = $false
        foreach ($p in $priorityPrefixes) {
            if ($k.StartsWith($p, [StringComparison]::Ordinal)) { $isPriority = $true; break }
        }
        if (-not $isPriority) { continue }
        if (-not $baseMap.ContainsKey($k)) { continue }
        $eng = [string]$baseMap[$k]
        $val = [string]$map[$k]
        if ($val -ne $eng) { continue }
        $stripped = $eng.Trim()
        if ($techExact.IsMatch($stripped) -or $stripped.Length -le 8) { continue }
        if ($stripped -notmatch '[A-Za-z]{4}') { continue }
        $englishLike++
        if ($samples.Count -lt 5) { $samples.Add($k) }
    }
    if ($englishLike -gt 0) {
        $failures.Add("$($file.Name): $englishLike priority UI string(s) still equal English ($($samples -join ', '))")
    }
}

# Languages list cultures that must have a satellite (shipped UI languages).
# Canonical BCP 47 form (CONTRIBUTING.md); keep in sync with LocalizationHelper.Languages.
$requiredCultures = @(
    'ar','bg','cs','de','el','en','es','fr','hu','it','ja','lv','nl-NL','pl','pt','pt-BR',
    'ro','ru','sk','tr','uk','uz-Latn-UZ','vi','zh-Hans','zh-Hant'
)
foreach ($c in $requiredCultures) {
    $path = Join-Path $resDir "Resource.$c.resx"
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required satellite Resource.$c.resx (Languages / shipping list)")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "WPF l10n coverage FAILED:" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host "  - $f" }
    exit 1
}

Write-Host "WPF l10n coverage OK ($($satellites.Count) satellites, base=$($baseKeys.Count) keys)."
exit 0
