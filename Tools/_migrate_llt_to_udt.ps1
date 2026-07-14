<#
.SYNOPSIS
  Hard-cutover rename: LenovoLegionToolkit.* ABI → UniversalDeviceToolkit.*

.DESCRIPTION
  Phase 3 migration for UniversalDeviceToolkit host repo.
  Preserves intentional *legacy* string constants (AppData, dual IPC pipe, LLT_* env keys)
  via placeholders, then restores them after bulk replace.

  Run from repo root:
    pwsh -File Tools/_migrate_llt_to_udt.ps1
#>
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$exts = @(
  '.cs', '.csproj', '.props', '.targets', '.xaml', '.resx', '.json', '.md',
  '.yml', '.yaml', '.ps1', '.xml', '.config', '.sln', '.txt', '.editorconfig'
)
$excludeDirNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
@(
  'bin', 'obj', '.git', '_obsolete_translations', 'node_modules',
  'packages', '.vs', 'TestResults', 'artifacts'
) | ForEach-Object { [void]$excludeDirNames.Add($_) }

function Test-ExcludedPath([string]$fullPath) {
  $parts = $fullPath.Split([char[]]@('\', '/'))
  foreach ($p in $parts) {
    if ($excludeDirNames.Contains($p)) { return $true }
    # transient WPF temp projects
    if ($p -like '*_wpftmp*') { return $true }
  }
  return $false
}

# Placeholders for strings that MUST remain LLT after cutover (compat surface)
$protect = [ordered]@{
  '__LEGACY_ASSEMBLY_LIB_PLUGINS__' = 'LenovoLegionToolkit.Lib.Plugins'
  '__LEGACY_ASSEMBLY_LIB__'         = 'LenovoLegionToolkit.Lib'
  '__LEGACY_PLUGINS_SDK__'          = 'LenovoLegionToolkit.Plugins.SDK'
  '__LEGACY_PLUGINS_SHARED__'       = 'LenovoLegionToolkit.Plugins.Shared'
  '__LEGACY_PLUGINS_PREFIX__'       = 'LenovoLegionToolkit.Plugins.'
  '__LEGACY_COMPACT_NAME__'         = 'LenovoLegionToolkit'
  '__LEGACY_DISPLAY_NAME__'         = 'Lenovo Legion Toolkit'
  '__LEGACY_IPC_PIPE__'             = 'LenovoLegionToolkit-IPC-0'
  '__LEGACY_REPO_URL_SSC__'         = 'https://github.com/SSC-STUDIO/LenovoLegionToolkit'
  '__LEGACY_FOLDER_PLUGINS_REPO__'  = 'LenovoLegionToolkit-Plugins'
}

# Files/regions where we protect legacy tokens before bulk replace
# Strategy: protect quoted string literals and known constant assignments first globally via regex

function Protect-Legacy([string]$text) {
  # Dual IPC legacy pipe (exact)
  $text = $text.Replace('"LenovoLegionToolkit-IPC-0"', '"__LEGACY_IPC_PIPE__"')
  $text = $text.Replace("'LenovoLegionToolkit-IPC-0'", "'__LEGACY_IPC_PIPE__'")

  # Assembly simple names used as legacy constants
  $text = $text.Replace('"LenovoLegionToolkit.Lib.Plugins"', '"__LEGACY_ASSEMBLY_LIB_PLUGINS__"')
  $text = $text.Replace('"LenovoLegionToolkit.Lib"', '"__LEGACY_ASSEMBLY_LIB__"')
  $text = $text.Replace('"LenovoLegionToolkit.Plugins.SDK"', '"__LEGACY_PLUGINS_SDK__"')
  $text = $text.Replace('"LenovoLegionToolkit.Plugins.Shared"', '"__LEGACY_PLUGINS_SHARED__"')
  $text = $text.Replace('"LenovoLegionToolkit.Plugins.SDK.dll"', '"__LEGACY_PLUGINS_SDK__.dll"')
  $text = $text.Replace('"LenovoLegionToolkit.Plugins.Shared.dll"', '"__LEGACY_PLUGINS_SHARED__.dll"')
  $text = $text.Replace('"LenovoLegionToolkit.Plugins."', '"__LEGACY_PLUGINS_PREFIX__"')
  $text = $text.Replace('"LenovoLegionToolkit"', '"__LEGACY_COMPACT_NAME__"')
  $text = $text.Replace('"Lenovo Legion Toolkit"', '"__LEGACY_DISPLAY_NAME__"')
  $text = $text.Replace('"https://github.com/SSC-STUDIO/LenovoLegionToolkit"', '"__LEGACY_REPO_URL_SSC__"')

  # Path fallbacks to old plugins repo folder name (compat condition paths)
  $text = $text.Replace('LenovoLegionToolkit-Plugins', '__LEGACY_FOLDER_PLUGINS_REPO__')

  # LLT_* environment variable *string keys* (not type names) — protect LLT_ prefix in quotes
  $text = [regex]::Replace($text, '"LLT_([A-Z0-9_]+)"', '"__LLT_KEY__$1"')
  $text = [regex]::Replace($text, "'LLT_([A-Z0-9_]+)'", "'__LLT_KEY__$1'")

  return $text
}

function Unprotect-Legacy([string]$text) {
  foreach ($k in $protect.Keys) {
    $text = $text.Replace($k, $protect[$k])
  }
  $text = [regex]::Replace($text, '"__LLT_KEY__([A-Z0-9_]+)"', '"LLT_$1"')
  $text = [regex]::Replace($text, "'__LLT_KEY__([A-Z0-9_]+)'", "'LLT_$1'")
  return $text
}

function Transform-Content([string]$text) {
  $text = Protect-Legacy $text

  # Longest-first identifier renames
  $pairs = @(
    @('LenovoLegionToolkit.Lib.Plugins', 'UniversalDeviceToolkit.Lib.Plugins'),
    @('LenovoLegionToolkit.Plugins', 'UniversalDeviceToolkit.Plugins'),
    @('LenovoLegionToolkit.Lib', 'UniversalDeviceToolkit.Lib'),
    # Remaining type/product tokens that are not protected legacy literals
    @('LenovoLegionToolkit', 'UniversalDeviceToolkit')
  )
  foreach ($p in $pairs) {
    $text = $text.Replace($p[0], $p[1])
  }

  $text = Unprotect-Legacy $text
  return $text
}

$files = Get-ChildItem -Path $root -Recurse -File | Where-Object {
  $exts -contains $_.Extension.ToLowerInvariant() -and -not (Test-ExcludedPath $_.FullName)
}

$changed = 0
$scanned = 0
foreach ($f in $files) {
  $scanned++
  # Skip this migration script itself (contains many example tokens)
  if ($f.Name -eq '_migrate_llt_to_udt.ps1') { continue }

  $raw = [System.IO.File]::ReadAllText($f.FullName)
  if ($raw -notmatch 'LenovoLegionToolkit|LLT_') {
    # still may need llt assembly name later
  }
  $new = Transform-Content $raw

  # CLI executable rename: AssemblyName llt → udt-cli (avoid clash with CrossPlatform "udt")
  if ($f.Name -eq 'UniversalDeviceToolkit.CLI.csproj') {
    $new = $new -replace '<AssemblyName>llt</AssemblyName>', '<AssemblyName>udt-cli</AssemblyName>'
  }
  # Also rename bare "llt.exe" references in docs/scripts to udt-cli.exe where appropriate
  if ($f.Extension -in '.md', '.ps1', '.yml', '.yaml', '.cs') {
    $new = $new -replace '\bllt\.exe\b', 'udt-cli.exe'
    # package references to tool name "llt " carefully — only command examples
  }

  if ($new -ne $raw) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    # preserve BOM if original had it
    $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $enc = New-Object System.Text.UTF8Encoding $hasBom
    [System.IO.File]::WriteAllText($f.FullName, $new, $enc)
    $changed++
  }
}

Write-Host "Scanned: $scanned  Changed: $changed"

# Post: update csproj comments for Lib / Lib.Plugins explicitly
$libCsproj = Join-Path $root 'UniversalDeviceToolkit.Lib\UniversalDeviceToolkit.Lib.csproj'
$pluginsCsproj = Join-Path $root 'UniversalDeviceToolkit.Lib.Plugins\UniversalDeviceToolkit.Lib.Plugins.csproj'

foreach ($path in @($libCsproj, $pluginsCsproj)) {
  if (-not (Test-Path $path)) { continue }
  $c = [System.IO.File]::ReadAllText($path)
  $c2 = $c -replace '<!-- ABI retention:.*?-->', '<!-- Assembly/namespace: UniversalDeviceToolkit.* (Phase 3 hard cutover). Legacy LLT names remain only as BrandCompatibility / dual-load strings. -->'
  if ($c2 -ne $c) {
    [System.IO.File]::WriteAllText($path, $c2, (New-Object System.Text.UTF8Encoding $false))
    Write-Host "Updated comments: $path"
  }
}

Write-Host 'Done host-repo bulk migration.'
