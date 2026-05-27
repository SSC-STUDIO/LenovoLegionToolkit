param(
  [Parameter(Mandatory = $true)]
  [string]$Version,

  [Parameter(Mandatory = $true)]
  [string]$ChangelogPath,

  [string[]]$AssetNames = @(),

  [string]$ProductName = 'Universal Device Toolkit',

  [string]$ReleaseDate,

  [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

function Get-ReleaseSection {
  param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $true)]
    [string]$ReleaseVersion
  )

  # Always read changelog as UTF-8 (no BOM). Default Get-Content encoding is locale-dependent on Windows.
  $lines = [System.IO.File]::ReadAllLines($Path, [System.Text.UTF8Encoding]::new($false))
  $versionPattern = '^## \[' + [regex]::Escape($ReleaseVersion) + '\] - (?<date>\d{4}-\d{2}-\d{2})$'
  $startIndex = -1
  $endIndex = $lines.Count
  $releaseDate = $null

  for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match $versionPattern) {
      $startIndex = $i + 1
      $releaseDate = $Matches['date']
      break
    }
  }

  if ($startIndex -lt 0 -or [string]::IsNullOrWhiteSpace($releaseDate)) {
    return $null
  }

  for ($i = $startIndex; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^## \[') {
      $endIndex = $i
      break
    }
  }

  $section = ''
  if ($endIndex -gt $startIndex) {
    $section = ($lines[$startIndex..($endIndex - 1)] -join "`n").Trim()
  }

  [pscustomobject]@{
    Date = $releaseDate
    Body = $section
  }
}

function Get-SectionSummary {
  param(
    [string]$Body
  )

  if ([string]::IsNullOrWhiteSpace($Body)) {
    return @('- Release notes were not available in the repository changelog for this historical version.')
  }

  $items = New-Object System.Collections.Generic.List[string]
  foreach ($line in ($Body -split "`r?`n")) {
    $trimmed = $line.Trim()
    if (-not $trimmed.StartsWith('- ')) {
      continue
    }

    $items.Add($trimmed)
    if ($items.Count -ge 5) {
      break
    }
  }

  if ($items.Count -eq 0) {
    return @('- See the detailed changes section below.')
  }

  return $items.ToArray()
}

function Add-AssetLine {
  param(
    [System.Collections.Generic.List[string]]$Lines,
    [string]$AssetName,
    [string]$Description
  )

  $Lines.Add("- ``$AssetName`` - $Description")
}

function Get-DownloadLines {
  param(
    [string]$ReleaseVersion,
    [string[]]$Names
  )

  $lines = New-Object System.Collections.Generic.List[string]
  $sorted = @($Names | Sort-Object)

  $fullSetup = $sorted | Where-Object { $_ -match '_Full_Setup\.exe$' } | Select-Object -First 1
  $onlineSetup = $sorted | Where-Object { $_ -match '_Online_Setup\.exe$' } | Select-Object -First 1
  $fullZip = $sorted | Where-Object { $_ -match '_Full_win-x64\.zip$' } | Select-Object -First 1
  $onlineZip = $sorted | Where-Object { $_ -match '_Online_win-x64\.zip$' } | Select-Object -First 1
  $englishSetup = $sorted | Where-Object { $_ -match '_English_Setup\.exe$' } | Select-Object -First 1
  $englishZip = $sorted | Where-Object { $_ -match '_English_win-x64\.zip$' } | Select-Object -First 1
  $legacyAlias = $sorted | Where-Object { $_ -match '^LenovoLegionToolkit_v\d+\.\d+\.\d+_Setup\.exe$' } | Select-Object -First 1
  $setup = $sorted | Where-Object { $_ -match 'Setup\.exe$' -and $_ -ne $fullSetup -and $_ -ne $onlineSetup -and $_ -ne $englishSetup -and $_ -ne $legacyAlias } | Select-Object -First 1
  $zip = $sorted | Where-Object { $_ -match '_win-x64\.zip$' -and $_ -ne $fullZip -and $_ -ne $onlineZip -and $_ -ne $englishZip } | Select-Object -First 1
  $sha = $sorted | Where-Object { $_ -match '_SHA256\.txt$' } | Select-Object -First 1
  $languagePacks = @($sorted | Where-Object { $_ -match '_lang_[^/]+\.zip$' })

  if ($fullSetup) { Add-AssetLine $lines $fullSetup 'full installer with all bundled languages and built-in device packs.' }
  if ($onlineSetup) { Add-AssetLine $lines $onlineSetup 'online installer with English bundled; additional language and device resources are installed from the online catalog.' }
  if ($fullZip) { Add-AssetLine $lines $fullZip 'full portable package with all bundled languages and built-in device packs.' }
  if ($onlineZip) { Add-AssetLine $lines $onlineZip 'online portable package with English bundled; additional resources are installed from the online catalog.' }
  if ($legacyAlias) { Add-AssetLine $lines $legacyAlias 'legacy Lenovo Legion Toolkit compatibility installer alias for existing updater and package-manager users.' }
  if ($englishSetup) { Add-AssetLine $lines $englishSetup 'English-only installer; install additional languages from Settings.' }
  if ($englishZip) { Add-AssetLine $lines $englishZip 'English-only portable package; install additional languages from Settings.' }
  if ($setup) { Add-AssetLine $lines $setup 'installer package.' }
  if ($zip) { Add-AssetLine $lines $zip 'portable win-x64 package.' }
  if ($languagePacks.Count -gt 0) { $lines.Add("- ``*_lang_<culture>.zip`` - optional language packs for this release ($($languagePacks.Count) assets).") }
  if ($sha) { Add-AssetLine $lines $sha 'SHA256 checksum manifest.' }

  if ($lines.Count -eq 0) {
    $lines.Add('- No release assets are currently attached to this historical release.')
  }

  return $lines.ToArray()
}

