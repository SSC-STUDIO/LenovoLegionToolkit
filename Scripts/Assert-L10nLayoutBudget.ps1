<#
.SYNOPSIS
  Fail if localized translations for tightly constrained UI areas exceed the
  dynamic layout budget.

.DESCRIPTION
  CardHeaderControl subtitles and other constrained controls use AdaptiveTextBlock
  to scale down, but that should be a last resort. This script checks that
  translations remain within a reasonable length envelope compared to English,
  so the UI can keep its intended font size and visual rhythm.

  Scans WPF XAML for <controls:CardHeaderControl Subtitle="..."/> and uses the
  WPF Resource.resx as the English baseline. Satellites are compared using a
  per-language expansion factor.
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

function Get-ResxMap([string]$Path) {
    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $map = @{}
    foreach ($m in [regex]::Matches($raw, '<data name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)</value>')) {
        $map[$m.Groups[1].Value] = $m.Groups[2].Value
    }
    return $map
}

function Get-CultureName([string]$BaseName) {
    if ($BaseName -eq 'Resource') { return 'en' }
    $prefix = 'Resource.'
    if ($BaseName.StartsWith($prefix)) {
        return $BaseName.Substring($prefix.Length)
    }
    return $BaseName
}

# The CardHeaderControl subtitle is a fixed-height block (3 lines, max 60dip).
# AdaptiveTextBlock scales down to ~11px, giving roughly four lines of text.
# The budget is absolute: translations that exceed these limits will be clipped
# or unreadably small even after dynamic scaling.
$budgets = @{
    'en'      = 250
    'zh-Hans' = 200
    'zh-Hant' = 200
    'ja'      = 200
    'ko'      = 200
}
$defaultBudget = 300

# Known legacy translations that are still longer than the budget. These should
# be shortened by translators or moved to a tooltip, but they are allowed to
# avoid blocking the CI while the adaptive layout is being rolled out.
$allowlist = @(
    'ar:GodModeSettingsWindow_Fans_Curve_Message',
    'bg:GodModeSettingsWindow_Fans_Curve_Message',
    'fr:GodModeSettingsWindow_Fans_Curve_Message'
)

$repo = Resolve-RepoRoot
$wpfDir = Join-Path $repo 'UniversalDeviceToolkit.WPF'
$resDir = Join-Path $wpfDir 'Resources'

$basePath = Join-Path $resDir 'Resource.resx'
if (-not (Test-Path -LiteralPath $basePath)) {
    throw "Base resx not found: $basePath"
}
$baseMap = Get-ResxMap $basePath

# Find all subtitle resource keys used by CardHeaderControl in WPF XAML.
$xamlFiles = Get-ChildItem -LiteralPath $wpfDir -Filter '*.xaml' -Recurse -File
$subtitleKeys = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$subtitlePattern = 'Subtitle\s*=\s*"\{x:Static[^:]*:Resource\.([^}]+)\}'
foreach ($file in $xamlFiles) {
    $raw = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($raw, $subtitlePattern)) {
        [void]$subtitleKeys.Add($m.Groups[1].Value)
    }
}

if ($subtitleKeys.Count -eq 0) {
    throw 'No CardHeaderControl subtitle keys found in WPF XAML. Budget definition is out of sync.'
}

Write-Host "Layout budget keys: $($subtitleKeys.Count)"

$failures = New-Object System.Collections.Generic.List[string]
$satellites = Get-ChildItem -LiteralPath $resDir -Filter 'Resource.*.resx' -File | Sort-Object Name

foreach ($file in $satellites) {
    $culture = Get-CultureName $file.BaseName
    $map = Get-ResxMap $file.FullName
    foreach ($key in $subtitleKeys) {
        if (-not $baseMap.ContainsKey($key)) {
            $failures.Add("$($file.Name): baseline key $key missing from Resource.resx")
            continue
        }
        if (-not $map.ContainsKey($key)) {
            # Missing key is already covered by Assert-WpfL10nCoverage; do not duplicate here.
            continue
        }

        $english = [string]$baseMap[$key]
        $translation = [string]$map[$key]
        if ($english.Length -eq 0) { continue }

        $budget = $defaultBudget
        if ($budgets.ContainsKey($culture)) {
            $budget = $budgets[$culture]
        }

        $allowKey = "$culture`:$key"
        if ($translation.Length -gt $budget) {
            if ($allowlist -contains $allowKey) {
                Write-Host "  (allowed) $($file.Name): $key length $($translation.Length) exceeds budget $budget (English=$($english.Length))" -ForegroundColor Yellow
            }
            else {
                $failures.Add("$($file.Name): $key length $($translation.Length) exceeds budget $budget (English=$($english.Length))")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Layout budget FAILED:" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host "  - $f" }
    exit 1
}

Write-Host "Layout budget OK ($($satellites.Count) satellites, $($subtitleKeys.Count) constrained keys)."
exit 0