function Get-OnlineResourceLines {
  param(
    [string]$ReleaseVersion,
    [string[]]$Names
  )

  $hasUniversalAssets = @($Names | Where-Object { $_ -like 'UniversalDeviceToolkit_*' }).Count -gt 0
  $hasLanguagePackAssets = @($Names | Where-Object { $_ -match '_lang_[^/]+\.zip$' }).Count -gt 0

  if ($hasUniversalAssets) {
    return @(
      "- Language packs are published through GitHub Pages under ``resources/$ReleaseVersion/languages``.",
      "- Device packs are published through GitHub Pages under ``resources/$ReleaseVersion/devices``.",
      '- The stable online resource catalog is published at `resources/stable/catalog.json`, with `stable/catalog.json` kept as a compatibility copy.'
    )
  }

  if ($hasLanguagePackAssets) {
    return @(
      '- Optional language packs are attached directly to this historical GitHub Release.',
      '- Device packs and the GitHub Pages resource catalog were introduced later in the Universal Device Toolkit release train.'
    )
  }

  return @(
    '- This historical release does not use the GitHub Pages language/device resource catalog.'
  )
}

function Get-VerificationLines {
  param(
    [string[]]$Names
  )

  $sha = @($Names | Where-Object { $_ -match '_SHA256\.txt$' }).Count -gt 0
  if ($sha) {
    return @(
      '- Download the SHA256 manifest attached to this release.',
      '- Verify any downloaded asset with `CertUtil -hashfile <file> SHA256` and compare it with the manifest.'
    )
  }

  return @(
    '- This historical release does not include a SHA256 manifest asset; verify the download source and prefer newer releases when possible.'
  )
}

$section = Get-ReleaseSection -Path $ChangelogPath -ReleaseVersion $Version
$releaseDate = if ($section) { $section.Date } elseif (-not [string]::IsNullOrWhiteSpace($ReleaseDate)) { $ReleaseDate } else { 'Unknown' }
$changeBody = if ($section) { $section.Body } else { '' }
$highlights = Get-SectionSummary -Body $changeBody
$downloads = Get-DownloadLines -ReleaseVersion $Version -Names $AssetNames
$onlineResources = Get-OnlineResourceLines -ReleaseVersion $Version -Names $AssetNames
$verification = Get-VerificationLines -Names $AssetNames

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# $ProductName v$Version")
$lines.Add('')
$lines.Add("Release date: $releaseDate")
$lines.Add('')
$lines.Add('## Highlights')
$lines.AddRange([string[]]$highlights)
$lines.Add('')
$lines.Add('## Changes')
if ([string]::IsNullOrWhiteSpace($changeBody)) {
  $lines.Add('- No changelog section is available for this historical version.')
} else {
  $lines.Add($changeBody)
}
$lines.Add('')
$lines.Add('## Downloads')
$lines.AddRange([string[]]$downloads)
$lines.Add('')
$lines.Add('## Online resources')
$lines.AddRange([string[]]$onlineResources)
$lines.Add('')
$lines.Add('## Compatibility')
$lines.Add('- OS: Windows 10/11 x64')
$lines.Add('- Architecture: x64')
$lines.Add('')
$lines.Add('## Verification')
$lines.AddRange([string[]]$verification)

$notes = ($lines -join "`n").Trim() + "`n"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
  Write-Output $notes
} else {
  $parent = Split-Path -Parent $OutputPath
  if (-not [string]::IsNullOrWhiteSpace($parent)) {
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
  }
  [System.IO.File]::WriteAllText($OutputPath, $notes, [System.Text.UTF8Encoding]::new($false))
}
